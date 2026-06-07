using System.Runtime.InteropServices;
using TabletSignGetterLib.Exceptions;
using TabletSignGetterLib.Models;
using TabletSignGetterLib.Utilities;

namespace TabletSignGetterLib.Services;

public class SignalService
{
    private SignalCriticalValues _criticalValues = new();
    private TabletDevice? _currentTablet;
    private ButtonsData _buttonsData = new();
    
    private static readonly Dictionary<IntPtr, IntPtr> PreparsedHidData = new Dictionary<IntPtr, IntPtr>();

    public bool IsBlocked { get; private set; } = true;

    public void Reset()
    {
        _criticalValues = new();
        _buttonsData = new();
    }
    
    public void ClearCache()
        => PreparsedHidData.Clear();

    public void Block()
    {
        IsBlocked = true;
    }

    public void Release()
    {
        IsBlocked = false;
    }
    
    public void SetTablet(TabletDevice tablet)
        => _currentTablet = tablet;
    
    public TabletDevice? GetTablet()
        => _currentTablet;

    public SignalCriticalValues GetCriticalValues()
        => _criticalValues;

    public void SaveCriticalValues(float x, float y)
    {
        if (_criticalValues.MinX > x) _criticalValues.MinX = x;
        else if (_criticalValues.MaxX < x) _criticalValues.MaxX = x;

        if (_criticalValues.MinY > y) _criticalValues.MinY = y;
        else if (_criticalValues.MaxY < y) _criticalValues.MaxY = y;
    }
    
    public IntPtr WindowHookProcess(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != Rih.WM_INPUT || IsBlocked) return IntPtr.Zero;
        
        handled = true;
        handled = ProcessRawInput(lParam);
        return IntPtr.Zero;
    }
    
    private bool ProcessRawInput(IntPtr hRawInput)
    {
        uint dwSize = 0;
        
        // Get buffer size
        Rih.GetRawInputData(hRawInput, Rih.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTHEADER)));

        var buffer = Marshal.AllocHGlobal((int)dwSize);
        try
        {
            if (Rih.GetRawInputData(hRawInput, Rih.RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTHEADER))) != dwSize)
                return false;
            
            var raw = Marshal.PtrToStructure<Rih.RAWINPUT>(buffer);
            var is64Bit = IntPtr.Size == 8;

            var hDevice = raw.header.hDevice;
            if (!IsTargetTablet(hDevice)) return false;
            
            if (raw.header.dwType == Rih.RIM_TYPEMOUSE)
            {
                var mouse = is64Bit ? raw.mouse64 : raw.mouse32;
                HandleMouseInput(mouse);
                return true;
            }
            else if (raw.header.dwType == Rih.RIM_TYPEHID)
            {
                var hid = is64Bit ? raw.hid64 : raw.hid32;

                var rawBytes = new byte[hid.dwSizeHid];
                
                var headerOffset = is64Bit ? 24 : 16;
                var dataStartPtr = IntPtr.Add(buffer, headerOffset + 8);

                Marshal.Copy(dataStartPtr, rawBytes, 0, (int)hid.dwSizeHid);
                HandleHidInput(hDevice, rawBytes);
                return true;
            }

            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void HandleMouseInput(Rih.RAWMOUSE mouse)
    {
        GetterManager.HandleDataReceived(CheckMouseDataButtons(ParseMouseData(mouse)));
    }
    
    private void HandleHidInput(IntPtr hDevice, byte[] data)
    {
        GetterManager.HandleDataReceived(ParseHidData(hDevice, data));
    }
    
    private TabletData CheckMouseDataButtons(TabletDataRaw raw)
    {
        _buttonsData.Tip = raw.TipPressed || !raw.TipUnPressed && _buttonsData.Tip;
        _buttonsData.Button1 = raw.Button1Pressed || !raw.Button1UnPressed && _buttonsData.Button1;
        _buttonsData.Button2 = raw.Button2Pressed || !raw.Button2UnPressed && _buttonsData.Button2;

        return new TabletData
        {
            X = raw.X,
            Y = raw.Y,
            Button1 = _buttonsData.Button1,
            Button2 = _buttonsData.Button2,
            Tip = _buttonsData.Tip,
        };
    }
    
    private TabletDataRaw ParseMouseData(Rih.RAWMOUSE mouse)
    {
        if (_currentTablet == null)
            throw new TabletNotSelectedException("No tablet selected");
        
        var data = new TabletDataRaw(); 

        data.X = mouse.lLastX / (_currentTablet.GetMaxX() * 1.0f);
        data.Y = mouse.lLastY / (_currentTablet.GetMaxY() * 1.0f);

        var flags = mouse.usButtonFlags;

        if ((flags & 0x0001) != 0) data.TipPressed = true;      // RI_MOUSE_LEFT_BUTTON_DOWN
        if ((flags & 0x0002) != 0) data.TipUnPressed = true;    // RI_MOUSE_LEFT_BUTTON_UP

        if ((flags & 0x0004) != 0) data.Button1Pressed = true;  // RI_MOUSE_RIGHT_BUTTON_DOWN
        if ((flags & 0x0008) != 0) data.Button1UnPressed = true;// RI_MOUSE_RIGHT_BUTTON_UP

        if ((flags & 0x0010) != 0) data.Button2Pressed = true;  // RI_MOUSE_MIDDLE_BUTTON_DOWN
        if ((flags & 0x0020) != 0) data.Button2UnPressed = true;// RI_MOUSE_MIDDLE_BUTTON_UP

        return data;
    }

    private TabletData ParseHidData(IntPtr hDevice, byte[] data)
    {
        if (_currentTablet == null) throw new TabletNotSelectedException("No tablet selected");
        
        var pPreparsedData = GetPreparsedData(hDevice);
        if (pPreparsedData == IntPtr.Zero) return default;
    
        var result = new TabletData();
        var pinnedReport = GCHandle.Alloc(data, GCHandleType.Pinned);
        
        try
        {
            var pReport = pinnedReport.AddrOfPinnedObject();
            var reportLen = (uint)data.Length;
    
            Rih.HidP_GetUsageValue(0, 0x01, 0, 0x30, out uint x, pPreparsedData, pReport, reportLen);
            Rih.HidP_GetUsageValue(0, 0x01, 0, 0x31, out uint y, pPreparsedData, pReport, reportLen);
            Rih.HidP_GetUsageValue(0, 0x0D, 0, 0x30, out uint pressure, pPreparsedData, pReport, reportLen);
    
            result.X = (float)x / _currentTablet.GetMaxX(); 
            result.Y = (float)y / _currentTablet.GetMaxY();
            result.Pressure = (float)pressure / _currentTablet.GetMaxPressure();
    
            var activeUsages = new ushort[16];
            
            var usageLen = (uint)activeUsages.Length;
            Rih.HidP_GetUsages(0, 0x0D, 0, activeUsages, ref usageLen, pPreparsedData, pReport, reportLen);
            for (int i = 0; i < usageLen; i++)
            {
                if (activeUsages[i] == 0x42) result.Tip = true;
                if (activeUsages[i] == 0x44) result.Button1 = true;
                if (activeUsages[i] == 0x5A) result.Button2 = true;
            }
        }
        finally
        {
            pinnedReport.Free();
        }
    
        return result;
    }
    
    private static IntPtr GetPreparsedData(IntPtr hDevice)
    {
        if (PreparsedHidData.TryGetValue(hDevice, out IntPtr cached)) return cached;

        uint size = 0;
        Rih.GetRawInputDeviceInfo(hDevice, Rih.RIDI_PREPARSEDDATA, IntPtr.Zero, ref size);
        if (size == 0) return IntPtr.Zero;

        IntPtr pData = Marshal.AllocHGlobal((int)size);
        Rih.GetRawInputDeviceInfo(hDevice, Rih.RIDI_PREPARSEDDATA, pData, ref size);
        
        PreparsedHidData[hDevice] = pData;
        return pData;
    }
    
    private bool IsTargetTablet(IntPtr hDevice)
    {
        var (vid,pid) = DeviceFilter.GetIdsFromPath(DeviceFilter.GetDevicePath(hDevice));
        if (vid is null || pid is null) return false;

        return _currentTablet?.VendorId == vid.Value
               && _currentTablet.ProductId == pid.Value;
    }
}