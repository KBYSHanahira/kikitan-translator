using System.Globalization;
using System.Runtime.Versioning;
using Newtonsoft.Json;
using Serilog;

namespace KikitanTranslator.Base.Outputs;

public class TtsVoice
{
    // The SAPI token ID (a registry path), stable across launches
    [JsonProperty("id")] public string Id = null!;
    [JsonProperty("name")] public string Name = null!;
    // Two-letter code as used by AppConfig.SourceLanguage / TargetLanguage
    [JsonProperty("language")] public string Language = null!;
}

/// <summary>
/// Offline voices installed in Windows, used through SAPI's COM API so no Windows-specific target framework is needed.
/// The OneCore voices (the ones added by Settings > Time &amp; language > Speech, e.g. Thai "Pattara") are not in SAPI's
/// default list, so their token category is read directly.
/// </summary>
public static class WindowsVoices
{
    private static readonly string[] Categories =
    [
        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices",
        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices"
    ];

    private const int SampleRate = 48000;
    private const int Channels = 2;
    private const int SpeechAudioFormat48KHz16BitStereo = 39;
    private const int SpeakFlagIsNotXml = 16;

    public static TtsVoice[] List()
    {
        if (!OperatingSystem.IsWindows()) return [];

        var voices = new List<TtsVoice>();
        foreach (var category in Categories)
        {
            try
            {
                voices.AddRange(ListCategory(category));
            }
            catch (Exception e)
            {
                Log.Warning($"[TTS]  Unable to list the voices in {category}: {e.Message}");
            }
        }

        return voices.ToArray();
    }

    /// <summary>Speaks the text into a WAV file (48 kHz, 16-bit, stereo) held in memory.</summary>
    [SupportedOSPlatform("windows")]
    public static byte[] Synthesize(TtsVoice voice, string text)
    {
        dynamic token = CreateCom("SAPI.SpObjectToken");
        token.SetId(voice.Id, "", false);

        dynamic spVoice = CreateCom("SAPI.SpVoice");
        spVoice.Voice = token;

        dynamic stream = CreateCom("SAPI.SpMemoryStream");
        stream.Format.Type = SpeechAudioFormat48KHz16BitStereo;
        spVoice.AudioOutputStream = stream;
        spVoice.Speak(text, SpeakFlagIsNotXml);

        return ToWav((byte[])stream.GetData());
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<TtsVoice> ListCategory(string categoryId)
    {
        dynamic category = CreateCom("SAPI.SpObjectTokenCategory");
        category.SetId(categoryId, false);

        dynamic tokens = category.EnumerateTokens();
        for (var i = 0; i < tokens.Count; i++)
        {
            dynamic token = tokens.Item(i);

            var language = ToLanguageCode((string)token.GetAttribute("Language"));
            if (language == null) continue;

            yield return new TtsVoice { Id = token.Id, Name = token.GetAttribute("Name"), Language = language };
        }
    }

    // The Language attribute holds hex LCIDs separated by ';', e.g. "41E" for Thai or "409;9" for English.
    private static string? ToLanguageCode(string lcids)
    {
        try
        {
            var code = CultureInfo.GetCultureInfo(Convert.ToInt32(lcids.Split(';')[0], 16)).TwoLetterISOLanguageName;

            // Windows calls Norwegian Bokmål/Nynorsk "nb"/"nn", the app uses "no".
            return code is "nb" or "nn" ? "no" : code;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static dynamic CreateCom(string progId) =>
        Activator.CreateInstance(Type.GetTypeFromProgID(progId, true)!)!;

    private static byte[] ToWav(byte[] pcm)
    {
        const int bitsPerSample = 16;
        const int blockAlign = Channels * bitsPerSample / 8;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + pcm.Length);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)Channels);
        writer.Write(SampleRate);
        writer.Write(SampleRate * blockAlign);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(pcm.Length);
        writer.Write(pcm);

        return ms.ToArray();
    }
}
