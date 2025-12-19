using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using DataLayer;
using HidSharp;
using TabletLib.Models;
using TabletLib.Models.Profile;
using DeviceFilter = TabletLib.Utilities.DeviceFilter;
// ReSharper disable All

namespace TabletLib.Services;

public class TabletManager : IDisposable
{
   public enum TabletStatus
   {
      NotSelected,
      NotRegistered,
      Captured,
      ReSelected
   }

   private DevicePreview? _selectedTablet;
   private HwndSource? _hwndSource;
   private IntPtr _targetTablet = IntPtr.Zero;
   private IntPtr _targetWindow;
   private TabletStatus _currentStatus = TabletStatus.NotSelected;
   private bool _disposed = false;
   private bool _hooked = false;

   private TabletProfile? _tabletProfile; //TODO Rebuild Profiles
   
   public TabletStatus CurrentStatus => _currentStatus;
   public string TabletName => _selectedTablet?.Name ?? "Unknown";
   public string TabletManufacturer => _selectedTablet?.Manufacturer ?? "Unknown";
   
   

   #region Events
   public event Action<TabletStatus>? OnStatusChanged;
   public event Action<TabletData>? OnDataReceived;
   public event Action<string>? OnErrorMessage;
   public event Action<string>? OnWarningMessage;
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

   #region Tablet Lifetime Process
   private IntPtr WindowHookProcess(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
   {
      if (msg != RawInputHelper.WM_INPUT) return IntPtr.Zero; // TODO Always true
      
      if (_currentStatus == TabletStatus.ReSelected)
      {
         _targetTablet = IntPtr.Zero;
         _currentStatus = TabletStatus.Captured;
      }
      var rawInput = GetRawInput(lParam);
      if (rawInput is null 
          || !rawInput.Value.header.IsHid // TODO Always true
          || !IsTargetDevice(rawInput.Value.header.hDevice)) return IntPtr.Zero; // TODO Always true
      handled = ProcessHid(rawInput.Value.hid);
      return IntPtr.Zero;
   }

   private bool IsTargetDevice(IntPtr hDevice)
   {
      if (_targetTablet != IntPtr.Zero) return hDevice == _targetTablet;
      
      var path = DeviceFilter.GetDevicePath(hDevice);
      if (path is null) return false;
      
      var ids = DeviceFilter.GetIdsFromPath(path);
      if (ids is null) return false;

      if (_selectedTablet?.VendorID == ids.Value.Item1 && _selectedTablet?.ProductID == ids.Value.Item2 &&
          _selectedTablet?.InstanceGuid == ids.Value.Item3)
      {
         _targetTablet = hDevice;
         return true;
      }
      return false;
   }

   private bool ProcessHid(RawInputHelper.RAWHID hid)
   {
      int totalBytes = (int)(hid.dwSizeHid * hid.dwCount);
      byte[] data = new byte[totalBytes];
    
      Marshal.Copy(hid.bRawData, data, 0, totalBytes);

      var tabletData = ParseData(data);
      if (tabletData is null) return false;
      
      OnDataReceived?.Invoke(ParseData(data)!);
      return true;
   }

   private static RawInputHelper.RAWINPUT? GetRawInput(IntPtr lParam)
   {
      uint dataSize = 0;
      uint headerSize = (uint)Marshal.SizeOf(typeof(RawInputHelper.RAWINPUTHEADER));
      
      uint result = RawInputHelper.GetRawInputData(lParam, RawInputHelper.RID_INPUT, IntPtr.Zero, ref dataSize, headerSize);

      if (result == 0 && dataSize == 0)
      {
         int error = Marshal.GetLastWin32Error();
         Console.WriteLine("[TabletManager] Failed to read input size: {0}", error);
         return null;
      }
      
      IntPtr buffer = Marshal.AllocHGlobal((int)dataSize);
      try
      {
         result = RawInputHelper.GetRawInputData(lParam, RawInputHelper.RID_INPUT, buffer, ref dataSize, headerSize);

         if (result == 0 && dataSize == 0)
         {
            int error = Marshal.GetLastWin32Error();
            Console.WriteLine("[TabletManager] Failed to read input data: {0}", error);
            return null;
         }
         
         return Marshal.PtrToStructure<RawInputHelper.RAWINPUT>(buffer);
         
      }
      finally
      {
         Marshal.FreeHGlobal(buffer);
      }
   }

   private TabletData? ParseData(byte[] data)
   {
      if (_tabletProfile == null) return null;
        var tabletData = new TabletData();
        
        // Parse Status
        byte status = data[0];
        tabletData.IsPenDown = (status & (1 << _tabletProfile.Data.StatusMasks.TipSwitch)) != 0;
        tabletData.InRange = (status & (1 << _tabletProfile.Data.StatusMasks.InRange)) != 0;
        tabletData.IsEraser = (status & (1 << _tabletProfile.Data.StatusMasks.Eraser)) != 0;
        tabletData.Button1 = (status & (1 << _tabletProfile.Data.StatusMasks.BarrelButton1)) != 0;
        tabletData.Button2 = (status & (1 << _tabletProfile.Data.StatusMasks.BarrelButton2)) != 0;
        
        //Parse X & Y
        tabletData.X = ParseValue<byte>(
            _tabletProfile.Data.ByteOffsets.X,
            _tabletProfile.Data.ValueRanges.MaxX) ?? throw new Exception("Wrong Type for X coordinates");
        
        tabletData.Y = ParseValue<byte>(
            _tabletProfile.Data.ByteOffsets.Y,
            _tabletProfile.Data.ValueRanges.MaxY) ?? throw new Exception("Wrong Type for Y coordinates");
        
        // Parse pressure
        tabletData.Pressure = ParseValue<ushort>(
            _tabletProfile.Data.ByteOffsets.Pressure,
            _tabletProfile.Data.ValueRanges.MaxPressure) ?? throw new Exception("Wrong Type for Pressure value");
        
        //Parse Tilt & Wheel
        if (_tabletProfile.Data.ByteOffsets.TiltX.ByteOffset != -1)
        {
            tabletData.TiltX = ParseValue<sbyte>(
                _tabletProfile.Data.ByteOffsets.TiltX,
                _tabletProfile.Data.ValueRanges.MaxTilt) ?? throw new Exception("Wrong Type for TiltX value");
        }
        else tabletData.TiltX = 0;

        if (_tabletProfile.Data.ByteOffsets.TiltY.ByteOffset != -1)
        {
            tabletData.TiltY = ParseValue<sbyte>(
                _tabletProfile.Data.ByteOffsets.TiltY,
                _tabletProfile.Data.ValueRanges.MaxTilt) ?? throw new Exception("Wrong Type for TiltY value");
        }
        else tabletData.TiltY = 0;

        if (_tabletProfile.Data.ByteOffsets.Wheel.ByteOffset != -1)
        {
            tabletData.TiltX = ParseValue<sbyte>(
                _tabletProfile.Data.ByteOffsets.Wheel,
                1) ?? throw new Exception("Wrong Type for Wheel value");
        }
        else tabletData.Wheel = 0;
        
        return tabletData;

        T? ParseValue<T>(FieldInfo field, int maxValue) where T : struct
        {
            var value = Utilities.Utils.ReadBytes(
                data,
                field.ByteOffset,
                field.BitSize,
                field.IsSigned);
            
            return value switch
            {
                long l => (T)(object)(l / maxValue),
                ulong ul => (T)(object)(ul / (uint)maxValue),
                _ => null
            };
        }
   } //TODO Rebuild
   private bool RegisterTablet()
   {
      if (_currentStatus != TabletStatus.NotRegistered)
      {
         Console.WriteLine("[TabletManager] Cant register tablet, unregister first");
         return false;
      }
      RawInputHelper.RAWINPUTDEVICE device = new RawInputHelper.RAWINPUTDEVICE
      {
         usUsagePage = RawInputHelper.HID_USAGE_PAGE_DIGITIZER,
         usUsage = RawInputHelper.HID_USAGE_DIGITIZER,
         dwFlags = RawInputHelper.RIDEV_INPUTSINK,
         hwndTarget = _targetWindow
      };

      if (!RawInputHelper.RegisterRawInputDevices([device], 1, (uint)Marshal.SizeOf(typeof(RawInputHelper.RAWINPUTDEVICE)))) return false;
      _currentStatus = TabletStatus.Captured;
      return true;
   }

   private void UnregisterTablet()
   {
      if (_currentStatus is not (TabletStatus.Captured or TabletStatus.ReSelected)) return;
      RawInputHelper.RAWINPUTDEVICE device = new RawInputHelper.RAWINPUTDEVICE
      {
         usUsagePage = RawInputHelper.HID_USAGE_PAGE_DIGITIZER,
         usUsage = RawInputHelper.HID_USAGE_DIGITIZER,
         dwFlags = RawInputHelper.RIDEV_REMOVE,
         hwndTarget = IntPtr.Zero
      };

      RawInputHelper.RegisterRawInputDevices([device], 1, 28);
      _currentStatus = TabletStatus.NotRegistered;
   }
   #endregion

   #region Preparing
   public static List<DevicePreview> GetHidDevices()
   {
      var hidDevices = DeviceList.Local.GetHidDevices().Where(IsSimilarToTablet);
      return hidDevices.Select(d =>
      {
         var path = d.DevicePath;
         return new DevicePreview(d.GetProductName(), d.GetManufacturer(), d.VendorID, d.ProductID,
            Guid.Parse(Regex.Match(path, @"{(.+)}").Groups[1].Value));
      }).ToList();
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
   
   public bool SelectTablet(DevicePreview? tablet = null)
   {
      var hidDevices = GetHidDevices();
      var selected = tablet != null ? SelectManually(hidDevices, tablet) : SelectAuto(hidDevices);
      if (!selected) return false;

      _currentStatus = _currentStatus is TabletStatus.NotSelected ? TabletStatus.NotRegistered : TabletStatus.ReSelected;
      return true;
   }
   private bool SelectManually(List<DevicePreview> hidDevices, DevicePreview tablet)
   {
      _selectedTablet =
         hidDevices.FirstOrDefault(d => d.InstanceGuid == tablet.InstanceGuid);
      if (_selectedTablet == null) return false;
      
      AppConfigManager.AppSettings.LastDeviceVID = _selectedTablet.VendorID;
      AppConfigManager.AppSettings.LastDevicePID = _selectedTablet.ProductID;
      AppConfigManager.AppSettings.LastDeviceName = _selectedTablet.Name;
      AppConfigManager.SaveAppSettings();
      return true;
   }

   private bool SelectAuto(List<DevicePreview> hidDevices)
   {
      var appSettings = AppConfigManager.AppSettings;
      if (appSettings.LastDeviceVID == 0 || appSettings.LastDevicePID == 0) return false;
        
      _selectedTablet = hidDevices.FirstOrDefault(d => d.VendorID != appSettings.LastDeviceVID || d.ProductID != appSettings.LastDevicePID || d.Name.Contains(appSettings.LastDeviceName, StringComparison.OrdinalIgnoreCase));
      return _selectedTablet != null;
   }
   
   private bool LoadProfile() //TODO remove
   {
      if (_selectedTablet == null) return false;
      var profile = ProfileManager.GetProfile(_selectedTablet);
      if (profile != null)
      {
         _tabletProfile = profile;
         return true;
      }
      // profile = ProfileManager.GenerateProfile(device);
      // _tabletProfile = profile;
      return true;
   }
   #endregion

   public async Task<bool> StartAsync()
   {
      if (_selectedTablet == null)
      {
         OnErrorMessage?.Invoke("Select the tablet first");
         return false;
      }

      if (!LoadProfile()) //TODO Remove
      {
         OnErrorMessage?.Invoke("Profile load failed");
         return false;
      }

      while (_targetWindow == IntPtr.Zero)
      {
         await Task.Delay(500);
      }

      if (!RegisterTablet())
      {
         OnErrorMessage?.Invoke("Register tablet failed");
         return false;
      }

      return true;
   }

   public void Stop()
   {
      UnregisterTablet();
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