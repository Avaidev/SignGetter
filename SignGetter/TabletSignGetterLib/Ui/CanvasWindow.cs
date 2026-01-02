using System.Windows;
using System.Windows.Input;

namespace TabletSignGetterLib.Ui;

public class CanvasWindow : Window
{
    public CanvasWindow(FrameworkElement content, KeyEventHandler keyDownFunc)
    {
        Title = "SignGetter";
        Content = content;
        WindowState = WindowState.Maximized;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;

        KeyDown += keyDownFunc;
    }
}