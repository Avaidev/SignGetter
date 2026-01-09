using System.Windows;
using System.Windows.Media;

namespace Ds510SignGetter.Ui;

public class InkCanvasHost : FrameworkElement
{
    private readonly VisualCollection visuals;
    private Point? lastPoint;
    private float minP = 0.5f;
    private float maxP = 4.0f;

    public InkCanvasHost()
    {
        visuals = new VisualCollection(this);
    }

    private void ReCalculateParameters(ref float x, ref float y, ref float p)
    {
        x *= (float)ActualWidth;
        y *= (float)ActualHeight;
        p = p * (maxP - minP) + minP;
    }

    // public void DrawPoint(float x, float y, float p)
    // {
    //     var dv = new DrawingVisual();
    //     using (var dc = dv.RenderOpen())
    //     {
    //         ReCalculateParameters(ref x, ref y, ref p);
    //         if (lastPoint != null)
    //         {
    //             dc.DrawLine(new Pen(Brushes.Black, p),
    //                 lastPoint.Value,
    //                 new Point(x, y));
    //         }
    //         else
    //         {
    //             dc.DrawEllipse(Brushes.Black, null, new Point(x, y), p/2.0f, p/2.0f);
    //         }
    //     }
    //     visuals.Add(dv);
    //     lastPoint = new Point(x, y);
    // }
    
    private readonly List<Point> points = new();

    public void DrawPoint(float x, float y, float p)
    {
        ReCalculateParameters(ref x, ref y, ref p);
        var current = new Point(x, y);
        points.Add(current);

        if (points.Count < 4)
        {
            // пока мало точек — рисуем просто точку
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawEllipse(Brushes.Black, null, current, p / 2.0f, p / 2.0f);
            }
            visuals.Add(dv);
            return;
        }

        // Берём последние 4 точки для сплайна
        var p0 = points[points.Count - 4];
        var p1 = points[points.Count - 3];
        var p2 = points[points.Count - 2];
        var p3 = points[points.Count - 1];

        var dvSpline = new DrawingVisual();
        using (var dc = dvSpline.RenderOpen())
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(p1, false, false);

                // интерполяция сплайна
                for (double t = 0; t <= 1; t += 0.1)
                {
                    double t2 = t * t;
                    double t3 = t2 * t;

                    double xPos = 0.5 * ((2 * p1.X) +
                                         (-p0.X + p2.X) * t +
                                         (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 +
                                         (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3);

                    double yPos = 0.5 * ((2 * p1.Y) +
                                         (-p0.Y + p2.Y) * t +
                                         (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                                         (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);

                    ctx.LineTo(new Point(xPos, yPos), true, false);
                }
            }
            dc.DrawGeometry(null, new Pen(Brushes.Black, p), geometry);
        }
        visuals.Add(dvSpline);
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

    public void SetMinPressure(float p) => minP = p;
    public void SetMaxPressure(float p) => maxP = p;

    public (double, double, double, double) GetBoundaries() // MinX, MinY, MaxX, MaxY
    {
        var totalBounds = Rect.Empty;

        foreach (var visual in visuals)
        {
            var dv = (DrawingVisual)visual;
            var bounds = dv.ContentBounds;

            totalBounds.Union(bounds);
        }

        return (totalBounds.Left, totalBounds.Top, totalBounds.Right, totalBounds.Bottom);

    }
}