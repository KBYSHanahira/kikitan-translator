using Microsoft.ML.OnnxRuntime;
using Serilog;

namespace KikitanTranslator.Base.Rvc;

internal static class OnnxSessions
{
    /// <summary>Opens the model on the GPU through DirectML when useGpu is set and possible, otherwise on the CPU.</summary>
    public static InferenceSession Create(string path, bool useGpu, out bool onGpu)
    {
        if (useGpu && OperatingSystem.IsWindows())
        {
            try
            {
                // DirectML does not support memory patterns or parallel execution.
                var options = new SessionOptions { EnableMemoryPattern = false, ExecutionMode = ExecutionMode.ORT_SEQUENTIAL };
                options.AppendExecutionProvider_DML(0);

                onGpu = true;

                return new InferenceSession(path, options);
            }
            catch (Exception e)
            {
                Log.Warning($"[RVC]  DirectML is not available for {Path.GetFileName(path)}, using the CPU: {e.Message}");
            }
        }

        onGpu = false;

        return new InferenceSession(path);
    }
}
