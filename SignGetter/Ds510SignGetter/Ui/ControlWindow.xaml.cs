using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Ds510SignGetter.Utilities;

namespace Ds510SignGetter.Ui;

public partial class ControlWindow : Window
{
    public ControlWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => DataContext = new ControlWindowViewModel(this);
    }

    public void ShowImage(string path)
    {
        if (File.Exists(path))
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = fs;
                bmp.EndInit();
                bmp.Freeze();

                SignImage.Source = bmp;
            }
            
            SignImage.Visibility = Visibility.Visible;
            if (DataContext is ControlWindowViewModel dc) dc.AcceptBtnModeChange(1);
        }
        else
        {
            MessageService.ErrorMessage("Error showing image: Invalid path");
            HideImage();
        }
    }

    public void HideImage()
    {
        SignImage.Source = null;
        SignImage.Visibility = Visibility.Collapsed;
        if (DataContext is ControlWindowViewModel dc) dc.AcceptBtnModeChange(0);
    }
    
}