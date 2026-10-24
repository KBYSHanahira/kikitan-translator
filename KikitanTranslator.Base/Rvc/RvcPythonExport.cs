using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Serilog;

namespace KikitanTranslator.Base.Rvc;

public delegate void OnExportStatus(string state, string detail, double progress);

/// <summary>
/// Turns an RVC .pth model into ONNX with Rvc/Python/export_rvc.py. The first time, a private Python 3.12 (the official
/// NuGet build) and CPU-only PyTorch are installed into the app folder; nothing touches a Python already on the PC.
/// Converted models are cached by the hash of the .pth file and of the export scripts, so choosing the same model again
/// is instant and a fixed export script reconverts.
/// </summary>
public static class RvcPythonExport
{
    private static readonly HelperModel PythonPackage = new("python.3.12.10.nupkg",
        "https://api.nuget.org/v3-flatcontainer/python/3.12.10/python.3.12.10.nupkg",
        "0eb85c2dfccccf1b17352de4c397f69194035b7d37149eacc16f1147d93de3b8", 14515433);

    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromMinutes(10);

    private static string ScriptFolder => Path.Combine(AppContext.BaseDirectory, "Rvc", "Python");
    private static string PythonHome => Path.Combine(RvcModelFiles.Folder, "python");
    private static string PythonExe => Path.Combine(PythonHome, "tools", "python.exe");
    // Holds the hash of the requirements.txt that was installed, so a changed lock file reinstalls the packages.
    private static string PackagesMarker => Path.Combine(PythonHome, "kikitan-requirements.sha256");

    public static bool IsPthModel(string path) => Path.GetExtension(path).Equals(".pth", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns the path of the converted ONNX model, converting (and installing Python) when needed.</summary>
    public static async Task<string> ConvertAsync(string pthPath, OnExportStatus status, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Converting .pth models is only supported on Windows. Export the model to ONNX with RVC WebUI instead.");

        var cacheFolder = Path.Combine(RvcModelFiles.Folder, "converted");
        Directory.CreateDirectory(cacheFolder);

        string hash;
        using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            sha.AppendData(await File.ReadAllBytesAsync(pthPath, token));
            foreach (var script in Directory.GetFiles(ScriptFolder, "*.py", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                sha.AppendData(await File.ReadAllBytesAsync(script, token));

            hash = Convert.ToHexString(sha.GetHashAndReset())[..16];
        }

        var target = Path.Combine(cacheFolder, $"{Path.GetFileNameWithoutExtension(pthPath)}-{hash.ToLowerInvariant()}.onnx");
        if (File.Exists(target)) return target;

        await EnsurePythonAsync(status, token);

        status("converting", Path.GetFileName(pthPath), 0);
        Log.Information($"[RVC]  Converting {Path.GetFileName(pthPath)} to ONNX");

        var (exitCode, output) = await RunAsync(PythonExe, ["-I", "-B", Path.Combine(ScriptFolder, "export_rvc.py"), pthPath, target], ExportTimeout, token);
        if (exitCode != 0 || !File.Exists(target))
            throw new InvalidDataException(LastLine(output) ?? $"The conversion failed (exit code {exitCode})");

        Log.Information($"[RVC]  {LastLine(output)}");

        return target;
    }

    private static async Task EnsurePythonAsync(OnExportStatus status, CancellationToken token)
    {
        if (!File.Exists(PythonExe))
        {
            status("installing", "Python", 0);
            await RvcModelFiles.EnsureAsync(PythonPackage, new Progress<double>(p => status("installing", "Python", p)), token);

            var package = RvcModelFiles.PathOf(PythonPackage);
            var staging = PythonHome + ".part";
            if (Directory.Exists(staging)) Directory.Delete(staging, true);

            using (var zip = ZipFile.OpenRead(package))
            {
                var root = Path.GetFullPath(staging) + Path.DirectorySeparatorChar;
                foreach (var entry in zip.Entries)
                {
                    if (!entry.FullName.StartsWith("tools/") || entry.FullName.EndsWith('/')) continue;

                    var destination = Path.GetFullPath(Path.Combine(staging, entry.FullName));
                    if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;

                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    entry.ExtractToFile(destination, true);
                }
            }

            if (Directory.Exists(PythonHome)) Directory.Delete(PythonHome, true);
            Directory.Move(staging, PythonHome);
            File.Delete(package);

            Log.Information("[RVC]  Installed Python for model conversion");
        }

        var requirements = Path.Combine(ScriptFolder, "requirements.txt");
        var requirementsHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(requirements, token)));
        if (File.Exists(PackagesMarker) && (await File.ReadAllTextAsync(PackagesMarker, token)).Trim() == requirementsHash) return;

        status("installing", "PyTorch", 0);
        Log.Information("[RVC]  Installing PyTorch for model conversion");

        var (exitCode, output) = await RunAsync(PythonExe,
        [
            "-I", "-m", "pip", "install", "--disable-pip-version-check", "--no-warn-script-location",
            "--only-binary=:all:", "--require-hashes", "--no-deps",
            "--index-url", "https://download.pytorch.org/whl/cpu", "--extra-index-url", "https://pypi.org/simple",
            "-r", requirements
        ], InstallTimeout, token);

        if (exitCode != 0) throw new InvalidOperationException($"Installing PyTorch failed: {LastLine(output)}");

        await File.WriteAllTextAsync(PackagesMarker, requirementsHash, token);

        Log.Information("[RVC]  Installed PyTorch for model conversion");
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(string fileName, string[] arguments, TimeSpan timeout, CancellationToken token)
    {
        var info = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        info.Environment["PYTHONUTF8"] = "1";

        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Unable to start {fileName}");
        var stdout = process.StandardOutput.ReadToEndAsync(token);
        var stderr = process.StandardError.ReadToEndAsync(token);

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);
        limit.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            if (token.IsCancellationRequested) throw;

            throw new TimeoutException($"{Path.GetFileName(fileName)} did not finish within {timeout.TotalMinutes} minutes");
        }

        var output = (await stdout) + (await stderr);
        Log.Debug($"[RVC]  {Path.GetFileName(fileName)} exited with {process.ExitCode}: {output.Trim()}");

        return (process.ExitCode, output);
    }

    private static string? LastLine(string output) =>
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
}
