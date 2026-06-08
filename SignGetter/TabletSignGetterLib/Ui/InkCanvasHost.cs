using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TabletSignGetterLib.Ui;

public class InkCanvasHost : FrameworkElement
{
    private readonly VisualCollection _visuals;
    private Point? _lastPoint;

    public InkCanvasHost()
    {
        _visuals = new VisualCollection(this);
    }

    private void DrawBackground()
    {
        var back = new DrawingVisual();
        using (var dc = back.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));
        }
        _visuals.Add(back);
    }

    public void DrawPoint(double x, double y)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            if (_lastPoint != null)
            {
                dc.DrawLine(new Pen(Brushes.Black, 2),
                    _lastPoint.Value,
                    new Point(x, y));
            }
            else
            {
                dc.DrawEllipse(Brushes.Black, null, new Point(x, y), 1, 1);
            }
        }
        _visuals.Add(dv);
        _lastPoint = new Point(x, y);
    }
    
    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _visuals.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _visuals[index];
    }
    
    public void ResetLastPoint() => _lastPoint = null;

    public void ClearAll()
    {
        ResetLastPoint();
        _visuals.Clear();
        DrawBackground();
    }
    
    public bool CheckEmpty() => _visuals.Count == 0;
}