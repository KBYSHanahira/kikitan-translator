using KikitanTranslator.Utility;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Backends.MiniAudio.Enums;
using SoundFlow.Enums;
using SoundFlow.Extensions.WebRtc.Apm;
using SoundFlow.Extensions.WebRtc.Apm.Modifiers;
using SoundFlow.Structs;

namespace KikitanTranslator.Capture;

public class Microphone : ICapture
{
    public event OnData? OnDataReceived;
    private bool _paused;
    
    private MiniAudioEngine _engine;
    private WebRtcApmModifier? _apmModifier;
    private AudioCaptureDevice? _captureDevice;
    private IErrorHandler _errorHandler;
    private SileroVad _vad;
    private int _channels = 1;

    public uint GetSampleRate() => 16000;

    public Microphone(string sileroModelPath, IErrorHandler errorHandler)
    {
        _engine = new MiniAudioEngine(backendPriority:[MiniAudioBackend.Wasapi, MiniAudioBackend.Oss]);
        _vad = new(sileroModelPath);
        _errorHandler = errorHandler;
    }

    public DeviceInfo[] GetCaptureDevices()
    {
        _engine.UpdateAudioDevicesInfo();

        return CaptureDeviceNames.Fix(_engine.CaptureDevices);
    }

    public bool Start()
    {
        _engine.UpdateAudioDevicesInfo();
        var devices = CaptureDeviceNames.Fix(_engine.CaptureDevices);
        if (_captureDevice != null) Stop();

        DeviceInfo? device;
        
        var audioFormat = new AudioFormat
        {
            SampleRate = 16000,
            Channels = 1,
            Format = SampleFormat.S16,
            Layout = ChannelLayout.Mono 
        };

        if (AppConfig.ConfigObject.Microphone.Length == 0)
        {
            device = devices.First(d => d.IsDefault);
            AppConfig.ConfigObject.Microphone = device.Value.Name;

            Log.Warning($"[MIC]  No microphone was selected, using the default device ({device.Value.Name}) for capture");
        }
        else if (devices.Any(d => d.Name == AppConfig.ConfigObject.Microphone))
        {
            device = devices.First(d => d.Name == AppConfig.ConfigObject.Microphone);
        }
        else
        {
            // DeviceInfo is a struct, so FirstOrDefault returns an empty value (never null) when there is no default.
            device = devices.Any(d => d.IsDefault) ? devices.First(d => d.IsDefault) : devices[0];
                    
            Log.Warning($"[MIC]  The selected mic ({AppConfig.ConfigObject.Microphone}) is not available. Switching to the system default ({device.Value.Name})");
            
            AppConfig.ConfigObject.Microphone = device.Value.Name;
        }
        
        Log.Information($"[MIC]  Starting capture using {device.Value.Name}");
        
        try
        {
            _captureDevice = InitializeCaptureDevice(device, audioFormat);
            _captureDevice.OnAudioProcessed += AudioProcessCallback;
            _captureDevice.Start();
            _apmModifier = new WebRtcApmModifier(
                device: _captureDevice,
                aecEnabled: true,
                agc1Enabled: true,
                hpfEnabled: true
            );
        } catch (Exception e)
        {
            _errorHandler.OnError($"[MIC]  Error initializing capture: {e}");
            AppConfig.ConfigObject.Microphone = "";
            
            if (!device.Value.IsDefault)
            {
                Log.Error($"[MIC]  Error initializing capture: {e}. Restarting with default mic");
                
                Start();
            }
            else
            {
                Log.Error($"[MIC]  Unable to start capture: {e.Message}");
                
                Stop();

                return false;
            }
        }
        
        Log.Information("[MIC]  Capture has started");
        
        return true;
    }

    public void Stop()
    {
        if (_captureDevice == null) return;
        
        _captureDevice.Stop();
        _apmModifier?.Dispose();
        _apmModifier = null;
        _captureDevice = null;
        
        Log.Information("[MIC]  Capture has stopped");
    }

    public void Pause()
    {
        Log.Verbose("[MIC]  Capture paused");
        
        _paused = true;
    }

    public void Resume()
    {
        Log.Verbose("[MIC]  Capture resumed");
        
        _paused = false;
    }

    private AudioCaptureDevice InitializeCaptureDevice(DeviceInfo? device, AudioFormat monoFormat)
    {
        try
        {
            _channels = 1;

            return _engine.InitializeCaptureDevice(device, monoFormat);
        }
        catch (Exception e)
        {
            // Some devices (e.g. Sound BlasterX G1) reject mono capture over WASAPI but accept stereo,
            // so capture in stereo and downmix to mono in the callback.
            Log.Warning($"[MIC]  Mono capture failed ({e.Message}). Retrying in stereo");
            _channels = 2;

            return _engine.InitializeCaptureDevice(device, monoFormat with { Channels = 2, Layout = ChannelLayout.Stereo });
        }
    }

    private void AudioProcessCallback(Span<float> samples, Capability capability)
    {
        if (_paused) return;

        if (_channels == 2)
        {
            var mono = new float[samples.Length / 2];
            for (var i = 0; i < mono.Length; i++) mono[i] = (samples[2 * i] + samples[2 * i + 1]) * 0.5f;

            samples = mono;
        }

        _apmModifier?.Process(samples, samples.Length);

        bool part1 = _vad.SpeechDetection(samples.Slice(0, 480));
        bool part2 = _vad.SpeechDetection(samples.Slice(480, 480));

        OnDataReceived?.Invoke(samples.ToArray(), part1 || part2);
    }
}