using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TabletSignGetterLib.Utilities;

public static class DeviceFilter
{
    
    public static string? GetDevicePath(IntPtr hDevice)
    {
        uint bufferSize = 0;

        Rih.GetRawInputDeviceInfo(
            hDevice,
            Rih.RIDI_DEVICENAME,
            IntPtr.Zero,
            ref bufferSize);

        
        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize * 2);
        try
        {
            uint result = Rih.GetRawInputDeviceInfo(
                hDevice,
                Rih.RIDI_DEVICENAME,
                buffer,
                ref bufferSize);

            if (result == unchecked((uint)-1)) return null;
            
            return Marshal.PtrToStringUni(buffer, (int)bufferSize);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static (int?, int?) GetIdsFromPath(string? path)
    {
        if (path is null) return (null, null);
        var regex = new Regex(@"vid[&_]([A-Za-z0-9]+)[_&]pid[_&]([A-Za-z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var match = regex.Match(path);
        if (match.Groups.Count < 2) return (null, null);
        try
        {
            var vid = Convert.ToInt32(match.Groups[1].Value, 16);
            var pid = Convert.ToInt32(match.Groups[2].Value, 16);
            return (vid, pid);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DeviceFilter] Exception in converting path to Ids: {0}", ex.Message);
            return (null, null);
        }
    }
}