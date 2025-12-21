using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HidSharp;

namespace TabletLib.Utilities;

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
        var regex = new Regex(@"vid_(\w+)&pid_(\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var match = regex.Match(path);
        try
        {
            var vid = Convert.ToInt32(match.Groups[1].Value, 16);
            var pid = Convert.ToInt32(match.Groups[2].Value, 16);
            return (vid, pid);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception in converting path to Ids: {0}", ex.Message);
            return (null, null);
        }
    }
}