using System.Security.Cryptography;
using KikitanTranslator.Utility;
using Serilog;

namespace KikitanTranslator.Base.Rvc;

public record HelperModel(string FileName, string Url, string Sha256, long Size);

/// <summary>
/// The models RVC needs besides the user's voice model, downloaded on first use into the app folder.
/// Each download is checked against the SHA-256 published by Hugging Face before it is used.
/// </summary>
public static class RvcModelFiles
{
    public static readonly HelperModel Rmvpe = new("rmvpe.onnx",
        "https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/rmvpe.onnx",
        "5370e71ac80af8b4b7c793d27efd51fd8bf962de3a7ede0766dac0befa3660fd", 361688443);

    public static readonly HelperModel ContentVec768 = new("vec-768-layer-12.onnx",
        "https://huggingface.co/NaruseMioShirakana/MoeSS-SUBModel/resolve/main/vec-768-layer-12.onnx",
        "b3886e7dff1495cda514f94f4680a7b1261e05d6929f5c764cdb17934b413c2a", 377655729);

    public static readonly HelperModel ContentVec256 = new("vec-256-layer-9.onnx",
        "https://huggingface.co/NaruseMioShirakana/MoeSS-SUBModel/resolve/main/vec-256-layer-9.onnx",
        "61d0d0598803f74d5a4bcba65ca367571889accc0e86196f0982e8e455d56393", 293348935);

    private static readonly HttpClient HttpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    public static string Folder
    {
        get
        {
            var folder = Path.Combine(AppConfig.GetAppFolder(), "rvc");
            Directory.CreateDirectory(folder);

            return folder;
        }
    }

    public static HelperModel ContentVecFor(int channels) => channels == 256 ? ContentVec256 : ContentVec768;

    public static string PathOf(HelperModel model) => Path.Combine(Folder, model.FileName);

    // The hash is checked once, right after the download; later launches only compare the size.
    public static bool IsPresent(HelperModel model) =>
        File.Exists(PathOf(model)) && new FileInfo(PathOf(model)).Length == model.Size;

    /// <param name="progress">Receives the downloaded fraction, 0 to 1</param>
    public static async Task EnsureAsync(HelperModel model, IProgress<double>? progress, CancellationToken token)
    {
        if (IsPresent(model)) return;

        var target = PathOf(model);
        var partial = target + ".part";

        Log.Information($"[RVC]  Downloading {model.FileName} ({model.Size / 1_000_000} MB)");

        using (var response = await HttpClient.GetAsync(model.Url, HttpCompletionOption.ResponseHeadersRead, token))
        {
            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(token);
            await using var file = File.Create(partial);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            var buffer = new byte[1 << 20];
            long received = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, token)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), token);
                hash.AppendData(buffer, 0, read);

                received += read;
                progress?.Report((double)received / model.Size);
            }

            var actual = Convert.ToHexString(hash.GetHashAndReset());
            if (!actual.Equals(model.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                file.Close();
                File.Delete(partial);

                throw new InvalidDataException($"{model.FileName} failed the integrity check (SHA-256 {actual})");
            }
        }

        File.Move(partial, target, true);

        Log.Information($"[RVC]  Downloaded and verified {model.FileName}");
    }
}
