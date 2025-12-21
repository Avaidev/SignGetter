using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using DataLayer;
using HidSharp;
using TabletLib.Models;
using TabletLib.Utilities;
using DeviceFilter = TabletLib.Utilities.DeviceFilter;

namespace TabletLib.Services;

public class TabletManager : IDisposable
{
    public enum TabletStatus
    {
        NotSelected,
        NotRegistered,
        Finding,
        Captured,
        ReSelected
    }
    private HidDeviceInfo? _selectedTablet;
    private TabletStatus _currentStatus = TabletStatus.NotSelected;
    private HwndSource? _hwndSource;
    private IntPtr _targetHandler = IntPtr.Zero;
    private IntPtr _targetWindow;
    private bool _disposed = false;
    private bool _hooked = false;
    
    #region Events
    public event Action<TabletStatus>? OnStatusChanged;
    public event Action<TabletData>? OnDataReceived;
    public event Action<string>? OnErrorMessage;
    public event Action<string>? OnWarningMessage;
    #endregion

    #region TabletInfo
    public string TabletName => _selectedTablet?.DeviceName ?? "Unknown";
    public string TabletManufacturer => _selectedTablet?.Manufacturer ?? "Unknown";
    public int TabletVid => _selectedTablet?.VendorID ?? 0;
    public int TabletPid => _selectedTablet?.ProductID ?? 0;
    public TabletStatus CurrentStatus
    {
        get => _currentStatus;
        set
        {
            if (_currentStatus != value)
            {
                _currentStatus = value;
                OnStatusChanged?.Invoke(value);
            }
        }
    }
    #endregion

    #region Initializing
    public TabletManager(Window window)
    {
        _targetWindow = new WindowInteropHelper(window).Handle;
        if (_targetWindow == IntPtr.Zero)
        {
            window.SourceInitialized += OnWindowInit;
        }
        else SetHook();
    }

    public TabletManager(Window window, Action<TabletData> callback) : this(window)
    {
        OnDataReceived += callback;
    }
    
    private void OnWindowInit(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.SourceInitialized -= OnWindowInit;
            _targetWindow = new WindowInteropHelper(window).Handle;
            SetHook();
        }
    }

    private void SetHook()
    {
        if (_targetWindow == IntPtr.Zero) throw new InvalidOperationException("[TabletManager] Window is not available");
        _hwndSource = HwndSource.FromHwnd(_targetWindow);
      
        if (_hwndSource == null) throw new InvalidOperationException("[TabletManager] Invalid getting HWND from window");
      
        _hwndSource.AddHook(WindowHookProcess);
        _hooked = true;
    }

    #endregion
    
    #region Registration
    private bool RegisterTablet()
    {
        if (CurrentStatus != TabletStatus.NotRegistered)
        {
            OnWarningMessage?.Invoke("Cant register tablet, unregister first");
            return false;
        }

        var rid = new Rih.RAWINPUTDEVICE
        {
            usUsagePage = Rih.HID_USAGE_PAGE_GENERIC,
            usUsage = Rih.HID_USAGE_PEN,
            dwFlags = Rih.RIDEV_INPUTSINK,
            hwndTarget = _targetWindow
        };

        if (!Rih.RegisterRawInputDevices([rid], 1, (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTDEVICE)))) return false;

        CurrentStatus = TabletStatus.Finding;
        return true;
    }

    private void UnregisterTablet()
    {
        var rid = new Rih.RAWINPUTDEVICE
        {
            usUsagePage = Rih.HID_USAGE_PAGE_GENERIC,
            usUsage = Rih.HID_USAGE_PEN,
            dwFlags = Rih.RIDEV_REMOVE,
            hwndTarget = IntPtr.Zero
        };

        Rih.RegisterRawInputDevices([rid], 1, (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTDEVICE)));
        CurrentStatus = TabletStatus.NotRegistered;
    }
    #endregion
    
    #region Message Processing
    private IntPtr WindowHookProcess(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != Rih.WM_INPUT) return IntPtr.Zero;
        
        if (CurrentStatus == TabletStatus.ReSelected)
        {
            _targetHandler = IntPtr.Zero;
            CurrentStatus = TabletStatus.Finding;
        }

        handled = true;
        handled = ProcessRawInput(lParam);
        return IntPtr.Zero;
    }
    
    private bool IsTargetTablet(IntPtr hDevice)
    {
        if (_targetHandler != IntPtr.Zero) return hDevice == _targetHandler;
        
        var (vid,pid) = DeviceFilter.GetIdsFromPath(DeviceFilter.GetDevicePath(hDevice));
        if (vid is null || pid is null) return false;

        return _selectedTablet?.VendorID == vid.Value
               && _selectedTablet?.ProductID == pid.Value;
    }

    private bool ProcessRawInput(IntPtr lParam)
    {
        uint dataSize = 0;
        var headerSize = (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTHEADER));
      
        var result = Rih.GetRawInputData(lParam, Rih.RID_INPUT, IntPtr.Zero, ref dataSize, headerSize);

        if (result == 0 && dataSize == 0)
        {
            Console.WriteLine("[TabletManager] Failed to read input size: {0}", Marshal.GetLastWin32Error());
            return false;
        }
      
        var buffer = Marshal.AllocHGlobal((int)dataSize);
        try
        {
            if (Rih.GetRawInputData(lParam, Rih.RID_INPUT, buffer, ref dataSize, headerSize) != dataSize)
            {
                Console.WriteLine("[TabletManager] Failed to read input data: {0}", Marshal.GetLastWin32Error());
                return false;
            }
         
            var raw = Marshal.PtrToStructure<Rih.RAWINPUT>(buffer);
         
            var hDevice = raw.header.hDevice;
            if (!IsTargetTablet(hDevice) || raw.header.dwType != Rih.RIM_TYPEMOUSE) return false;
        
            if (_targetHandler == IntPtr.Zero)
            {
                _targetHandler = hDevice;
                CurrentStatus = TabletStatus.Captured;
            }
            
            var mouse = raw.mouse;
            OnDataReceived?.Invoke(ParseData(mouse));
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private TabletData ParseData(Rih.RAWMOUSE mouse)
    {
        var data = new TabletData();
        if (_selectedTablet == null) return data;
        data.X = mouse.lLastX / (_selectedTablet.GetMaxX() * 1.0f);
        data.Y = mouse.lLastY / (_selectedTablet.GetMaxY() * 1.0f);
        
        var btnData = mouse.usButtonData;
        if (btnData % 2 != 0)
        {
            data.TipPressed = true;
            btnData--;
        }
        if (btnData == 0) return data;
        
        btnData -= 2;
        if (new ushort[] { 4, 8, 16, 32 }.Contains(btnData)) data.TipUnPressed = true;
        else btnData += 2;

        switch (btnData)
        {
            case 2:
                data.TipUnPressed = true;
                break;
            case 4:
                data.Button1Pressed = true;
                break;
            case 8:
                data.Button1UnPressed = true;
                break;
            case 16:
                data.Button2Pressed = true;
                break;
            case 32:
                data.Button2UnPressed = true;
                break;
        }

        return data;
    }
    #endregion
    
    #region Preparing
    public static List<HidDeviceInfo> GetDevices()
    {
        return DeviceList.Local.GetHidDevices().Where(IsSimilarToTablet)
            .Select(d => new HidDeviceInfo(d)).ToList();
    }
    
    private static bool IsSimilarToTablet(HidDevice device)
    {
        bool isDigitizer = device.GetReportDescriptor() != null 
                           && device.GetReportDescriptor().DeviceItems.SelectMany(item => item.Usages.GetAllValues()).Any(usage => (ushort)(usage >> 16) == 0x0D);

        if (!isDigitizer) return false;
        
        string[] internalKeywords = 
        {
            "HIDI2C", "TouchPad", "Touchpad", "Synaptics", "ELAN", 
            "Precision Touchpad", "Touchscreen", "TrackPad", "Trackpad",
            "I2C", "HID Compliant Mouse", "PS/2", "USB Input Device"
        };

        foreach (var keyword in internalKeywords)
        {
            if (device.GetProductName().Contains(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    public bool SelectTablet(HidDeviceInfo? tablet = null)
    {
        var hidDevices = GetDevices();
        var selected = tablet != null ? SelectManually(hidDevices, tablet) : SelectAuto(hidDevices);
        if (!selected)
        {
            OnWarningMessage?.Invoke("Error selecting the tablet");
            return false;
        }

        CurrentStatus = CurrentStatus is TabletStatus.NotSelected
            ? TabletStatus.NotRegistered
            : TabletStatus.ReSelected;
        return true;
    }
    
    private bool SelectManually(List<HidDeviceInfo> hidDevices, HidDeviceInfo tablet)
    {
        _selectedTablet =
            hidDevices.FirstOrDefault(d => d.Equals(tablet));
        if (_selectedTablet == null) return false;
      
        AppConfigManager.AppSettings.LastDeviceVID = _selectedTablet.VendorID;
        AppConfigManager.AppSettings.LastDevicePID = _selectedTablet.ProductID;
        AppConfigManager.AppSettings.LastDeviceName = _selectedTablet.DeviceName;
        AppConfigManager.SaveAppSettings();
        return true;
    }

    private bool SelectAuto(List<HidDeviceInfo> hidDevices)
    {
        var appSettings = AppConfigManager.AppSettings;
        if (appSettings.LastDeviceVID == 0 || appSettings.LastDevicePID == 0) return false;
        
        _selectedTablet = hidDevices.FirstOrDefault(d => d.VendorID != appSettings.LastDeviceVID || d.ProductID != appSettings.LastDevicePID || d.DeviceName.Contains(appSettings.LastDeviceName, StringComparison.OrdinalIgnoreCase));
        return _selectedTablet != null;
    }
    #endregion

    public async Task<bool> StartAsync()
    {
        if (_selectedTablet == null)
        {
            OnErrorMessage?.Invoke("Select the tablet first");
            return false;
        }

        var attempts = 10;
        while (_targetWindow == IntPtr.Zero && attempts-- > 0)
        {
            await Task.Delay(1000);
        }

        if (attempts < 0)
        {
            OnErrorMessage?.Invoke("Failed to find a target window");
            return false;
        }

        if (!RegisterTablet())
        {
            OnErrorMessage?.Invoke("Failed to register the tablet");
            return false;
        }

        return true;
    }

    public void Stop()
    {
        UnregisterTablet();
        _targetHandler = IntPtr.Zero;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hooked && _hwndSource != null)
        {
            _hwndSource.RemoveHook(WindowHookProcess);
            _hwndSource.Dispose();
            _hooked = false;
        }
        UnregisterTablet();
    }
}