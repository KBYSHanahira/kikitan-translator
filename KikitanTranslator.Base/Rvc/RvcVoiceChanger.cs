using KikitanTranslator.Utility;
using Newtonsoft.Json;
using Serilog;

namespace KikitanTranslator.Base.Rvc;

public class RvcStatus
{
    // off, installing (Python/PyTorch for .pth models), converting (.pth to ONNX), downloading, loading, ready or error
    [JsonProperty("state")] public string State = "off";
    // Download progress, 0 to 1 (installing PyTorch and converting report no progress)
    [JsonProperty("progress")] public double Progress;
    // Model summary when ready, what is installed, converted or downloaded while busy, message on error
    [JsonProperty("detail")] public string Detail = "";
}

/// <summary>
/// Keeps the configured RVC model loaded. Loading takes seconds (and a download the first time), so it runs in the
/// background and survives pipeline restarts; until it is ready, speech is played unconverted. A .pth model is
/// converted to ONNX first (see RvcPythonExport).
/// </summary>
public sealed class RvcVoiceChanger
{
    private readonly object _lock = new();
    private RvcConverter? _converter;
    // The configured path and device the converter was loaded with, as "path|gpu" or "path|cpu" (the path is
    // the .pth for converted models, unlike RvcConverter.ModelPath). The _loadingPath and _failedPath keys use the same form.
    private string? _loadedPath;
    private string? _loadingPath;
    // A model that failed is not retried on every config change, only after RVC is turned off and on again.
    private string? _failedPath;
    private CancellationTokenSource? _loading;

    public RvcStatus Status { get; private set; } = new();
    public event Action? StatusChanged;

    /// <summary>Starts loading the configured model if it is not loaded yet, or unloads it when RVC is off.</summary>
    public void Refresh()
    {
        var path = AppConfig.ConfigObject.RvcModelPath;

        if (!AppConfig.ConfigObject.RvcEnabled || path.Length == 0)
        {
            Unload();
            _failedPath = null;
            SetStatus("off");

            return;
        }

        var useGpu = AppConfig.ConfigObject.RvcUseGpu;
        var key = $"{path}|{(useGpu ? "gpu" : "cpu")}";

        CancellationTokenSource loading;
        lock (_lock)
        {
            if (_loadedPath == key || _loadingPath == key || _failedPath == key) return;

            UnloadLocked();
            _loadingPath = key;
            _loading = loading = new CancellationTokenSource();
        }

        Task.Run(() => Load(path, useGpu, key, loading.Token));
    }

    /// <summary>Converts 48 kHz mono audio, or returns null when no model is ready.</summary>
    public float[]? TryConvert(float[] audio, int sampleRate)
    {
        lock (_lock)
        {
            if (_converter == null) return null;

            var timer = System.Diagnostics.Stopwatch.StartNew();
            var result = _converter.Convert(audio, sampleRate, AppConfig.ConfigObject.RvcTranspose);

            Log.Debug($"[RVC]  Converted {(double)audio.Length / sampleRate:F2}s of audio in {timer.ElapsedMilliseconds}ms");

            return result;
        }
    }

    private async Task Load(string path, bool useGpu, string key, CancellationToken token)
    {
        try
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"{path} does not exist");

            var onnxPath = path;
            if (RvcPythonExport.IsPthModel(path))
                onnxPath = await RvcPythonExport.ConvertAsync(path, (state, detail, progress) => SetStatus(state, progress, detail), token);

            SetStatus("loading");
            var contentVec = RvcModelFiles.ContentVecFor(RvcConverter.FeatureChannels(onnxPath));

            foreach (var model in new[] { contentVec, RvcModelFiles.Rmvpe })
            {
                if (RvcModelFiles.IsPresent(model)) continue;

                SetStatus("downloading", 0, model.FileName);
                await RvcModelFiles.EnsureAsync(model, new Progress<double>(p => SetStatus("downloading", p, model.FileName)), token);
            }

            SetStatus("loading");
            var converter = new RvcConverter(onnxPath, RvcModelFiles.PathOf(contentVec), RvcModelFiles.PathOf(RvcModelFiles.Rmvpe), useGpu);

            lock (_lock)
            {
                if (token.IsCancellationRequested)
                {
                    converter.Dispose();

                    return;
                }

                _converter = converter;
                _loadedPath = key;
                _loadingPath = null;
            }

            SetStatus("ready", 1, $"{converter.Version} · {converter.SampleRate / 1000} kHz · {(converter.OnGpu ? "GPU" : "CPU")}");
        }
        catch (OperationCanceledException)
        {
            // Another model was chosen or RVC was turned off
        }
        catch (Exception e)
        {
            Log.Error($"[RVC]  Unable to load {path}: {e.Message}");

            lock (_lock)
            {
                if (_loadingPath == key) _loadingPath = null;
                _failedPath = key;
            }

            if (!token.IsCancellationRequested) SetStatus("error", 0, e.Message);
        }
    }

    private void Unload()
    {
        lock (_lock) UnloadLocked();
    }

    private void UnloadLocked()
    {
        _loading?.Cancel();
        _loading = null;
        _loadingPath = null;

        _converter?.Dispose();
        _converter = null;
        _loadedPath = null;
    }

    private void SetStatus(string state, double progress = 0, string detail = "")
    {
        // Progress reports arrive for every megabyte; only whole percents are worth a UI update.
        if (Status.State == state && Status.Detail == detail && Math.Floor(Status.Progress * 100) == Math.Floor(progress * 100)) return;

        Status = new RvcStatus { State = state, Progress = progress, Detail = detail };
        StatusChanged?.Invoke();
    }
}
