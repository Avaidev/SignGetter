using HidSharp;
using TabletLib.Models;
using TabletLib.Models.Profile;
using TabletLib.Utilities;

namespace TabletLib.Services;

public class TabletManager : IDisposable
{
    private TabletProfile? _tabletProfile;
    private HidDevice? _hidDevice;
    private HidStream? _hidStream;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning = false;
    private bool _disposed = false;
    
    public bool TabletIsLoaded = false;
    public bool TabletIsConnected = false;
    public string TabletDeviceName => _tabletProfile?.DeviceName ?? "Unknown";
    public string TabletManufacturer => _tabletProfile?.Manufacturer ?? "Unknown";
    
    private event Action<TabletData>? OnDataReceived;
    private event Action<string>? OnWarningMessage;
    private event Action<string>? OnErrorMessage;
    
    public TabletManager(){}

    public TabletManager(Action<TabletData> onDataCallback)
    {
        OnDataReceived += onDataCallback;
    }
    
    public void SetOnDataReceivedCallback(Action<TabletData> onDataCallback){ OnDataReceived += onDataCallback; }
    public void SetOnWarningMessageCallback(Action<string> callback) {OnWarningMessage += callback;}
    public void SetOnErrorMessageCallback(Action<string> callback) {OnErrorMessage += callback;}

    public bool Start()
    {
        if (_isRunning) return false;

        if (!DetectTablet())
        {
            TabletIsLoaded = false;
            TabletIsConnected = false;
            OnErrorMessage?.Invoke("Failed to load tablet. Check connection");
            return false;
        }
        
        TabletIsConnected = true;

        if (!LoadProfile())
        {
            TabletIsLoaded = false;
            OnErrorMessage?.Invoke("Failed to load tablet profile");
            return false;
        }

        TabletIsLoaded = true;
        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        Task.Factory.StartNew(async () => await RunTabletLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        return true;
    }

    public void Stop()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
    }

    private async Task RunTabletLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_hidStream == null && !TryOpenTabletStream())
                {
                    await Task.Delay(1000, token);
                    continue;
                }

                await ReadTabletData(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                OnErrorMessage?.Invoke(ex.Message);
                await Task.Delay(1000, token);
            }
        }
    }

    private async Task ReadTabletData(CancellationToken token)
    {
        if (_tabletProfile == null ||
            _hidDevice == null ||
            _hidStream == null ||
            token.IsCancellationRequested) return;

        byte[] buffer = new byte[_hidDevice.GetMaxInputReportLength()];
        while (!token.IsCancellationRequested)
        {
            try
            {
                int bytesRead = await _hidStream.ReadAsync(buffer, token);

                if (bytesRead > 0)
                {
                    var tabletData = ParseData(buffer);
                    if (tabletData != null)
                    {
                        OnDataReceived?.Invoke(tabletData);
                    }
                }
            }
            catch(Exception ex) when (ex is IOException or OperationCanceledException)
            {
                break;
            }
            catch (TimeoutException)
            {
                continue;
            }
            
            catch (Exception ex)
            {
                OnWarningMessage?.Invoke(ex.Message);
                await Task.Delay(1000, token);
            }
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
        
        if (_tabletProfile.Data.ByteOffsets.TiltY.ByteOffset != -1)
        {
            tabletData.TiltY = ParseValue<sbyte>(
                _tabletProfile.Data.ByteOffsets.TiltY,
                _tabletProfile.Data.ValueRanges.MaxTilt) ?? throw new Exception("Wrong Type for TiltY value");
        }
        
        if (_tabletProfile.Data.ByteOffsets.Wheel.ByteOffset != -1)
        {
            tabletData.TiltX = ParseValue<sbyte>(
                _tabletProfile.Data.ByteOffsets.Wheel,
                1) ?? throw new Exception("Wrong Type for Wheel value");
        }
        
        return tabletData;

        T? ParseValue<T>(FieldInfo field, int maxValue) where T : struct
        {
            var value = Utils.ReadBytes(
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
    }

    private bool TryOpenTabletStream()
    {
        if(_hidDevice == null) return false;
        if(_hidStream != null) return true;

        if (!_hidDevice.TryOpen(out _hidStream))
        {
            OnErrorMessage?.Invoke("Failed to open tablet stream");
            return false;
        }

        _hidStream.ReadTimeout = Timeout.Infinite;

        return true;
    }

    private bool DetectTablet()
    {
        var deviceList = DeviceList.Local;
        var hidDevices = deviceList.GetHidDevices().ToList();

        if (hidDevices.Count == 0) return false;
        
        foreach (var device in hidDevices.Where(IsSimilarToTablet))
        {
            _hidDevice = device;
            return true;
        }

        var profiles = ProfileManager.GetAllProfiles();
        if (profiles is null ||  profiles.Count == 0) return false;

        foreach (var device in profiles.Select(profile => hidDevices.FirstOrDefault(d => d.VendorID == profile.VendorId && d.ProductID == profile.ProductId)).OfType<HidDevice>())
        {
            _hidDevice = device;
            return true;
        }
        
        return false;
    }
    
    private bool IsSimilarToTablet(HidDevice device)
    {
        var descriptor = device.GetReportDescriptor();
        if (descriptor != null)
        {
            return descriptor.DeviceItems.SelectMany(item => item.Usages.GetAllValues()).Any(usage => (ushort)(usage >> 16) == 0x0D);
        }
            
        return false;
    }

    private bool LoadProfile()
    {
        if (_hidDevice == null) return false;
        var profile = ProfileManager.GetProfile(_hidDevice);
        if (profile != null)
        {
            _tabletProfile = profile;
            return true;
        }
        profile = ProfileManager.GenerateProfile(_hidDevice);
        _tabletProfile = profile;
        return true;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}