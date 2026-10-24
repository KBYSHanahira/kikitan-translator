using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Serilog;

namespace KikitanTranslator.Base.Rvc;

/// <summary>
/// Converts speech into the voice of an RVC model exported to ONNX with RVC WebUI's "Export Onnx"
/// (inputs phone, phone_lengths, pitch, pitchf, ds, rnd; output audio). Follows RVC 2.2's onnx_inference.py,
/// with RMVPE for pitch. Not used: the .index file, protect and RMS mixing.
/// </summary>
public sealed class RvcConverter : IDisposable
{
    private const float F0Min = 50;
    private const float F0Max = 1100;
    private const int NoiseChannels = 192;
    private const int ProbeFrames = 200;
    // The relative attention needs more frames than its window (10); 0.3 s of 16 kHz audio gives 30.
    private const int MinimumSamples16K = 4800;

    private static readonly double F0MelMin = 1127 * Math.Log(1 + F0Min / 700);
    private static readonly double F0MelMax = 1127 * Math.Log(1 + F0Max / 700);

    private readonly InferenceSession _model;
    private readonly ContentVec _contentVec;
    private readonly Rmvpe? _rmvpe;
    private readonly int _channels;
    private readonly Random _random = new();

    public string ModelPath { get; }
    public string Version => _channels == 256 ? "v1" : "v2";
    public bool UsesPitch => _rmvpe != null;
    public int SampleRate { get; }
    public bool OnGpu { get; }

    public static int FeatureChannels(string modelPath)
    {
        using var session = new InferenceSession(modelPath);

        return ReadChannels(session);
    }

    /// <param name="contentVecPath">vec-256-layer-9.onnx for v1 models, vec-768-layer-12.onnx for v2 (see FeatureChannels)</param>
    public RvcConverter(string modelPath, string contentVecPath, string rmvpePath, bool useGpu = true)
    {
        ModelPath = modelPath;
        _model = OnnxSessions.Create(modelPath, useGpu, out var onGpu);

        try
        {
            _channels = ReadChannels(_model);
            _contentVec = new ContentVec(contentVecPath, useGpu);
            if (_model.InputMetadata.ContainsKey("pitchf")) _rmvpe = new Rmvpe(rmvpePath, useGpu);

            OnGpu = onGpu && _contentVec.OnGpu && (_rmvpe?.OnGpu ?? true);

            // The sample rate is not stored in the ONNX file. The synthesizer outputs sampleRate / 100 samples per frame,
            // so one run on silence gives it (and warms up the GPU).
            var probe = Synthesize(new float[ProbeFrames, _channels], new float[ProbeFrames]);
            SampleRate = probe.Length / ProbeFrames * 100;

            // RVC WebUI 2.2's exporter froze the sequence length to its 200-frame example, so such a file fails on
            // any other length. Kikitan's own .pth conversion fixes that, so point the user there.
            try
            {
                Synthesize(new float[ProbeFrames - 63, _channels], new float[ProbeFrames - 63]);
            }
            catch (OnnxRuntimeException)
            {
                throw new InvalidDataException("This ONNX file only accepts one fixed input length (a known problem with RVC WebUI's exporter). Choose the .pth model instead and Kikitan will convert it.");
            }
        }
        catch
        {
            Dispose();
            throw;
        }

        Log.Information($"[RVC]  Loaded {Path.GetFileName(modelPath)}: {Version}, {SampleRate} Hz, pitch={UsesPitch}, gpu={OnGpu}");
    }

    /// <summary>Converts mono audio and returns it at the same sample rate and length.</summary>
    public float[] Convert(float[] audio, int sampleRate, int transpose)
    {
        var audio16K = Resampler.Resample(audio, sampleRate, MelSpectrogram.SampleRate);
        // Very short clips are padded with silence; the result is cut back to the input length below.
        if (audio16K.Length < MinimumSamples16K) Array.Resize(ref audio16K, MinimumSamples16K);

        var features = _contentVec.Compute(audio16K);
        var frames = features.GetLength(0);

        var f0 = new float[frames];
        if (_rmvpe != null)
        {
            var pitch = _rmvpe.Compute(audio16K);
            frames = Math.Min(frames, pitch.Length);

            var shift = (float)Math.Pow(2, transpose / 12.0);
            for (var t = 0; t < frames; t++) f0[t] = pitch[t] * shift;
        }

        if (frames != features.GetLength(0))
        {
            var trimmed = new float[frames, _channels];
            Array.Copy(features, trimmed, frames * _channels);
            features = trimmed;
        }

        var converted = Synthesize(features, f0);

        // onnx_inference.py pads the end with silence and cuts to the input length.
        var result = Resampler.Resample(converted, SampleRate, sampleRate);
        Array.Resize(ref result, audio.Length);
        for (var i = 0; i < result.Length; i++) result[i] = Math.Clamp(result[i], -1f, 1f);

        return result;
    }

    private float[] Synthesize(float[,] features, float[] f0)
    {
        var frames = features.GetLength(0);

        var phone = new DenseTensor<float>([1, frames, _channels]);
        for (var t = 0; t < frames; t++)
            for (var c = 0; c < _channels; c++)
                phone[0, t, c] = features[t, c];

        var noise = new DenseTensor<float>([1, NoiseChannels, frames]);
        for (var i = 0; i < noise.Length; i++) noise.Buffer.Span[i] = NextGaussian();

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("phone", phone),
            NamedOnnxValue.CreateFromTensor("phone_lengths", new DenseTensor<long>(new long[] { frames }, [1])),
            NamedOnnxValue.CreateFromTensor("ds", new DenseTensor<long>(new long[] { 0 }, [1])),
            NamedOnnxValue.CreateFromTensor("rnd", noise)
        };

        if (_model.InputMetadata.ContainsKey("pitchf"))
        {
            var pitch = new DenseTensor<long>([1, frames]);
            var pitchf = new DenseTensor<float>([1, frames]);
            for (var t = 0; t < frames; t++)
            {
                pitchf[0, t] = f0[t];
                pitch[0, t] = CoarsePitch(f0[t]);
            }

            inputs.Add(NamedOnnxValue.CreateFromTensor("pitch", pitch));
            inputs.Add(NamedOnnxValue.CreateFromTensor("pitchf", pitchf));
        }

        using var results = _model.Run(inputs);

        return results.First().AsTensor<float>().ToArray();
    }

    // f0 in Hz to the 1-255 mel-scale index the synthesizer embeds (onnx_inference.py)
    private static long CoarsePitch(float f0)
    {
        var mel = 1127 * Math.Log(1 + f0 / 700.0);
        if (mel > 0) mel = (mel - F0MelMin) * 254 / (F0MelMax - F0MelMin) + 1;

        return (long)Math.Round(Math.Clamp(mel, 1, 255), MidpointRounding.ToEven);
    }

    private static int ReadChannels(InferenceSession session)
    {
        if (!session.InputMetadata.TryGetValue("phone", out var phone))
            throw new InvalidDataException("This is not an RVC model exported with RVC WebUI (it has no \"phone\" input)");

        var channels = phone.Dimensions[^1];
        if (channels != 256 && channels != 768) throw new InvalidDataException($"Unexpected RVC feature size {channels}");

        return channels;
    }

    // Box-Muller; np.random.randn in the reference
    private float NextGaussian()
    {
        var u1 = 1.0 - _random.NextDouble();
        var u2 = _random.NextDouble();

        return (float)(Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2));
    }

    public void Dispose()
    {
        _model.Dispose();
        _contentVec?.Dispose();
        _rmvpe?.Dispose();
    }
}
