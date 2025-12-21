using System.Windows;
using System.Windows.Input;
using TabletLib.Services;


namespace TabletLib.TestUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private TabletManager? _tabletManager;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
        this.Closed += OnClosed;
    }
    
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();

        var tipStatus = false;
        var btn1Status = false;
        var btn2Status = false;
    
        _tabletManager = new TabletManager(this, data =>
        {
            try
            {
                if (data.TipPressed) tipStatus = true;
                if (data.TipUnPressed) tipStatus = false;
                
                if (data.Button1Pressed) btn1Status = true;
                if (data.Button1UnPressed) btn1Status = false;
                
                if (data.Button2Pressed) btn2Status = true;
                if (data.Button2UnPressed) btn2Status = false;
                
                Console.WriteLine("= Report Received =");
                Console.WriteLine($"- X coordinates: {data.X}");
                Console.WriteLine($"- Y coordinates: {data.Y}");
                Console.WriteLine($"- Tip Pressed: {tipStatus}");
                Console.WriteLine($"- Btn 1: {btn1Status}");
                Console.WriteLine($"- Btn 2: {btn2Status}");
                Console.WriteLine("= End of Report =\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Callback Error] {ex}");
            }
        });
    
        _tabletManager.OnErrorMessage += message => Console.WriteLine($"[Error] {message}");
        _tabletManager.OnWarningMessage += message => Console.WriteLine($"[Warning] {message}");
    
        Console.WriteLine("First selection: " + _tabletManager.SelectTablet());
    
        var deviceList = TabletManager.GetDevices();
        if (deviceList.Count > 0)
        {
            for (int i = 0; i < deviceList.Count; i++)
                Console.WriteLine($"[{i}] - {deviceList[i]}");
    
            // choose a device here if needed:
            // var selected = deviceList[index];
            // Console.WriteLine("Second selection: " + _tabletManager.SelectTablet(selected));
        }
        else
        {
            Console.WriteLine("Device list is empty");
        }
        
    
        Console.WriteLine("=== Starting test ===");
        bool started;
        try
        {
            started = await _tabletManager.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Starting failed: {ex}");
            return;
        }
    
        if (!started)
        {
            Console.WriteLine("Starting failed");
            return;
        }
    
        Console.WriteLine("= Tablet Info =");
        Console.WriteLine($"- Tablet status: {_tabletManager.CurrentStatus}");
        Console.WriteLine($"- Device name: {_tabletManager.TabletName}");
        Console.WriteLine($"- Device manufacturer: {_tabletManager.TabletManufacturer}");
        Console.WriteLine("= End of Tablet Info =");
    
        // Periodic heartbeat without blocking cleanup
        _ = Task.Run(async () =>
        {
            while (!_cts!.IsCancellationRequested)
            {
                await Task.Delay(1000, _cts.Token);
            }
        });
    }
    
    private void OnClosed(object? sender, EventArgs e)
    {
        _cts?.Cancel();

        if (_tabletManager != null)
        {
            try
            {
                Console.WriteLine("=== Stopping test ===");
                _tabletManager.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stop error: {ex}");
            }

            try
            {
                _tabletManager.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dispose error: {ex}");
            }
        }
    }
}