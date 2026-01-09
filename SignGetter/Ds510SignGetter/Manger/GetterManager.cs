using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Ds510SignGetter.Exceptions;
using Ds510SignGetter.Models;
using Ds510SignGetter.Ui;
using Ds510SignGetter.Utilities;

namespace Ds510SignGetter.Manger;

public static class GetterManager // Max X/Y = 0 + 7999; Max P = 2047 + 0
{
    private static ApplicationHost _appHost = new();
    private static GetterResult _result = new();
    private static GetterStatus _status = new();
    private static readonly CallbackStrReceive TabletCommandReceivedCallaback = TabletDataReceived;
    private static CancellationTokenSource? _cts;
    private static string _imagePath = string.Empty;

    private static readonly LinkedList<IntPtr> UnreleasedMemory = new();
    
    public static bool CanBeExecuted => !_status.IsExecuting;
    public static int GetStatusCode => _status.StatusCode;

    public static int GetSign(out IntPtr returnArrayPointer, out int returnArraySize,
        out int returnImageWidth, out int returnImageHeight, out int returnImageStride)
    {
        returnArrayPointer = IntPtr.Zero;
        returnArraySize = 0;
        returnImageWidth = 0;
        returnImageHeight = 0;
        returnImageStride = 0;
        
        _status.Reset();
        if (!_status.Initialized)
        {
            _status.StatusCode = InitializeSdk();
            if (_status.StatusCode != 0) return _status.StatusCode;
        }
        
        if (!_appHost.IsRunning)
        {
            _appHost.StartApp();
            var attempts = 10;
            do
            {
                Task.Delay(500).Wait();
            }
            while(!_appHost.IsRunning && attempts-- > 0);

            if (attempts <= 0)
            {
                _status.StatusCode = (int)StatusCodes.WindowCreationTimedOut;
                MessageService.ErrorMessage(StatusCodeToStringConverter(_status.StatusCode));
                return _status.StatusCode;
            }
        }

        _status.StatusCode = StartProcessing();
        if (_status.StatusCode != 0) return _status.StatusCode;
        
        WaitForStop().Wait();
        _cts!.Dispose();

        if ((StatusCodes)_status.StatusCode == StatusCodes.Success)
        {
            returnArrayPointer = _result.ResultPointer;
            returnArraySize = _result.ResultSize;
            returnImageHeight = _result.ImageHeight;
            returnImageWidth = _result.ImageWidth;
            returnImageStride = _result.ImageStride;
            UnreleasedMemory.AddLast(_result.ResultPointer);
        }
        
        _result.Reset();
        return _status.StatusCode;
    }

    private static int InitializeSdk()
    {
        _status.Initialized = false;
        var status = GWQ_Init(); // Initialize SDK
        if (status != 0)
        {
            MessageService.ErrorMessage("Error SDK Initializing\n" + StatusCodeToStringConverter(status));
            return status;
        }
            
        status = GWQ_OnOffScreen(0); // Turn Screen On
        if (status != 0)
        {
            MessageService.ErrorMessage("Error turning screen on\n" + StatusCodeToStringConverter(status));
            return status;
        }
            
        status = GWQ_SwitchLanguage(2); // Set Device screen to Blank
        if (status != 0)
        {
            MessageService.ErrorMessage("Error setting \"Blank\" screen\n" + StatusCodeToStringConverter(status));
            return status;
        }

        status = GWQ_setCallback(TabletCommandReceivedCallaback);
        if (status != 0)
        {
            MessageService.ErrorMessage("Error setting callback for data receiving\n" + StatusCodeToStringConverter(status));
            return status;
        }

        _status.Initialized = true;
        return 0;
    }
    
    private static async Task WaitForStop()
    {
        while (!_cts!.Token.IsCancellationRequested)
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
        await Task.Delay(500);
    }
    
    #region Process Controlling
    public static void RestartGetter()
    {
        RebootTablet();
        _appHost.RestartApp();
    }
    
    public static int ShutGetter()
    {
        var released = GWQ_Release();
        if (released != 0) return released;
        
        _appHost.Dispose();
        _cts?.Dispose();
        return 0;
    }

    private static int StartProcessing()
    {
        var s = ReSing();
        if (s != 0) return s;

        _appHost.ShowWindow();
        _cts = new();
        _status.IsExecuting = true;
        
        return 0;
    }

    private static void StopProcessing()
    {
        _cts?.Cancel();
        _appHost.HideImage();
        _appHost.HideWindow();
        _status.IsExecuting = false;
    }
    
    public static void CancelOperation()
    {
        StopProcessing();
        _status.StatusCode = (int)StatusCodes.OperationCanceled;
    }
    #endregion

    #region Tablet Access
    public static int ReSing()
    {
        if (GWQ_ReSign() is var status && status != 0) MessageService.ErrorMessage($"Error resigning\n" + StatusCodeToStringConverter(status));
        return status;
    }

    public static int TurnScreenOn()
    {
        if (GWQ_OnOffScreen(0) is var status && status != 0) MessageService.ErrorMessage($"Error turning screen On\n" + StatusCodeToStringConverter(status));
        return status;
    }

    public static int TurnScreenOff()
    {
        if (GWQ_OnOffScreen(1) is var status && status != 0) MessageService.ErrorMessage($"Error turning screen Off\n" + StatusCodeToStringConverter(status));
        return status;
        
    }

    public static int RebootTablet()
    {
        var s = GWQ_Reboot();
        if (s != 0) MessageService.ErrorMessage($"Error rebooting\n" + StatusCodeToStringConverter(s));
        return s;
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

    #region Image Processing
    private static void TabletDataReceived(int type, IntPtr data, string path) // type: 64 - resign; 176 - get image
    {
        switch (type)
        {
            case 176:
                Task.Delay(1000).Wait();
                _appHost.ShowImage(path);
                _imagePath = path;
                break;
        }
    }

    public static bool GetImage()
    {
        var s = GWQ_GetImage();
        if (s == 0) return true;
        
        MessageService.ErrorMessage("Error getting image from tablet\n" + StatusCodeToStringConverter(s));
        _status.StatusCode = s;
        return false;
    }

    public static bool SaveResult()
    {
        if (!SaveImage()) return false;
        StopProcessing();
        return true;
    }

    private static bool SaveImage()
    {
        if (!File.Exists(_imagePath)) return false;

        int width, height, size, stride;
        IntPtr ptr;
        
        using (var fs = new FileStream(_imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = fs;
            bmp.EndInit();
            bmp.Freeze();

            width = bmp.PixelWidth;
            height = bmp.PixelHeight;
            stride = width * (bmp.Format.BitsPerPixel / 8);
            size = stride * height;
            
            ptr = Marshal.AllocHGlobal(size);
            bmp.CopyPixels(Int32Rect.Empty, ptr, size, stride);
        }
        
        _result.ResultPointer = ptr;
        _result.ResultSize = size;
        _result.ImageHeight = height;
        _result.ImageWidth = width;
        _result.ImageStride = stride;
        _imagePath = string.Empty;

        return true;
    }
    #endregion

    #region Utils
    private static string StatusCodeToStringConverter(StatusCodes status) => StatusCodeToStringConverter((int)status);
    private static string StatusCodeToStringConverter(int status)
    {
        return status switch
        {
            0 => "Success",
            -1 => "SDK not initialized",
            -2 => "Device not connected",
            1 => "Window creation timed out",
            2 => "Operation Canceled",
            3 => "Out Of Memory",
            4 => "Saving Exception",
            5 => "Canvas is empty",
            6 => "Window is null",
            _ => "Unknown error"
        };
    }
    #endregion

    #region Dll Imports
    private const string DllName = "device5_hid_sdk.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_Init();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_Release();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_DeviceOnline();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_SwitchLanguage(int language);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_GetImage();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_SetImage([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_ShowImage();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_OnOffScreen(int type);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_Reboot();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_ReSign();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_OnOffPointReporting(int type);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_SetSigningBackground(IntPtr data, int len);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_SetSigningBackground2(IntPtr data, int len);

    // --- Callbacks ---
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CallbackStrReceive(int type, IntPtr data,
        [MarshalAs(UnmanagedType.LPStr)] string path); // Device Buttons press (Re-sign, cancel, submit)

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_setCallback(CallbackStrReceive callback);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CallbackSignatureData(int x, int y, int p, int sn); // Device points press (after OnOffPoints set to 1)

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GWQ_setSignatureData(CallbackSignatureData callback);
    #endregion
}