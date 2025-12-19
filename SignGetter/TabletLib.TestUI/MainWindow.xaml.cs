using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TabletLib.Services;

namespace TabletLib.TestUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        _ = Test();
    }

    private async Task Test()
    {
        var tabletManager = new TabletManager(this, (data =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("= Report Received =");
                // Console.WriteLine($"- X coordinates: {data.X}");
                // Console.WriteLine($"- Y coordinates: {data.Y}");
                // Console.WriteLine($"- Pressure value: {data.Pressure}");
                // Console.WriteLine($"- TiltX value: {data.TiltX}");
                // Console.WriteLine($"- TiltY value: {data.TiltY}");
                // Console.WriteLine($"- Wheel value: {data.Wheel}");
                // Console.WriteLine($"- Pen is Down: {data.IsPenDown}");
                // Console.WriteLine($"- Eraser: {data.IsEraser}");
                // Console.WriteLine($"- Pen in Range: {data.InRange}");
                // Console.WriteLine($"- Button 1 pressed: {data.Button1}");
                // Console.WriteLine($"- Button 2 pressed: {data.Button2}");
                Console.WriteLine("= End of Report =\n");
            });
        }));
        tabletManager.OnErrorMessage += (message => {Console.WriteLine($"[Error] {message}");});
        tabletManager.OnWarningMessage += (message => {Console.WriteLine($"[Warning] {message}");});
        Console.WriteLine("First selection: " + tabletManager.SelectTablet());
        var deviceList = TabletManager.GetHidDevices();
        
        if (deviceList.Count > 0)
        {
            var i = 0;
            foreach (var device in deviceList)
            {
                Console.WriteLine($"[{i++}] - {device}");
            }
            // Console.Write("Choose device index: ");
            // var input = Console.ReadLine();
            // if (!int.TryParse(input, out int index))
            // {
            //     Console.WriteLine("Wrong index");
            //     return;
            // }
            // var selected = deviceList[index];
            // Console.WriteLine("Second selection: " + tabletManager.SelectTablet(selected));
        }
        else
        {
            Console.WriteLine("Device list is empty");
        }
        
        Console.WriteLine("=== Starting of Test ===");
        var started = await tabletManager.StartAsync();
        if (!started)
        {
            Console.WriteLine("Starting failed");
            return;
        }
        
        Console.WriteLine("= Tablet Info =");
        Console.WriteLine($"- Tablet status: {tabletManager.CurrentStatus}");
        Console.WriteLine($"- Device name: {tabletManager.TabletName}");
        Console.WriteLine($"- Device manufacturer: {tabletManager.TabletManufacturer}");
        Console.WriteLine("= End of Tablet Info =");
        
        while (true)
        {
            await Task.Delay(1000);
        }
        
        Console.WriteLine("=== Stopping test ===");
        tabletManager.Stop();
        
        tabletManager.Dispose();
    }
}