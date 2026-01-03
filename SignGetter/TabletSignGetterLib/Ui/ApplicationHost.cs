using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TabletLib.Utilities;
using TabletSignGetterLib.Exceptions;

namespace TabletSignGetterLib.Ui;

public class ApplicationHost(HwndSourceHook hook) : IDisposable
{
    private Thread? _uiThread;
    private InkCanvasHost? _canvas;
    private CanvasWindow? _window;
    private HwndSource? _hwndSrc;
    private bool _hooked;

    public IntPtr TargetWindowHandle { get; private set; } = IntPtr.Zero;
    public bool IsRunning => Application.Current != null && _window != null && _hooked;

    private void OnWindowInit(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.SourceInitialized -= OnWindowInit;
            TargetWindowHandle = new WindowInteropHelper(window).Handle;
            SetHook();
        }
    }

    private void SetHook()
    {
        if (TargetWindowHandle == IntPtr.Zero) return;
        _hwndSrc = HwndSource.FromHwnd(TargetWindowHandle);
        if (_hwndSrc is null) return;
        _hwndSrc.AddHook(hook);
        _hooked = true;
    }
    
    public bool CanRender() => _canvas != null && !_canvas.IsEmpty();

    public RenderTargetBitmap? RenderCanvas()
    {
        if (_canvas == null) return null;
        var w = (int)_canvas.ActualWidth;
        var h = (int)_canvas.ActualHeight;

        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(_canvas);
        return rtb;
    }

    public void StartApp(KeyEventHandler keyEventFunc)
    {
        if (_window != null)
        {
            Console.WriteLine("[SignGetter > AppHost] Cant start the app that is already started");
            return;
        }
        
        if (Application.Current != null) Application.Current.Dispatcher.Invoke(() =>
            {
                _canvas = new InkCanvasHost();
                _window = new CanvasWindow(_canvas, keyEventFunc);
                _window.SourceInitialized += OnWindowInit;
                _window.Show();
                _window.Hide();
            });
        else
        {
            _uiThread = new Thread(() =>
            {
                var app = new Application();
                _canvas = new InkCanvasHost();
                _window = new CanvasWindow(_canvas, keyEventFunc);
                _window.SourceInitialized += OnWindowInit;
            
                app.Run(_window);
            });
        
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
        }
    }

    public void RestartApp(KeyEventHandler keyEventFunc)
    {
        _window?.Dispatcher.Invoke(() => _window?.Close());
        _window = null;
        _hwndSrc?.Dispose();
        _hwndSrc = null;
        _hooked = false;
        StartApp(keyEventFunc);
    }

    public void ShowWindow()
    {
        if (_window == null) throw new InvalidWindowException();
        
        DisableLightInput();
        _window.Dispatcher.Invoke(() =>
        {
            _window.Show();
            _window.Focus();
        });
    }

    public void HideWindow()
    {
        if (_window == null) throw new InvalidWindowException();
        
        EnableLightInput();
        _window.Dispatcher.Invoke(() =>
        {
            _window.Hide();
        });
    }

    public void DrawPoint(float absoluteX, float absoluteY)
    {
        _canvas?.Dispatcher.Invoke(() =>
        {
            var x = absoluteX * _canvas.ActualWidth;
            var y = absoluteY * _canvas.ActualHeight;

            _canvas.DrawPoint(x, y);
        });
    }

    public void ClearCanvas()
    {
        _canvas?.Dispatcher.Invoke(() => _canvas.ClearAll());
    }

    public void ResetCanvasPoint()
    {
        _canvas?.Dispatcher.Invoke(() => _canvas.ResetLastPoint());
    }
    
    private void DisableLightInput()
    {
        Blocker.DisableWinKey();
        DisableCursor();
    }

    private void EnableLightInput()
    {
        Blocker.EnableWinKey();
        EnableCursor();
    }

    private void DisableCursor()
    {
        if (_window == null) throw new InvalidWindowException("Cant disable the cursor when window is null");

        _window.Dispatcher.Invoke(() =>
        {
            // while (Blocker.ShowCursor(false) >= 0) { }
            Mouse.OverrideCursor = Cursors.Pen;
            _window.IsHitTestVisible = false;
        });
        
        Console.WriteLine("[SignGetter > Ui] Cursor has been disabled");
    }

    private void EnableCursor()
    {
        if (_window == null) throw new InvalidWindowException("Cant enable the cursor when window is null");

        _window.Dispatcher.Invoke(() =>
        {
            // while (Blocker.ShowCursor(true) < 0) { }
            Mouse.OverrideCursor = Cursors.Arrow;
            _window.IsHitTestVisible = true;
        });
        
        Console.WriteLine("[SignGetter > Ui] Cursor has been enabled");
    }

    public void Dispose()
    {
        if (_window == null) return;
        
        EnableLightInput();
        _window.Dispatcher.Invoke(() =>
        {
            _window.Close();
            if (_uiThread != null) Application.Current.Shutdown();
        });
        
        _window = null;
        _hwndSrc?.Dispose();
        _hooked = false;

        if (_uiThread == null) return;
        
        _uiThread.Join();
        _uiThread = null;
    }

    public void ShowMessage(Action<string> msgFunc, string msg)
    {
        try
        {
            EnableLightInput();
            msgFunc(msg);
            DisableCursor();
        }
        catch (InvalidWindowException ex)
        {
            Console.WriteLine("[SignGetter > Ui] Showing message error: {0}", ex.Message);
            msgFunc(msg);
        }
    }

    public bool AskMessage(Func<string, bool> msgFunc, string msg)
    {
        bool result;
        try
        {
            EnableLightInput();
            result = msgFunc(msg);
            DisableCursor();
        }
        catch (InvalidWindowException ex)
        {
            Console.WriteLine("[SignGetter > Ui] Asking message error: {0}", ex.Message);
            result = msgFunc(msg);
        }
        return result;
    }
}