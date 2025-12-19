using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TabletLib.Utilities;

public static class DeviceFilter
{
    private const uint RIDI_DEVICENAME = 0x20000007;
    
    public static string? GetDevicePath(IntPtr hDevice)
    {
        uint bufferSize = 0;

        GetRawInputDeviceInfo(
            hDevice,
            RIDI_DEVICENAME,
            IntPtr.Zero,
            ref bufferSize);

        if (bufferSize == 0) return null;
        
        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            uint result = GetRawInputDeviceInfo(
                hDevice,
                RIDI_DEVICENAME,
                buffer,
                ref bufferSize);

            if (result == 0 || result == uint.MaxValue) return null;
            
            return Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static (int, int, Guid)? GetIdsFromPath(string path)
    {
        var regex = new Regex(@"vid_(\w+)&pid_(\w+).+{(.+)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var match = regex.Match(path);
        var (vidString, pidString, guidString) = (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
        var vid = 0;
        var pid = 0;
        Guid guid;
        try
        {
            vid = Convert.ToInt32(match.Groups[1].Value, 16);
            pid = Convert.ToInt32(pidString, 16);
            if (!Guid.TryParse(guidString, out guid)) throw new Exception("Invalid GUID");
            return (vid, pid, guid);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception in converting path to Ids: {0}", ex.Message);
            return null;
        }
    }

    #region Dll Imports
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize);   
    #endregion
}