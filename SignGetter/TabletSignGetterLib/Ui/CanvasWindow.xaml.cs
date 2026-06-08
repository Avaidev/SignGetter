using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TabletSignGetterLib.Exceptions;
using TabletSignGetterLib.Models;
using TabletSignGetterLib.Utilities;

namespace TabletSignGetterLib.Ui;

public partial class CanvasWindow : Window
{
    private InkCanvasHost _canvas;
    
    public CanvasWindow(InkCanvasHost canvas)
    {
        InitializeComponent();
        
        _canvas = canvas;
        InkCanvasContainer.Child = _canvas;
        
        Loaded += (s, e) => { this.Focus(); };
        KeyDown += AppKeyDownEvent;
        
        Stylus.SetIsPressAndHoldEnabled(this, false);
        Stylus.SetIsFlicksEnabled(this, false);
    }
    
    public void DrawPoint(float absoluteX, float absoluteY)
    {
        var x = absoluteX * _canvas.ActualWidth;
        var y = absoluteY * _canvas.ActualHeight;
        _canvas.DrawPoint(x, y);
    }

    public void ClearCanvas()
    {
        _canvas.ClearAll();
    }
    
    public RenderTargetBitmap? RenderCanvas()
    {
        if (_canvas.CheckEmpty()) return null;
        
        var w = (int)_canvas.ActualWidth;
        var h = (int)_canvas.ActualHeight;
        
        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(_canvas);
        return rtb;
    }

    public void ResetCanvasPoint()
    {
        _canvas.ResetLastPoint();
    }

    private void AppKeyDownEvent(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape when Keyboard.Modifiers == ModifierKeys.None:
                e.Handled = true;
                HandleEscape();
                break;
            
            case Key.Escape when Keyboard.Modifiers == ModifierKeys.Shift:
                e.Handled = true;
                HandleShiftEscape();
                break;
            
            case Key.Z when Keyboard.Modifiers == ModifierKeys.Control:
                e.Handled = true;
                HandleCtrlZ();
                break;
            
            case Key.Enter:
                e.Handled = true;
                HandleEnter();
                break;
        }
    }

    private void HandleEscape()
    {
        GetterManager.EnableInput();
        var result = MessageService.AskYesNoMessage("Do you want to exit without saving?");
        GetterManager.DisableInput();

        if (result)
            GetterManager.RaiseOuterException(new BaseException(StatusCodes.Interrupted));
    }

    private void HandleEnter()
    {
        GetterManager.SaveResult();
    }

    private void HandleCtrlZ()
    {
        GetterManager.ReSign();
    }

    private void HandleShiftEscape()
    {
        var newMode = GetterManager.SetNextTabletMode();
        ModeLabel.Text = newMode.ToString().ToUpperInvariant();
    }
}