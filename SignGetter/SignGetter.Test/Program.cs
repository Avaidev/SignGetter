using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TabletSignGetterLib.Manager;

namespace SignGetter.Test;

public static class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            // var uiThread = new Thread(() =>
            // {
            //     var app = new Application();
            //     app.Run();
            // });
            // uiThread.SetApartmentState(ApartmentState.STA);
            // uiThread.Start();
            await Task.Delay(1000);
            Console.WriteLine(GetterManager.SelectTablet());

            return;
            
            IntPtr arrayPtr;
            int size;
            int width;
            int height;
            int stride;
            var result = GetterManager.GetSign(out arrayPtr, out size, out width, out height, out stride);
            
            Console.WriteLine($"Result: 0x{result:x}");
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
                    System.Windows.Media.PixelFormats.Pbgra32, null);

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

                var window = new Window
                {
                    Title = "Image Viewer",
                    Width = width,
                    Height = height,
                    Content = image,
                    UseLayoutRounding = true
                };

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