using System.Runtime.InteropServices;

namespace TabletSignGetterLib.Utilities;

public static class Rih //RawInputHelper
{
    public const ushort HID_USAGE_PAGE_GENERIC = 0x01;
    public const ushort HID_USAGE_PEN = 0x02;
    
    public const uint RIDI_DEVICENAME = 0x20000007;
    
    public const int WM_INPUT = 0x00FF;
    
    public const uint RIDEV_INPUTSINK = 0x00000100;
    public const uint RIDEV_REMOVE = 0x00000001;
    public const uint RID_HEADER = 0x10000005;
    public const uint RID_INPUT = 0x10000003;
    public const int RIM_TYPEMOUSE = 0;

    #region Dll Imports
    [DllImport("user32.dll")]
    public static extern bool RegisterRawInputDevices(
        [MarshalAs(UnmanagedType.LPArray)] RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices, uint cbSize);
    
    [DllImport("user32.dll")]
    public static extern uint GetRawInputData(
        IntPtr hRawInput, uint uiCommand, 
        IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
    
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize);  
    
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
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWMOUSE
    {
        public ushort usFlags;
        public ushort usButtonFlags;
        public ushort usButtonData;
        public uint ulRawButtons;
        public int lLastX;
        public int lLastY;
        public uint ulExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUT
    {
        public RAWINPUTHEADER header;
        public RAWMOUSE mouse;
    }

    #endregion
}