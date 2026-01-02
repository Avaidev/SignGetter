using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TabletSignGetterLib.Manager;

namespace SignGetter.Test;

public static class Program
{
    private static CancellationTokenSource _cts = new();
    
    private static async Task Main(string[] args)
    {
        try
        {
            IntPtr arrayPtr;
            int size;
            var result = GetterManager.GetSign(out arrayPtr, out size);
            Console.WriteLine($"Result: 0x{result:x}");
            Console.WriteLine($"Size: {size}");
            await Task.Delay(1000);
            
            byte[] buffer = new byte[size];
            Marshal.Copy(arrayPtr, buffer, 0, size);

            Console.WriteLine("Enter width, height, stride");
            var width = Convert.ToInt32(Console.ReadLine());
            var height = Convert.ToInt32(Console.ReadLine());
            var stride = Convert.ToInt32(Console.ReadLine());

            Application.Current.Dispatcher.Invoke(() =>
            {
                var wb = new WriteableBitmap(width, height, 96, 96, 
                    System.Windows.Media.PixelFormats.Pbgra32, null);

                wb.WritePixels(
                    new Int32Rect(0, 0, width, height),
                    arrayPtr,
                    stride * height,
                    stride);
               
                var image = new Image
                {
                    Source = wb,
                    Stretch = Stretch.None, // avoid scaling blur
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

                var window = new Window
                {
                    Title = "Image Viewer",
                    Width = 1000,
                    Height = 1000,
                    Content = image,
                    UseLayoutRounding = true
                };

                Application.Current.MainWindow = window;
                window.Show();
            });

            await Task.Delay(5000);
        }
        finally
        {
            GetterManager.ShutGetter();
            GetterManager.ReleaseMemory();
        }
    }
}