using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TabletLib.Utilities;

namespace TabletSignGetterLib.Ui;

public class ApplicationHost(HwndSourceHook hook) : IDisposable
{
    private Thread? _uiThread;
    private InkCanvasHost? _canvas;
    private CanvasWindow? _window;
    private HwndSource? _hwndSrc;
    private bool _hooked;

    public IntPtr TargetWindowHandle { get; private set; } = IntPtr.Zero;
    public bool IsRunning => _uiThread is { IsAlive: true } && _hooked && Application.Current != null;

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
        if (TargetWindowHandle == IntPtr.Zero) throw new InvalidOperationException();
        _hwndSrc = HwndSource.FromHwnd(TargetWindowHandle);
        if (_hwndSrc is null) throw new InvalidOperationException(); //TODO Exceptions
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
        if (_uiThread != null || Application.Current != null)
        {
            Console.WriteLine("[SignGetter > AppHost] Cant start the app that is already started");
            return;
        }
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

    public void ShowWindow()
    {
        if (_uiThread != null && _window != null)
        {
            DisableLightInput();
            _window.Dispatcher.Invoke(() =>
            {
                _window.Show();
                _window.Focus();
            });
        }
    }

    public void HideWindow()
    {
        if (_uiThread != null && _window != null)
        {
            EnableLightInput();
            _window.Dispatcher.Invoke(() =>
            {
                _window.Hide();
            });
        }
    }
    
    public void ShutApp()
    {
        if (_uiThread != null && _window != null)
        {
            EnableLightInput();
            _window.Dispatcher.Invoke(() =>
            {
                _window.Close();
                Application.Current.Shutdown();
            });
            
            _uiThread.Join();
            _window = null;
            _hwndSrc?.Dispose();
            _hooked = false;
            _uiThread = null;
        }
    }

    public void DrawPoint(float absoluteX, float absoluteY)
    {
        _canvas?.Dispatcher.Invoke(() =>
        {
            var x = absoluteX * _canvas.ActualWidth;
            var y = absoluteY * _canvas.ActualHeight;

            _canvas.Dispatcher.Invoke(() => _canvas.DrawPoint(x, y));
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
        if (_uiThread == null || _window == null)
        {
            Console.WriteLine("[SignGetter > Ui] Cant disable cursor when the app or window is not available");
            return;
        }

        _window.Dispatcher.Invoke(() =>
        {
            while (Blocker.ShowCursor(false) >= 0) { }
            _window.IsHitTestVisible = false;
        });
        Console.WriteLine("[SignGetter > Ui] Cursor has been disabled");
    }

    private void EnableCursor()
    {
        if (_uiThread == null || _window == null)
        {
            Console.WriteLine("[SignGetter > Ui] Cant enable cursor when the app or window is not available");
            return;
        }

        _window.Dispatcher.Invoke(() =>
        {
            while (Blocker.ShowCursor(true) < 0)
            {
            }

            _window.Cursor = Cursors.Arrow;
            _window.IsHitTestVisible = true;
        });
        Console.WriteLine("[SignGetter > Ui] Cursor has been enabled");
    }

    public void Dispose()
    {
        _uiThread?.Interrupt();
        _hwndSrc?.Dispose();  
        _hooked = false;
    }

    public void ShowMessage(Action<string> msgFunc, string msg)
    {
        EnableLightInput();
        msgFunc(msg);
        DisableCursor();
    }

    public bool AskMessage(Func<string, bool> msgFunc, string msg)
    {
        EnableLightInput();
        var result = msgFunc(msg);
        DisableLightInput();
        return result;
    }
}