using System.Windows;
using System.Windows.Interop;

namespace TabletSignGetterLib.Services;

public class HookService : IDisposable
{
    private HwndSource? _hwndSrc;
    private readonly HwndSourceHook _hook;
    
    private IntPtr _windowHandler = IntPtr.Zero;
    private readonly object _lock =  new object();

    public bool Hooked { get; private set; } = false;

    public HookService(HwndSourceHook hook)
    {
        _hook = hook;
    }

    public IntPtr GetWindowHandler()
    {
        lock (_lock)
        {
            return _windowHandler;
        }
    }
    
    public void OnWindowInit(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        
        window.SourceInitialized -= OnWindowInit;
        
        lock (_lock)
        {
            _windowHandler = new WindowInteropHelper(window).Handle;
            _hwndSrc = HwndSource.FromHwnd(_windowHandler);
        }
            
        if (_hwndSrc is null) return;
        
        _hwndSrc.AddHook(_hook);
        Hooked = true;
    }

    public void Dispose()
    {
        _hwndSrc?.RemoveHook(_hook);
        _hwndSrc?.Dispose();
        _hwndSrc = null;
        Hooked = false;
    }
}