using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace KikitanTranslator.Base.Rvc;

/// <summary>
/// Pitch (F0) extraction with RVC's rmvpe.onnx, following rmvpe.py: log-mel input padded to a multiple of 32 frames,
/// 360 salience bins per frame decoded by a local weighted average around the peak. One frame per 10 ms of 16 kHz audio.
/// </summary>
public sealed class Rmvpe : IDisposable
{
    private const float Threshold = 0.03f;
    private const int Bins = 360;

    private static readonly double[] CentsMapping = CreateCentsMapping();

    private readonly InferenceSession _session;
    public bool OnGpu { get; }

    public Rmvpe(string modelPath, bool useGpu = true)
    {
        _session = OnnxSessions.Create(modelPath, useGpu, out var onGpu);
        OnGpu = onGpu;
    }

    /// <summary>F0 in Hz per frame for 16 kHz mono audio; 0 marks unvoiced frames.</summary>
    public float[] Compute(float[] audio16K)
    {
        var mel = MelSpectrogram.Compute(audio16K);
        var frames = mel[0].Length;
        var padded = 32 * ((frames - 1) / 32 + 1);

        // Padding frames are constant zeros, like F.pad(mel, (0, n_pad), mode="constant").
        var input = new DenseTensor<float>([1, MelSpectrogram.MelBins, padded]);
        for (var m = 0; m < MelSpectrogram.MelBins; m++)
            for (var t = 0; t < frames; t++)
                input[0, m, t] = mel[m][t];

        using var results = _session.Run([NamedOnnxValue.CreateFromTensor(_session.InputMetadata.Keys.First(), input)]);
        var hidden = results.First().AsTensor<float>();

        var f0 = new float[frames];
        for (var t = 0; t < frames; t++) f0[t] = Decode(hidden, t);

        return f0;
    }

    // to_local_average_cents + decode: weighted cents over the 9 bins around the peak, 10 * 2^(cents / 1200) Hz.
    private static float Decode(Tensor<float> hidden, int frame)
    {
        var center = 0;
        var peak = float.MinValue;
        for (var i = 0; i < Bins; i++)
        {
            var value = hidden[0, frame, i];
            if (value > peak)
            {
                peak = value;
                center = i;
            }
        }

        if (peak <= Threshold) return 0;

        // The Python code pads salience and the mapping with 4 zeros on each side; bins outside [0, 360) count as 0.
        double weighted = 0, total = 0;
        for (var i = center - 4; i <= center + 4; i++)
        {
            if (i < 0 || i >= Bins) continue;

            var salience = hidden[0, frame, i];
            weighted += salience * CentsMapping[i];
            total += salience;
        }

        var cents = weighted / total;

        return (float)(10 * Math.Pow(2, cents / 1200));
    }

    private static double[] CreateCentsMapping()
    {
        var mapping = new double[Bins];
        for (var i = 0; i < Bins; i++) mapping[i] = 20 * i + 1997.3794084376191;

        return mapping;
    }

    public void Dispose() => _session.Dispose();
}
