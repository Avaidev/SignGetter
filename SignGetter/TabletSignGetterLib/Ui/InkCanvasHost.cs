using System.Windows;
using System.Windows.Media;

namespace TabletSignGetterLib.Ui;

public class InkCanvasHost : FrameworkElement
{
    private readonly VisualCollection visuals;
    private Point? lastPoint;

    public InkCanvasHost()
    {
        visuals = new VisualCollection(this);
    }

    public void DrawPoint(double x, double y)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            if (lastPoint != null)
            {
                dc.DrawLine(new Pen(Brushes.Black, 2),
                    lastPoint.Value,
                    new Point(x, y));
            }
            else
            {
                dc.DrawEllipse(Brushes.Black, null, new Point(x, y), 1, 1);
            }
        }
        visuals.Add(dv);
        lastPoint = new Point(x, y);
    }
    
    protected override int VisualChildrenCount => visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= visuals.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return visuals[index];
    }
    
    public void ResetLastPoint() => lastPoint = null;

    public void ClearAll()
    {
        ResetLastPoint();
        visuals.Clear();
    }
    
    public bool IsEmpty() => visuals.Count == 0;
}