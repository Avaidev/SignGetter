using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TabletSignGetterLib.Exceptions;
using TabletSignGetterLib.Models;
using TabletSignGetterLib.Services;
using TabletSignGetterLib.Ui;
using TabletSignGetterLib.Utilities;

namespace TabletSignGetterLib;

public static class GetterManager
{
    private const int AppHostStartAttempts = 10;
    private const int AppHostStartWaitTime = 500;

    private static int _cropPaddingSetting = 20;
    private static readonly MemoryController MemoryController = new();
    private static readonly GetterStatus Status = new();
    private static readonly SignalService SignalService = new();
    private static ApplicationHost _appHost = new(SignalService);
    
    private static CancellationTokenSource? _cts;
    private static Exception? _raisedException;
    
    private static GetterResult _lastResult;
    
    public static bool CheckCanBeExecuted() => !Status.IsExecuting;
    
    public static void SetCropPadding(int padding) => _cropPaddingSetting = padding;
    public static int GetCropPadding() => _cropPaddingSetting;

    public static void ReleaseMemory() => MemoryController.FreeAll();
    public static void ReleaseOneMemory() => MemoryController.Free();

    public static void ShutGetter()
    {
        if (Status.IsExecuting) StopExecuting();
        if (Status.IsRegistered) UnRegisterTablet();
        if(SignalService.IsBlocked) SignalService.Release();
        _appHost.Dispose();
        _cts?.Dispose();
    }

    public static int GetSign(out IntPtr returnArrayPointer, out int returnArraySize,
        out int returnImageWidth, out int returnImageHeight, out int returnImageStride)
    {
        returnArrayPointer = IntPtr.Zero;
        returnArraySize = 0;
        returnImageHeight = 0;
        returnImageWidth = 0;
        returnImageStride = 0;

        try
        {
            if (!CheckCanBeExecuted())
                throw new BaseException("There is another process is currently in progress", StatusCodes.IsExecuting);

            var tablet = SignalService.GetTablet();
            if (tablet == null || !TabletService.IsTabletExists(tablet))
            {
                if (Status.IsRegistered) UnRegisterTablet();
                SignalService.SetTablet(TabletService.SelectTablet());
            }

            if (!_appHost.IsRunning)
            {
                _appHost.Start();
                var attempts = 0;
                do
                {
                    Task.Delay(AppHostStartWaitTime).Wait();
                    attempts++;
                    if (attempts >= AppHostStartAttempts)
                        throw new BaseException("UI window start timeout", StatusCodes.WindowStartTimeOut);
                } while (!_appHost.IsRunning);
            }

            if (!Status.IsRegistered) RegisterTablet();

            ReSign();
            _raisedException = null;

            _cts = new CancellationTokenSource();
            StartExecuting();
            SignalService.Release();

            WaitForComplete().Wait();

            if (_raisedException != null)
                throw _raisedException;

            returnArrayPointer = _lastResult.ResultPointer;
            returnArraySize = _lastResult.ResultSize;
            returnImageHeight = _lastResult.ImageHeight;
            returnImageWidth = _lastResult.ImageWidth;
            returnImageStride = _lastResult.ImageStride;
            
            return (int)StatusCodes.Success;
        }
        catch (BaseException ex) when (ex.StatusCode == StatusCodes.Interrupted)
        {
            SignalService.Block();
            return (int)ex.StatusCode;
        }
        catch (BaseException ex)
        {
            var code = (int)ex.StatusCode;
            MessageService.ErrorMessage(
                $"{(string.IsNullOrWhiteSpace(ex.Message) ? "Some exception occured" : ex.Message)}: {code} ({ex.StatusCode.ToString()})");
            return code;
        }
        catch (Exception ex)
        {
            StopExecuting();
            SignalService.Block();
            MessageService.ErrorMessage(ex.Message);
            return (int)StatusCodes.OtherException;
        }
        finally
        {
            _cts?.Dispose();
        }
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
    
    internal static void HandleDataReceived(TabletData data)
    {
        if (data.Tip)
        {
            _appHost.DrawPoint(data.X, data.Y);
            SignalService.SaveCriticalValues(data.X, data.Y);
        }
        else _appHost.ResetCanvasPoint();
    }

    internal static void SaveResult()
    {
        SignalService.Block();
        var rtb = _appHost.RenderCanvas();
        if (rtb == null)
            throw new CanvasIsEmptyException("Canvas is empty");
        
        var cropped = CropSign(rtb);
        CopyToMemory(new WriteableBitmap(cropped));
        StopExecuting();
    }
    
    private static CroppedBitmap CropSign(BitmapSource src)
    {
        var criticalValues = SignalService.GetCriticalValues();
        var maxX = (int)(criticalValues.MaxX * src.PixelWidth);
        var maxY = (int)(criticalValues.MaxY * src.PixelHeight);
        var minX = (int)(criticalValues.MinX * src.PixelWidth);
        var minY = (int)(criticalValues.MinY * src.PixelHeight);

        if (minX > _cropPaddingSetting) minX -= _cropPaddingSetting;
        else minX = 0;

        if (minY > _cropPaddingSetting) minY -= _cropPaddingSetting;
        else minY = 0;

        if (maxX + _cropPaddingSetting < src.PixelWidth) maxX += _cropPaddingSetting;
        else maxX = src.PixelWidth;

        if (maxY + _cropPaddingSetting < src.PixelHeight) maxY += _cropPaddingSetting;
        else maxY = src.PixelHeight;
        
        var rect = new Int32Rect(minX, minY, maxX-minX, maxY-minY);
        return new CroppedBitmap(src, rect);
    }

    private static void CopyToMemory(BitmapSource src)
    {
        var converted = new FormatConvertedBitmap(src, PixelFormats.Bgr32, null, 0);

        int width = converted.PixelWidth;
        int height = converted.PixelHeight;

        int bitsPerPixel = converted.Format.BitsPerPixel;
        int stride = width * (bitsPerPixel / 8); 
        int size = stride * height;

        var ptr = MemoryController.AllocateNew(size);

        converted.CopyPixels(Int32Rect.Empty, ptr, size, stride);

        _lastResult.ResultPointer = ptr;
        _lastResult.ResultSize = size;
        _lastResult.ImageHeight = height;
        _lastResult.ImageWidth = width;
        _lastResult.ImageStride = stride;
    }

    #region Executing

    internal static void StartExecuting()
    {
        _appHost.ShowWindow();
        Status.IsExecuting = true;
    }
    
    internal static void StopExecuting()
    {
        _cts?.Cancel();
        _appHost.HideWindow();
        Status.IsExecuting = false;
    }
    
    internal static void DisableInput()
        => _appHost.DisableInput();
    
    internal static void EnableInput()
        => _appHost.EnableInput();

    internal static void ReSign()
    {
        _lastResult.Reset();
        _appHost.ClearCanvas();
        SignalService.Reset();
    }

    internal static void RaiseOuterException(Exception exception)
    {
        _raisedException = exception;
        StopExecuting();
    }
    
    #endregion
    
    #region Utils

    private static void RegisterTablet()
    {
        if (TabletService.RegisterTablet(_appHost.GetHwndTarget())) 
            Status.IsRegistered = true;
        else
        {
            Status.IsRegistered = false;
            throw new TabletRegisterFailException();
        }
    }

    private static void UnRegisterTablet()
    {
        TabletService.UnregisterTablet();
        Status.IsRegistered = false;
    }

    #endregion
}