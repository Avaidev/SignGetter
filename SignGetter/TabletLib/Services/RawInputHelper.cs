using System.Runtime.InteropServices;

namespace TabletLib.Services;

internal static class RawInputHelper
{
    public const ushort HID_USAGE_PAGE_DIGITIZER = 0x0D;
    public const ushort HID_USAGE_DIGITIZER = 0x01;
    public const int WM_INPUT = 0x00FF;
    public const uint RIDEV_INPUTSINK = 0x00000100;
    public const uint RIDEV_REMOVE = 0x00000001;
    public const uint RID_HEADER = 0x10000005;
    public const uint RID_INPUT = 0x10000003;
    public const uint RIDEV_NOLEGACY = 0x00000030;
    public const uint RIM_TYPEHID = 2;
    
    
    #region Dll Imports
    [DllImport("user32.dll")]
    public static extern bool RegisterRawInputDevices(
        [MarshalAs(UnmanagedType.LPArray)] RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices, uint cbSize);
    
    [DllImport("user32.dll")]
    public static extern uint GetRawInputData(
        IntPtr hRawInput, uint uiCommand, 
        out RAWINPUT pData, ref uint pcbSize, uint cbSizeHeader);
    
    [DllImport("user32.dll")]
    public static extern uint GetRawInputData(
        IntPtr hRawInput, uint uiCommand, 
        IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
   
    [DllImport("user32.dll")]
    public static extern uint GetRawinputDeviceInfo(
        IntPtr hRawInput, uint uiCommand, 
        IntPtr pData, ref uint pcbSize);
    #endregion
   
    #region RawInput Structs
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;

        public bool IsHid => dwType == RIM_TYPEHID;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWHID
    {
        public uint dwSizeHid;
        public uint dwCount;
        public IntPtr bRawData;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RAWINPUT
    {
        [FieldOffset(0)]
        public RAWINPUTHEADER header;
    
        [FieldOffset(24)]
        public RAWHID hid;
    }
    #endregion
}