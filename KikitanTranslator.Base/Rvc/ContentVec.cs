using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace KikitanTranslator.Base.Rvc;

/// <summary>
/// Speech content features from vec-768-layer-12.onnx (RVC v2) or vec-256-layer-9.onnx (v1), as in RVC's
/// onnx_inference.py: input [1, 1, samples] at 16 kHz, output [1, frames, channels] at 50 frames per second.
/// </summary>
public sealed class ContentVec : IDisposable
{
    private readonly InferenceSession _session;
    public bool OnGpu { get; }

    public ContentVec(string modelPath, bool useGpu = true)
    {
        _session = OnnxSessions.Create(modelPath, useGpu, out var onGpu);
        OnGpu = onGpu;
    }

    /// <summary>Returns [frames, channels] with every frame repeated twice, giving RVC's 100 frames per second.</summary>
    public float[,] Compute(float[] audio16K)
    {
        var input = new DenseTensor<float>(audio16K, [1, 1, audio16K.Length]);

        using var results = _session.Run([NamedOnnxValue.CreateFromTensor(_session.InputMetadata.Keys.First(), input)]);
        var output = results.First().AsTensor<float>();

        var frames = output.Dimensions[1];
        var channels = output.Dimensions[2];
        var features = new float[frames * 2, channels];

        for (var t = 0; t < frames; t++)
            for (var c = 0; c < channels; c++)
            {
                var value = output[0, t, c];
                features[2 * t, c] = value;
                features[2 * t + 1, c] = value;
            }

        return features;
    }

    public void Dispose() => _session.Dispose();
}
