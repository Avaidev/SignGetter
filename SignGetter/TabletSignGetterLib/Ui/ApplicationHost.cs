using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TabletSignGetterLib.Exceptions;
using TabletSignGetterLib.Services;
using TabletSignGetterLib.Utilities;

namespace TabletSignGetterLib.Ui;

public class ApplicationHost : IDisposable
{
    private readonly HookService _hookService;
    
    private Thread? _uiThread;
    private CanvasWindow? _window;
    
    public bool IsRunning => _window != null && _hookService.Hooked;
    
    public ApplicationHost(SignalService service)
    {
        _hookService = new HookService(service.WindowHookProcess);
    }

    public IntPtr GetHwndTarget() => _hookService.GetWindowHandler();
    
    public void Start()
    {
        if (_window != null) return;
        
        if (Application.Current != null) Application.Current.Dispatcher.Invoke(() =>
        {
            _window = new CanvasWindow(new InkCanvasHost());
            _window.SourceInitialized += _hookService.OnWindowInit;
            _window.Show();
            _window.Hide();
        });
        else
        {
            _uiThread = new Thread(() =>
            {
                var app = new Application();
                app.DispatcherUnhandledException += (s, args) => 
                {
                    GetterManager.RaiseOuterException(args.Exception);
                    args.Handled = true;
                };
                
                _window = new CanvasWindow(new InkCanvasHost());
                _window.SourceInitialized += _hookService.OnWindowInit;
                app.Run(_window);
            });
        
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
        }
    }
    
    public void ShowWindow()
    {
        if (_window == null) throw new WindowNotFoundException();
        
        DisableInput();
        _window.Dispatcher.Invoke(() =>
        {
            _window.Show();
            _window.Focus();
        });
    }
    
    public void HideWindow()
    {
        if (_window == null) throw new WindowNotFoundException();
        
        EnableInput();
        _window.Dispatcher.Invoke(() =>
        {
            _window.Hide();
        });
    }
    
    public void DisableInput()
    {
        Blocker.DisableWinKey();
        DisableCursor();
    }

    public void EnableInput()
    {
        Blocker.EnableWinKey();
        EnableCursor();
    }
    
    private void DisableCursor()
    {
        if (_window == null) throw new WindowNotFoundException();

        _window.Dispatcher.Invoke(() =>
        {
            Mouse.OverrideCursor = Cursors.None;
            _window.IsHitTestVisible = false;
        });
        
        Console.WriteLine("[SignGetter > Ui] Cursor has been disabled");
    }

    private void EnableCursor()
    {
        if (_window == null) throw new WindowNotFoundException();

        _window.Dispatcher.Invoke(() =>
        {
            Mouse.OverrideCursor = Cursors.Arrow;
            _window.IsHitTestVisible = true;
        });
        
        Console.WriteLine("[SignGetter > Ui] Cursor has been enabled");
    }

    public RenderTargetBitmap? RenderCanvas() => _window?.RenderCanvas();
    
    public void DrawPoint(float absoluteX, float absoluteY)
    {
        _window?.Dispatcher.Invoke(() => _window.DrawPoint(absoluteX, absoluteY));
    }

    public void ClearCanvas()
    {
        _window?.Dispatcher.Invoke(() => _window.ClearCanvas());
    }

    public void ResetCanvasPoint()
    {
        _window?.Dispatcher.Invoke(() => _window.ResetCanvasPoint());
    }
    
    public void Dispose()
    {
        if (_window == null) return;
        
        _window.Dispatcher.Invoke(() =>
        {
            _window.Close();
            if (_uiThread != null) Application.Current.Shutdown();
        });
        
        _window = null;
        _hookService.Dispose();

        if (_uiThread == null) return;
        
        _uiThread.Join();
        _uiThread = null;
    }
}