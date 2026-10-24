using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using Serilog;
using SoundFlow.Structs;

namespace KikitanTranslator.Capture;

/// <summary>
/// SoundFlow 1.4.x numbers MiniAudioBackend one higher than miniaudio's ma_backend, so asking for
/// Wasapi actually opens the DirectSound backend. DirectSound reports device names in the ANSI code
/// page, which SoundFlow decodes as UTF-8, so non-ASCII names (e.g. Japanese) come out garbled.
/// DirectSound device IDs are the WASAPI endpoint GUIDs, so the real names are looked up from Core Audio.
/// Playback devices have the same problem, so pass <see cref="DataFlow.Render"/> for those.
/// </summary>
public static class CaptureDeviceNames
{
    public static DeviceInfo[] Fix(DeviceInfo[] devices, DataFlow flow = DataFlow.Capture)
    {
        if (!OperatingSystem.IsWindows() || devices.Length == 0) return devices;

        try
        {
            using var enumerator = new MMDeviceEnumerator();

            var names = new Dictionary<Guid, string>();
            foreach (var endpoint in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
            {
                if (TryParseEndpointGuid(endpoint.ID, out var guid)) names[guid] = endpoint.FriendlyName;
                endpoint.Dispose();
            }

            Guid? defaultGuid = null;
            if (enumerator.HasDefaultAudioEndpoint(flow, Role.Console))
            {
                using var defaultEndpoint = enumerator.GetDefaultAudioEndpoint(flow, Role.Console);
                if (TryParseEndpointGuid(defaultEndpoint.ID, out var guid)) defaultGuid = guid;
            }

            var result = new List<DeviceInfo>();
            foreach (var device in devices)
            {
                var guid = ReadDeviceGuid(device);

                // The all-zero GUID is DirectSound's "Primary Sound Capture/Playback Driver", an alias of the system
                // default device. It is dropped and the real default device is flagged instead.
                if (guid == Guid.Empty) continue;

                if (!names.TryGetValue(guid, out var name))
                {
                    result.Add(device);
                    continue;
                }

                result.Add(device with { Name = name, IsDefault = guid == defaultGuid });
            }

            // Callers rely on exactly one default device; keep SoundFlow's list if that can't be guaranteed.
            if (result.Count(d => d.IsDefault) != 1) return devices;

            return result.ToArray();
        }
        catch (Exception e)
        {
            Log.Warning($"[MIC]  Unable to resolve {(flow == DataFlow.Render ? "playback" : "capture")} device names: {e.Message}");

            return devices;
        }
    }

    private static Guid ReadDeviceGuid(DeviceInfo device)
    {
        if (device.Id == IntPtr.Zero) return Guid.Empty;

        var bytes = new byte[16];
        Marshal.Copy(device.Id, bytes, 0, bytes.Length);

        return new Guid(bytes);
    }

    // Endpoint IDs look like "{0.0.1.00000000}.{9917af94-7949-4b8a-8657-f32a856a09cd}".
    private static bool TryParseEndpointGuid(string endpointId, out Guid guid)
    {
        var start = endpointId.LastIndexOf('{');

        return Guid.TryParse(start < 0 ? endpointId : endpointId[start..], out guid);
    }
}
