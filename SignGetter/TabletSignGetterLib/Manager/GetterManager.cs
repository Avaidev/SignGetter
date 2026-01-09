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
    
    private static readonly ApplicationHost AppHost = new(WindowHookProcess);
    private static TabletDevice? _selectedTablet;
    private static GetterStatus _status = new();
    private static GetterResult _result = new();
    private static IntPtr _targetHandler = IntPtr.Zero;
    private static CancellationTokenSource? _cts;
    
    private static readonly LinkedList<IntPtr> UnreleasedMemory = new();
    
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
            MessageService.ErrorMessage(StatusCodeToStringConverter(StatusCodes.GetterProcessNotCompleted));
            return (int)StatusCodes.GetterProcessNotCompleted;
        }
        
        if (_selectedTablet == null || !TabletManager.IsTabletExists(_selectedTablet))
        {
            if (!SelectTablet()) return _status.StatusCode;
        }
        
        if (!AppHost.IsRunning)
        {
            AppHost.StartApp(KeyDownEvent);
            var attempts = 10;
            do
            {
                Task.Delay(500).Wait();
            }
            while(!AppHost.IsRunning && attempts-- > 0);

            if (attempts <= 0)
            {
                MessageService.ErrorMessage(StatusCodeToStringConverter(StatusCodes.WindowCreationTimedOut));
                return (int)StatusCodes.WindowCreationTimedOut;
            }
        }

        if ((!_status.IsRegistered && !RegisterTablet()) || !StartProcessing()) return _status.StatusCode;

        WaitForComplete().Wait();
        _cts!.Dispose();
        
        returnArrayPointer = _result.ResultPointer;
        returnArraySize = _result.ResultSize;
        returnImageHeight = _result.ImageHeight;
        returnImageWidth = _result.ImageWidth;
        returnImageStride = _result.ImageStride;
        
        UnreleasedMemory.AddLast(_result.ResultPointer);
        _result.Reset();
        return _status.StatusCode;
    }

    public static bool SelectTablet()
    {
        if (_status.IsRegistered) UnregisterTablet();
        
        var status = TabletManager.SelectTablet(out _selectedTablet);
        if ((StatusCodes)status is (StatusCodes.Success or StatusCodes.AutoSelected)) return true;
        
        MessageService.WarningMessage($"The tablet is not selected! {StatusCodeToStringConverter((StatusCodes)status)}");
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
                if (AppHost.AskMessage(MessageService.AskYesNoMessage, "Do you want to exit without saving?"))
                    StopProcessing();
                return;
            }
            
            case Key.Z when Keyboard.Modifiers == ModifierKeys.Control:
                e.Handled = true;
                AppHost.ClearCanvas();
                return;
            
            case Key.Enter:
            {
                e.Handled = true;
                BlockProcessing();
                if (!SaveImage())
                {
                    AppHost.ShowMessage(MessageService.ErrorMessage, "Failed to save sign to memory. Try again");
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
            if (!AppHost.CanRender())
            {
                ChangeStatus(StatusCodes.CanvasIsEmpty);
                return false;
            }
            
            var rtb = AppHost.RenderCanvas();
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
            var ptr = UnreleasedMemory.First?.Value;
            if (ptr == null) return;
            
            Marshal.FreeHGlobal(ptr.Value);
            
            UnreleasedMemory.RemoveFirst();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("[SignGetter] The list of unreleased memory is empty");
        }
    }

    public static void ReleaseMemory()
    {
        foreach (var ptr in UnreleasedMemory)
        {
            Marshal.FreeHGlobal(ptr);
        }
        UnreleasedMemory.Clear();
    }
    #endregion

    #region Process Controlling
    private static bool StartProcessing()
    {
        try
        {
            AppHost.ClearCanvas();
            AppHost.ShowWindow();
            _status.IsExecuting = true;
            _status.IsBlocked = false;
            _cts = new();
            return true;
        }
        catch (InvalidWindowException ex)
        {
            Console.WriteLine("[SignGetter] Error in StartProcessing: {0}]", ex.Message);
            ChangeStatus(StatusCodes.InvalidWindow);
            _status.IsExecuting = false;
            _status.IsBlocked = true;
            return false;
        }
    }

    private static void BlockProcessing() => _status.IsBlocked = true;

    private static void UnblockProcessing() => _status.IsBlocked = false;
        
    private static void StopProcessing()
    {
        try
        {
            _cts?.Cancel();
            AppHost.HideWindow();
            _status.IsExecuting = false;
            _status.IsBlocked = true;
        }
        catch (InvalidWindowException ex)
        {
            Console.WriteLine("[SignGetter] Error in StopProcessing: {0}]", ex.Message);
            ChangeStatus(StatusCodes.InvalidWindow);
            _status.IsExecuting = true;
            _status.IsBlocked = false;
        }
    }

    public static void RestartGetter()
    {
        if (_status.IsExecuting) StopProcessing();
        UnregisterTablet();
        _targetHandler = IntPtr.Zero;
        AppHost.RestartApp(KeyDownEvent);
    }

    public static void ShutGetter()
    {
        if (_status.IsExecuting) StopProcessing();
        UnregisterTablet();
        _targetHandler = IntPtr.Zero;
        AppHost.Dispose();
    }
    #endregion

    #region Utils
    private static bool RegisterTablet()
    {
        if (TabletManager.RegisterTablet(AppHost.TargetWindowHandle)) _status.IsRegistered = true;
        else
        {
            MessageService.ErrorMessage(StatusCodeToStringConverter(StatusCodes.TabletRegisterFailed));
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

    private static string StatusCodeToStringConverter(int status)
    {
        return status switch
        {
            0 => "Success",
            -1 => "Other",
            0x01 => "Tablets list is empty",
            0x02 => "Tablet not found",
            0x04 => "Other tablet selection error",

            0x08 => "Invalid input",
            0x10 => "Auto selected",

            0x20 => "Window creation timed out",

            0x40 => "Saving error",
            0x80 => "Canvas is Null",
            0x100 => "Canvas is Empty",
            0x200 => "Out of Memory",

            0x400 => "SignGetter is currently executing",
            0x800 => "Tablet registering failed",
            0x1000 => "Exception in drawing",
            0x2000 => "Exception in reading input data",

            0x4000 => "Invalid window",
            _ => "Unknown error"
        };
    }

    private static string StatusCodeToStringConverter(StatusCodes status)
    {
        return StatusCodeToStringConverter((int)status);
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
               && _selectedTablet.ProductId == pid.Value;
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
                AppHost.DrawPoint(data.X, data.Y);
                SaveCriticalValues(data.X, data.Y);
            }
            else AppHost.ResetCanvasPoint();
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