using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TabletSignGetterLib.Exceptions;
using TabletSignGetterLib.Models;
using TabletSignGetterLib.Ui;
using TabletSignGetterLib.Utilities;
using DeviceFilter = TabletSignGetterLib.Utilities.DeviceFilter;
using TabletDevice = TabletSignGetterLib.Models.TabletDevice;

namespace TabletSignGetterLib.Manager;

public static class GetterManager
{
    private const int CropPadding = 20;
    
    private static readonly ApplicationHost _appHost = new(WindowHookProcess);
    private static TabletDevice? _selectedTablet;
    private static GetterStatus _status = new();
    private static GetterResult _result = new();
    private static IntPtr _targetHandler = IntPtr.Zero;
    private static CancellationTokenSource? _cts;
    
    private static readonly LinkedList<IntPtr> _unreleasedMemory = new();
    private static readonly LinkedList<int> _unreleasedMemorySizes = new();
    
    public static bool CanBeExecuted => !_status.IsExecuting;
    public static int GetStatusCode() => _status.StatusCode;

    public static int GetSign(out IntPtr returnArrayPointer, out int returnArraySize, 
        out int returnImageWidth, out int returnImageHeight, out int returnImageStride)
    {
        returnArrayPointer = IntPtr.Zero;
        returnArraySize = 0;
        returnImageHeight = 0;
        returnImageWidth = 0;
        returnImageStride = 0;
        
        _status.Reset();
        
        if (!CanBeExecuted)
        {
            MessageService.ErrorMessage("The tablet process is currently running. Call later");
            Console.WriteLine("[SignGetter] The tablet process is currently running. Cant execute your request");
            return (int)StatusCodes.GetterProcessNotCompleted;
        }
        
        if (_selectedTablet == null || !TabletManager.IsTabletExists(_selectedTablet))
        {
            if (!SelectTablet()) return _status.StatusCode;
        }
        
        if (!_appHost.IsRunning)
        {
            _appHost.StartApp(KeyDownEvent);
            var attempts = 10;
            do
            {
                Task.Delay(500).Wait();
            }
            while(!_appHost.IsRunning && attempts-- > 0);

            if (attempts <= 0)
            {
                MessageService.ErrorMessage("Error creating window");
                Console.WriteLine("[SignGetter] Error creating window");
                return (int)StatusCodes.WindowCreationTimedOut;
            }
        }

        if (!_status.IsRegistered && !RegisterTablet()) return _status.StatusCode;
        
        StartProcessing();
        WaitForComplete().Wait();
        _cts!.Dispose();
        
        returnArrayPointer = _result.ResultPointer;
        returnArraySize = _result.ResultSize;
        returnImageHeight = _result.ImageHeight;
        returnImageWidth = _result.ImageWidth;
        returnImageStride = _result.ImageStride;
        
        _unreleasedMemory.AddLast(_result.ResultPointer);
        _unreleasedMemorySizes.AddLast(_result.ResultSize);
        _result.Reset();
        return _status.StatusCode;
    }

    public static bool SelectTablet()
    {
        if (_status.IsRegistered) UnregisterTablet();
        
        var status = TabletManager.SelectTablet(out _selectedTablet);
        if ((StatusCodes)status is (StatusCodes.Success or StatusCodes.AutoSelected)) return true;
        
        MessageService.WarningMessage("The tablet is not selected!");
        ChangeStatus(status);
        return false;
    }
    
    private static async Task WaitForComplete()
    {
        if (_cts is null) return;
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
    
    private static void KeyDownEvent(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
            {
                e.Handled = true;
                if (_appHost.AskMessage(MessageService.AskYesNoMessage, "Do you want to exit without saving?"))
                    StopProcessing();
                return;
            }
            
            case Key.Z when Keyboard.Modifiers == ModifierKeys.Control:
                e.Handled = true;
                _appHost.ClearCanvas();
                return;
            
            case Key.Enter:
            {
                e.Handled = true;
                BlockProcessing();
                if (!SaveImage())
                {
                    _appHost.ShowMessage(MessageService.ErrorMessage, "Failed to save sign to memory. Try again");
                    UnblockProcessing();
                }
                else StopProcessing();
                return;
            }
        }
    }

    #region Save Result
    private static bool SaveImage()
    {
        try
        {
            if (!_appHost.CanRender())
            {
                ChangeStatus(StatusCodes.CanvasIsEmpty);
                return false;
            }
            
            var rtb = _appHost.RenderCanvas();
            if (rtb == null)
            {
                ChangeStatus(StatusCodes.CanvasIsNull);
                return false;
            }
            var cropped = CropSign(rtb);
            CopyToMemory(cropped);
            return true;
        }
        catch (OutOfMemoryException ex)
        {
            Console.WriteLine("[SignGetter] Too much memory requested: {0}", ex.Message);
            ChangeStatus(StatusCodes.OutOfMemory);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SignGetter > SaveImage] Error in saving: {0}", ex.Message);
            ChangeStatus(StatusCodes.SavingException);
            return false;
        }
    }

    private static CroppedBitmap CropSign(BitmapSource src)
    {
        var maxX = (int)(_status.MaxX * src.PixelWidth);
        var maxY = (int)(_status.MaxY * src.PixelHeight);
        var minX = (int)(_status.MinX * src.PixelWidth);
        var minY = (int)(_status.MinY * src.PixelHeight);

        if (minX > CropPadding) minX -= CropPadding;
        else minX = 0;

        if (minY > CropPadding) minY -= CropPadding;
        else minY = 0;

        if (maxX + CropPadding < src.PixelWidth) maxX += CropPadding;
        else maxX = src.PixelWidth;

        if (maxY + CropPadding < src.PixelHeight) maxY += CropPadding;
        else maxY = src.PixelHeight;
        
        var rect = new Int32Rect(minX, minY, maxX-minX, maxY-minY);
        return new CroppedBitmap(src, rect);
    }

    private static void CopyToMemory(BitmapSource src)
    {
        var stride = (src.PixelWidth * src.Format.BitsPerPixel + 7) / 8;
        var size = stride * src.PixelHeight;
        
        var ptr = Marshal.AllocHGlobal(size);
        src.CopyPixels(Int32Rect.Empty, ptr, size, stride);
        
        _result.ResultPointer = ptr;
        _result.ResultSize = size;
        _result.ImageHeight = src.PixelHeight;
        _result.ImageWidth = src.PixelWidth;
        _result.ImageStride = stride;
    }
    #endregion

    #region Memory Controlling
    public static void ReleaseOneMemory()
    {
        try
        {
            var ptr = _unreleasedMemory.First?.Value;
            if (ptr == null) return;
            
            Marshal.FreeHGlobal(ptr.Value);
            
            _unreleasedMemory.RemoveFirst();
            _unreleasedMemorySizes.RemoveFirst();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("[SignGetter] The list of unreleased memory is empty");
        }
    }

    public static void ReleaseMemory()
    {
        foreach (var ptr in _unreleasedMemory)
        {
            Marshal.FreeHGlobal(ptr);
        }
        _unreleasedMemory.Clear();
        _unreleasedMemorySizes.Clear();
    }
    #endregion

    #region Process Controlling
    private static void StartProcessing()
    {
        _appHost.ClearCanvas();
        _appHost.ShowWindow();
        _status.IsExecuting = true;
        _status.IsBlocked = false;
        _cts = new();
    }

    private static void BlockProcessing() => _status.IsBlocked = true;

    private static void UnblockProcessing() => _status.IsBlocked = false;
        
    private static void StopProcessing()
    {
        _cts?.Cancel();
        _appHost.HideWindow();
        _status.IsExecuting = false;
        _status.IsBlocked = true;
    }

    public static void ShutGetter()
    {
        if (_status.IsExecuting) StopProcessing();
        TabletManager.UnregisterTablet();
        _targetHandler = IntPtr.Zero;
        _status.IsRegistered = false;
        _appHost.ShutApp();
    }
    #endregion

    #region Utils
    private static bool RegisterTablet()
    {
        if (TabletManager.RegisterTablet(_appHost.TargetWindowHandle)) _status.IsRegistered = true;
        else
        {
            MessageService.ErrorMessage("Error registering the tablet");
            _status.IsRegistered = false;
            ChangeStatus(StatusCodes.TabletRegisterFailed);
        }

        return _status.IsRegistered;
    }

    private static void UnregisterTablet()
    {
        TabletManager.UnregisterTablet();
        _status.IsRegistered = false;
    }

    private static void ChangeStatus(StatusCodes status)
    {
        _status.StatusCode ^= (int)status;
    }

    private static void ChangeStatus(int status)
    {
        _status.StatusCode ^= status;
    }
    #endregion

    #region Message Processing
    private static IntPtr WindowHookProcess(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != Rih.WM_INPUT || _status.IsBlocked) return IntPtr.Zero;
        
        handled = true;
        handled = ProcessRawInput(lParam);
        return IntPtr.Zero;
    }
    
    private static bool IsTargetTablet(IntPtr hDevice)
    {
        if (_targetHandler != IntPtr.Zero) return hDevice == _targetHandler;
        
        var (vid,pid) = DeviceFilter.GetIdsFromPath(DeviceFilter.GetDevicePath(hDevice));
        if (vid is null || pid is null) return false;

        return _selectedTablet?.VendorId == vid.Value
               && _selectedTablet?.ProductId == pid.Value;
    }
    
    private static bool ProcessRawInput(IntPtr lParam)
    {
        uint dataSize = 0;
        var headerSize = (uint)Marshal.SizeOf(typeof(Rih.RAWINPUTHEADER));
      
        var result = Rih.GetRawInputData(lParam, Rih.RID_INPUT, IntPtr.Zero, ref dataSize, headerSize);

        if (result == 0 && dataSize == 0)
        {
            Console.WriteLine("[SignGetter > ProcessMsg] Failed to read input size: {0}", Marshal.GetLastWin32Error());
            _status.StatusCode = (int)StatusCodes.InputReadingException;
            return false;
        }
      
        var buffer = Marshal.AllocHGlobal((int)dataSize);
        try
        {
            if (Rih.GetRawInputData(lParam, Rih.RID_INPUT, buffer, ref dataSize, headerSize) != dataSize)
            {
                Console.WriteLine("[SignGetter > ProcessMsg] Failed to read input data: {0}", Marshal.GetLastWin32Error());
                _status.StatusCode = (int)StatusCodes.InputReadingException;
                return false;
            }
         
            var raw = Marshal.PtrToStructure<Rih.RAWINPUT>(buffer);
         
            var hDevice = raw.header.hDevice;
            if (!IsTargetTablet(hDevice) || raw.header.dwType != Rih.RIM_TYPEMOUSE) return false;
        
            if (_targetHandler == IntPtr.Zero)
            {
                _targetHandler = hDevice;
            }
            
            var mouse = raw.mouse;
            DrawData(ParseData(mouse));
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void SaveCriticalValues(float x, float y)
    {
        if (_status.MinX > x) _status.MinX = x;
        else if (_status.MaxX < x) _status.MaxX = x;

        if (_status.MinY > y) _status.MinY = y;
        else if (_status.MaxY < y) _status.MaxY = y;
    }

    private static void DrawData(TabletData data)
    {
        try
        {
            if (data.TipPressed) _status.TipStatus = true;
            if (data.TipUnPressed) _status.TipStatus = false;
        
            if (_status.TipStatus)
            {
                _appHost.DrawPoint(data.X, data.Y);
                SaveCriticalValues(data.X, data.Y);
            }
            else _appHost.ResetCanvasPoint();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SignGetter > DrawData] Data drawing exception: " + ex.Message);
            _status.StatusCode = (int)StatusCodes.DrawingException;
        }
    }

    private static TabletData ParseData(Rih.RAWMOUSE mouse)
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
}