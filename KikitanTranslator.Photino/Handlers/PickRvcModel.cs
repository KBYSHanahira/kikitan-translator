using KikitanTranslator.Utility;
using Serilog;

namespace KikitanTranslator.Photino.Handlers;

/// <summary>Lets the user choose an RVC model (.pth or .onnx) with the system file dialog and stores its path.</summary>
public class PickRvcModel(Connector connector) : IHandler
{
    public Task<string?> OnDataReceived(string data)
    {
        if (connector.WindowHandle == null) return Task.FromResult<string?>(null);

        try
        {
            var current = AppConfig.ConfigObject.RvcModelPath;
            var folder = current.Length != 0 ? Path.GetDirectoryName(current) : null;

            var files = connector.WindowHandle.ShowOpenFile("RVC model", folder, false, [("RVC model", ["*.pth", "*.onnx"])]);
            if (files is { Length: > 0 }) AppConfig.ConfigObject.RvcModelPath = files[0];
        }
        catch (Exception e)
        {
            Log.Error($"[RVC]  Unable to show the file dialog: {e.Message}");
        }

        return Task.FromResult<string?>(null);
    }
}
