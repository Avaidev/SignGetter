using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ds510SignGetter.Ui;

public class ApplicationHost : IDisposable
{
    private Thread? _uiThread;
    private ControlWindow? _window;
    
    public bool IsRunning => Application.Current != null && _window != null;

    public void StartApp()
    {
        if (_window != null)
        {
            Console.WriteLine("[SignGetter > AppHost] Cant start thet is already started");
            return;
        }

        if (Application.Current != null)
            Application.Current.Dispatcher.Invoke(() =>
            {
                _window = new ControlWindow();
                _window.Show();
            });
        else
        {
            _uiThread = new Thread(() =>
            {
                var app = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
                _window = new ControlWindow();

                app.Run(_window);
            });
            
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
        }
    }

    public void RestartApp()
    {
        _window?.Dispatcher.Invoke(() => _window.Close());
        _window = null;
        StartApp();
    }

    public void ShowWindow()
    {
        _window?.Dispatcher.Invoke(() =>
        {
            _window.Show();
            _window.Focus();
        }); 
    }

    public void ShowImage(string path)
    {
        _window?.Dispatcher.Invoke(() => _window.ShowImage(path));
    }

    public void HideImage()
    {
        _window?.Dispatcher.Invoke(() => _window.HideImage());
    }

    public void HideWindow()
    {
        _window?.Dispatcher.Invoke(() =>
        {
            _window?.Hide();
        });
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

        if (_uiThread == null) return;
        
        _uiThread.Join();
        _uiThread = null;
    }
}