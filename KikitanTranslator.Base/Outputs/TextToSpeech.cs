using System.Collections.Concurrent;
using KikitanTranslator.Base.Rvc;
using KikitanTranslator.Capture;
using KikitanTranslator.Utility;
using NAudio.CoreAudioApi;
using Serilog;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Backends.MiniAudio.Enums;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace KikitanTranslator.Base.Outputs;

/// <summary>
/// Speaks final results on a chosen playback device, with an installed Windows voice for the language or, when there is
/// none, Google Translate's TTS. Playing it on a virtual cable (e.g. "CABLE Input") and selecting the cable's output as
/// the microphone in VRChat or Discord lets other people hear it. With RVC on, the speech is converted into the voice
/// of the chosen RVC model before it is played.
/// </summary>
public class TextToSpeech : IOutput
{
    // AppConfig.TtsVoice value that always uses Google, even when a Windows voice exists for the language
    public const string GoogleVoice = "google";

    // Google rejects longer requests, so longer text is spoken in pieces.
    private const int MaxChunkLength = 200;
    private static readonly char[] SentenceBreaks = ['.', '!', '?', ',', '。', '、', '！', '？', '，'];

    private static readonly AudioFormat Format = new()
    {
        SampleRate = 48000,
        Channels = 2,
        Format = SampleFormat.F32,
        Layout = ChannelLayout.Stereo
    };

    // What speech is decoded into before RVC conversion
    private static readonly AudioFormat MonoFormat = Format with { Channels = 1, Layout = ChannelLayout.Mono };

    private static readonly HttpClient HttpClient = CreateHttpClient();

    // Lives as long as the app. Disposing it crashes: SoundFlow uninitializes the devices the worker already disposed.
    private readonly MiniAudioEngine _engine = new(backendPriority: [MiniAudioBackend.Wasapi, MiniAudioBackend.Oss]);
    private BlockingCollection<(string Text, string Language)>? _queue;
    private CancellationTokenSource? _cancellation;

    public RvcVoiceChanger Rvc { get; } = new();

    public DeviceInfo[] GetPlaybackDevices()
    {
        _engine.UpdateAudioDevicesInfo();

        return CaptureDeviceNames.Fix(_engine.PlaybackDevices, DataFlow.Render);
    }

    public void Start()
    {
        Stop();
        Rvc.Refresh();

        var devices = GetPlaybackDevices();
        if (devices.Length == 0)
        {
            Log.Error("[TTS]  No playback devices are available, text to speech is disabled");

            return;
        }

        DeviceInfo device;
        if (devices.Any(d => d.Name == AppConfig.ConfigObject.TtsDevice))
        {
            device = devices.First(d => d.Name == AppConfig.ConfigObject.TtsDevice);
        }
        else
        {
            // DeviceInfo is a struct, so FirstOrDefault returns an empty value (never null) when there is no default.
            device = devices.Any(d => d.IsDefault) ? devices.First(d => d.IsDefault) : devices[0];

            // The config is left alone so the chosen device (e.g. a virtual cable) is used again once it comes back.
            if (AppConfig.ConfigObject.TtsDevice.Length != 0)
                Log.Warning($"[TTS]  The selected playback device ({AppConfig.ConfigObject.TtsDevice}) is not available. Using the system default ({device.Name})");
        }

        var queue = new BlockingCollection<(string Text, string Language)>();
        var cancellation = new CancellationTokenSource();
        _queue = queue;
        _cancellation = cancellation;

        var voices = WindowsVoices.List();

        Task.Factory.StartNew(() => Worker(device, voices, queue, cancellation.Token), TaskCreationOptions.LongRunning);
    }

    public void Stop()
    {
        _queue?.CompleteAdding();
        _cancellation?.Cancel();
        _queue = null;
        _cancellation = null;
    }

    public void Send(string recognized, string translated, bool final)
    {
        if (!final) return;

        var sttOnly = AppConfig.ConfigObject.SpeechToTextOnly;
        var text = sttOnly ? recognized : translated;
        var language = sttOnly ? AppConfig.ConfigObject.SourceLanguage : AppConfig.ConfigObject.TargetLanguage;
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            _queue?.Add((text.Trim(), language));
        }
        catch (InvalidOperationException)
        {
            // Stopped between the null check and Add
        }
    }

    public bool IsDelayed() => false;

    // The device is opened, used and closed only on this thread. SoundFlow raises PlaybackEnded on the audio thread,
    // and stopping the device from there deadlocks, so that thread only ever signals.
    private void Worker(DeviceInfo deviceInfo, TtsVoice[] voices, BlockingCollection<(string Text, string Language)> queue, CancellationToken token)
    {
        try
        {
            using var device = _engine.InitializePlaybackDevice(deviceInfo, Format);
            device.Start();

            Log.Information($"[TTS]  Text to speech has started on {deviceInfo.Name}");

            try
            {
                foreach (var (text, language) in queue.GetConsumingEnumerable(token))
                {
                    try
                    {
                        var voice = ResolveVoice(voices, language);
                        if (voice != null && OperatingSystem.IsWindows())
                        {
                            Play(device.MasterMixer, ApplyRvc(WindowsVoices.Synthesize(voice, text)), token);
                        }
                        else
                        {
                            foreach (var chunk in Split(text))
                                Play(device.MasterMixer, ApplyRvc(SynthesizeWithGoogle(chunk, language, token)), token);
                        }

                        Log.Debug($"[TTS]  Spoke {text.Length} chars ({language}, {voice?.Name ?? "Google"})");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[TTS]  Unable to speak the text: {e.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Stop() was called
            }

            device.Stop();

            Log.Information("[TTS]  Text to speech has stopped");
        }
        catch (Exception e)
        {
            Log.Error($"[TTS]  Unable to open the playback device {deviceInfo.Name}: {e.Message}");
        }
    }

    // Returns the audio file converted by RVC, or unchanged when RVC is off or its model is not ready yet.
    private byte[] ApplyRvc(byte[] audio)
    {
        if (!AppConfig.ConfigObject.RvcEnabled) return audio;

        float[] samples;
        using (var provider = new AssetDataProvider(_engine, MonoFormat, new MemoryStream(audio)))
        {
            samples = new float[provider.Length];
            Array.Resize(ref samples, provider.ReadBytes(samples));
        }

        var converted = Rvc.TryConvert(samples, MonoFormat.SampleRate);

        return converted == null ? audio : ToWav(converted, MonoFormat.SampleRate);
    }

    // 32-bit float mono WAV
    private static byte[] ToWav(float[] samples, int sampleRate)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + samples.Length * 4);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)3);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 4);
        writer.Write((short)4);
        writer.Write((short)32);
        writer.Write("data"u8.ToArray());
        writer.Write(samples.Length * 4);
        foreach (var sample in samples) writer.Write(sample);

        return ms.ToArray();
    }

    // The chosen voice when it speaks this language, otherwise the first installed voice that does; null means Google.
    private static TtsVoice? ResolveVoice(TtsVoice[] voices, string language)
    {
        var chosen = AppConfig.ConfigObject.TtsVoice;
        if (chosen == GoogleVoice) return null;

        var matching = voices.Where(v => v.Language == language).ToArray();

        return matching.FirstOrDefault(v => v.Id == chosen) ?? matching.FirstOrDefault();
    }

    private static byte[] SynthesizeWithGoogle(string text, string language, CancellationToken token)
    {
        var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={language}&q={Uri.EscapeDataString(text)}";

        return HttpClient.GetByteArrayAsync(url, token).GetAwaiter().GetResult();
    }

    private void Play(Mixer mixer, byte[] mp3, CancellationToken token)
    {
        using var provider = new AssetDataProvider(_engine, Format, new MemoryStream(mp3));
        using var player = new SoundPlayer(_engine, Format, provider);
        using var ended = new ManualResetEventSlim();
        player.PlaybackEnded += (_, _) => ended.Set();

        var duration = TimeSpan.FromSeconds((double)provider.Length / Format.Channels / Format.SampleRate);

        mixer.AddComponent(player);
        player.Play();

        try
        {
            // The timeout only guards against PlaybackEnded never firing.
            ended.Wait(duration + TimeSpan.FromSeconds(2), token);
        }
        finally
        {
            mixer.RemoveComponent(player);
        }
    }

    internal static IEnumerable<string> Split(string text)
    {
        var rest = text.Trim();

        while (rest.Length > MaxChunkLength)
        {
            var window = rest[..MaxChunkLength];

            // Prefer ending a piece at punctuation, then at a space. Thai, Japanese and Chinese often have no spaces.
            var cut = window.LastIndexOfAny(SentenceBreaks);
            if (cut < MaxChunkLength / 2) cut = window.LastIndexOf(' ');
            if (cut < MaxChunkLength / 2) cut = MaxChunkLength - 1;
            if (char.IsHighSurrogate(rest[cut])) cut--;

            yield return rest[..(cut + 1)].Trim();

            rest = rest[(cut + 1)..].TrimStart();
        }

        if (rest.Length > 0) yield return rest;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        return client;
    }
}
