using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ds510SignGetter.Manger;

namespace Ds510SignGetter.Test;

public static class Program
{
    private static Window? _window;
    
    public static async Task Main(string[] args)
    {
        try
        {
            var uiThread = new Thread(() =>
            {
                var app = new Application();
                _window = new Window();
                app.Run(_window);

                _window.Hide();
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            await Task.Delay(1000);

            var result = GetterManager.GetSign(out var arrayPtr, out var size, out var width, out var height, out var stride);
            
            Console.WriteLine($"Result: {result}");
            Console.WriteLine($"Size: {size}");
            Console.WriteLine($"Width: {width}");
            Console.WriteLine($"Height: {height}");
            await Task.Delay(1000);
            
            if (result != 0) return;
            
            byte[] buffer = new byte[size];
            Marshal.Copy(arrayPtr, buffer, 0, size);

            
            Application.Current.Dispatcher.Invoke(() =>
            {
                var wb = new WriteableBitmap(width, height, 96, 96, 
                    PixelFormats.Pbgra32, null);
    
                wb.WritePixels(
                    new Int32Rect(0, 0, width, height),
                    arrayPtr,
                    stride * height,
                    stride);
                   
                var image = new Image
                {
                    Source = wb,
                    Stretch = Stretch.Uniform,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

                if (_window != null)
                {
                    _window.Title = "Image Viewer";
                    _window.Height = height;
                    _window.Width = width;
                    _window.Content = image;
                    _window.UseLayoutRounding = true;
                }
                else _window = new Window
                {
                    Title = "Image Viewer",
                    Width = width,
                    Height = height,
                    Content = image,
                    UseLayoutRounding = true
                };
    
                _window.Show();
            });

            await Task.Delay(5000);
        }
        finally
        {
            _window?.Dispatcher.Invoke(() => _window?.Close());
            GetterManager.ShutGetter();
            GetterManager.ReleaseMemory();
        }
    }
}