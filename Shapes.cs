using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyShapeApp;

public abstract class BaseShape
{
    public string ShapeName { get; set; } = "Фигура";
    public Point Center { get; set; } 
    public Point Anchor { get; set; } 
    public List<Color> SideColors { get; set; } = new();
    public List<double> Thicknesses { get; set; } = new();
    public Color? FillColor { get; set; } = null;
    public bool IsSelected { get; set; } = false;

    public List<Vector> DistancesFromVerticesToAnchor => GetVertices().Select(v => (Vector)(v - Anchor)).ToList();
    public Vector CenterRelativeToAnchor => Center - Anchor;
    public double DistanceTopToAnchor => Anchor.Y - (GetVertices().Any() ? GetVertices().Min(v => v.Y) : Anchor.Y);
    public double DistanceBottomToAnchor => (GetVertices().Any() ? GetVertices().Max(v => v.Y) : Anchor.Y) - Anchor.Y;
    public double DistanceLeftToAnchor => Anchor.X - (GetVertices().Any() ? GetVertices().Min(v => v.X) : Anchor.X);

    public abstract Point[] GetVertices();
    public abstract void Draw(DrawingContext context);
    public abstract bool IsHit(Point point);

    public bool IsAnchorHit(Point p) => Math.Sqrt(Math.Pow(p.X - Anchor.X, 2) + Math.Pow(p.Y - Anchor.Y, 2)) < 15;

    public virtual int GetSideIndex(Point p)
    {
        var v = GetVertices();
        if (v.Length < 2) return -1;
        for (int i = 0; i < v.Length; i++)
            if (DistanceFromPointToLine(p, v[i], v[(i + 1) % v.Length]) < 25) return i;
        return -1;
    }

    public virtual Rect GetVisualBounds()
    {
        var vertices = GetVertices();
        if (vertices.Length == 0) return new Rect();

        double minX = vertices.Min(v => v.X), maxX = vertices.Max(v => v.X);
        double minY = vertices.Min(v => v.Y), maxY = vertices.Max(v => v.Y);

        int n = vertices.Length;
        if (n >= 2 && Thicknesses.Count == n)
        {
            for (int i = 0; i < n; i++)
            {
                int prev = (i + n - 1) % n, next = (i + 1) % n, nnext = (i + 2) % n;
                Point outStart = GetOffsetPoint(vertices[prev], vertices[i], vertices[next], Thicknesses[prev], Thicknesses[i], true);
                Point outEnd = GetOffsetPoint(vertices[i], vertices[next], vertices[nnext], Thicknesses[i], Thicknesses[next], true);
                Point inStart = GetOffsetPoint(vertices[prev], vertices[i], vertices[next], Thicknesses[prev], Thicknesses[i], false);
                Point inEnd = GetOffsetPoint(vertices[i], vertices[next], vertices[nnext], Thicknesses[i], Thicknesses[next], false);

                minX = Math.Min(minX, Math.Min(Math.Min(outStart.X, outEnd.X), Math.Min(inStart.X, inEnd.X)));
                maxX = Math.Max(maxX, Math.Max(Math.Max(outStart.X, outEnd.X), Math.Max(inStart.X, inEnd.X)));
                minY = Math.Min(minY, Math.Min(Math.Min(outStart.Y, outEnd.Y), Math.Min(inStart.Y, inEnd.Y)));
                maxY = Math.Max(maxY, Math.Max(Math.Max(outStart.Y, outEnd.Y), Math.Max(inStart.Y, inEnd.Y)));
            }
        }
        return new Rect(new Point(minX, minY), new Point(maxX, maxY));
    }

    protected void DrawSelectionEffects(DrawingContext context, Point[] vertices)
    {
        if (!IsSelected) return;
        var bounds = GetVisualBounds();
        if (bounds.Width == 0 && bounds.Height == 0) return;

        var dashPen = new Pen(Brushes.SkyBlue, 2, new DashStyle(new[] { 4.0, 2.0 }, 0));
        context.DrawRectangle(null, dashPen, bounds);

        var handlePen = new Pen(Brushes.DodgerBlue, 1.5);
        double hSize = 6, offset = hSize / 2;

        context.DrawRectangle(Brushes.White, handlePen, new Rect(bounds.Left - offset, bounds.Top - offset, hSize, hSize));
        context.DrawRectangle(Brushes.White, handlePen, new Rect(bounds.Right - offset, bounds.Top - offset, hSize, hSize));
        context.DrawRectangle(Brushes.White, handlePen, new Rect(bounds.Right - offset, bounds.Bottom - offset, hSize, hSize));
        context.DrawRectangle(Brushes.White, handlePen, new Rect(bounds.Left - offset, bounds.Bottom - offset, hSize, hSize));

        context.DrawEllipse(Brushes.Orange, new Pen(Brushes.White, 2), Anchor, 7, 7);
    }

    protected void DrawFill(DrawingContext context, Point[] vertices)
    {
        if (FillColor == null || vertices.Length < 3) return;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(vertices[0], true);
            for (int i = 1; i < vertices.Length; i++) ctx.LineTo(vertices[i]);
            ctx.EndFigure(true);
        }
        context.DrawGeometry(new SolidColorBrush(FillColor.Value), null, geometry);
    }

    protected void DrawPolygonWithMiter(DrawingContext context, Point[] vertices)
    {
        int n = vertices.Length;
        for (int i = 0; i < n; i++)
        {
            int prev = (i + n - 1) % n, next = (i + 1) % n, nnext = (i + 2) % n;
            Point outStart = GetOffsetPoint(vertices[prev], vertices[i], vertices[next], Thicknesses[prev], Thicknesses[i], true);
            Point outEnd = GetOffsetPoint(vertices[i], vertices[next], vertices[nnext], Thicknesses[i], Thicknesses[next], true);
            Point inStart = GetOffsetPoint(vertices[prev], vertices[i], vertices[next], Thicknesses[prev], Thicknesses[i], false);
            Point inEnd = GetOffsetPoint(vertices[i], vertices[next], vertices[nnext], Thicknesses[i], Thicknesses[next], false);

            var path = new StreamGeometry();
            using (var ctx = path.Open())
            {
                ctx.BeginFigure(outStart, true);
                ctx.LineTo(outEnd); ctx.LineTo(inEnd); ctx.LineTo(inStart);
                ctx.EndFigure(true);
            }
            context.DrawGeometry(new SolidColorBrush(SideColors[i]), null, path);
        }
    }

    protected Point GetOffsetPoint(Point pPrev, Point pCurr, Point pNext, double thickPrev, double thickCurr, bool outer)
    {
        Vector v1 = pCurr - pPrev, v2 = pNext - pCurr;
        Vector n1 = new Vector(-v1.Y, v1.X) / Math.Max(0.1, Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y)) * (thickPrev / 2) * (outer ? 1 : -1);
        Vector n2 = new Vector(-v2.Y, v2.X) / Math.Max(0.1, Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y)) * (thickCurr / 2) * (outer ? 1 : -1);
        return GetIntersection(pPrev + n1, pCurr + n1, pCurr + n2, pNext + n2);
    }

    public static Point GetIntersection(Point ps1, Point pe1, Point ps2, Point pe2)
    {
        double x1 = ps1.X, y1 = ps1.Y, x2 = pe1.X, y2 = pe1.Y, x3 = ps2.X, y3 = ps2.Y, x4 = pe2.X, y4 = pe2.Y;
        double denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
        if (Math.Abs(denom) < 0.0001) return ps1;
        return new Point(x1 + (((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom) * (x2 - x1), 
                         y1 + (((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom) * (y2 - y1));
    }

    protected double DistanceFromPointToLine(Point p, Point a, Point b)
    {
        double l2 = Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2);
        if (l2 == 0) return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));
        double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y)) / l2));
        return Math.Sqrt(Math.Pow(p.X - (a.X + t * (b.X - a.X)), 2) + Math.Pow(p.Y - (a.Y + t * (b.Y - a.Y)), 2));
    }
}

public class MyRectangle : BaseShape {
    public double Width { get; set; } = 150; public double Height { get; set; } = 100;
    public override Point[] GetVertices() => new[] {
        new Point(Center.X - Width/2, Center.Y - Height/2), new Point(Center.X + Width/2, Center.Y - Height/2),
        new Point(Center.X + Width/2, Center.Y + Height/2), new Point(Center.X - Width/2, Center.Y + Height/2)
    };
    public override void Draw(DrawingContext context) { var v = GetVertices(); DrawFill(context, v); DrawPolygonWithMiter(context, v); DrawSelectionEffects(context, v); }
    public override bool IsHit(Point p) => new Rect(Center.X - Width/2, Center.Y - Height/2, Width, Height).Contains(p);
}

public class MyTriangle : BaseShape {
    public double Width { get; set; } = 150; public double Height { get; set; } = 100;
    public override Point[] GetVertices() => new[] {
        new Point(Center.X, Center.Y - Height/2), new Point(Center.X + Width/2, Center.Y + Height/2), new Point(Center.X - Width/2, Center.Y + Height/2)
    };
    public override void Draw(DrawingContext context) { var v = GetVertices(); DrawFill(context, v); DrawPolygonWithMiter(context, v); DrawSelectionEffects(context, v); }
    public override bool IsHit(Point p) => new Rect(Center.X - Width/2, Center.Y - Height/2, Width, Height).Contains(p);
}

public class MyTrapezoid : BaseShape {
    public double Width { get; set; } = 200; public double Height { get; set; } = 100;
    public override Point[] GetVertices() => new[] {
        new Point(Center.X - Width * 0.3, Center.Y - Height/2), new Point(Center.X + Width * 0.3, Center.Y - Height/2),
        new Point(Center.X + Width/2, Center.Y + Height/2), new Point(Center.X - Width/2, Center.Y + Height/2)
    };
    public override void Draw(DrawingContext context) { var v = GetVertices(); DrawFill(context, v); DrawPolygonWithMiter(context, v); DrawSelectionEffects(context, v); }
    public override bool IsHit(Point p) => new Rect(Center.X - Width/2, Center.Y - Height/2, Width, Height).Contains(p);
}

public class MyPentagon : BaseShape {
    public double Radius { get; set; } = 80;
    public override Point[] GetVertices() {
        Point[] pts = new Point[5];
        for (int i = 0; i < 5; i++) {
            double a = i * 2 * Math.PI / 5 - Math.PI / 2;
            pts[i] = new Point(Center.X + Radius * Math.Cos(a), Center.Y + Radius * Math.Sin(a));
        }
        return pts;
    }
    public override void Draw(DrawingContext context) { var v = GetVertices(); DrawFill(context, v); DrawPolygonWithMiter(context, v); DrawSelectionEffects(context, v); }
    public override bool IsHit(Point p) => Math.Sqrt(Math.Pow(p.X - Center.X, 2) + Math.Pow(p.Y - Center.Y, 2)) < Radius;
}

public class MyCircle : BaseShape {
    public double Radius { get; set; } = 60;
    public override Point[] GetVertices() => new[] {
        new Point(Center.X - Radius, Center.Y - Radius), new Point(Center.X + Radius, Center.Y - Radius),
        new Point(Center.X + Radius, Center.Y + Radius), new Point(Center.X - Radius, Center.Y + Radius)
    };
    public override Rect GetVisualBounds() {
        double t = Thicknesses.Count > 0 ? Thicknesses[0] : 0;
        double offset = t / 2;
        return new Rect(Center.X - Radius - offset, Center.Y - Radius - offset, Radius * 2 + t, Radius * 2 + t);
    }
    public override int GetSideIndex(Point p) {
        double dist = Math.Sqrt(Math.Pow(p.X - Center.X, 2) + Math.Pow(p.Y - Center.Y, 2));
        if (Math.Abs(dist - Radius) < 15) return 0;
        return -1;
    }
    public override void Draw(DrawingContext context) {
        if (FillColor != null) context.DrawEllipse(new SolidColorBrush(FillColor.Value), null, Center, Radius, Radius);
        
        // ЗАЩИТА ОТ КРАША: Проверяем, есть ли цвета, прежде чем их брать!
        var strokeBrush = new SolidColorBrush(SideColors.Count > 0 ? SideColors[0] : Colors.Black);
        var thick = Thicknesses.Count > 0 ? Thicknesses[0] : 2;
        context.DrawEllipse(null, new Pen(strokeBrush, thick), Center, Radius, Radius);
        
        DrawSelectionEffects(context, GetVertices());
    }
    public override bool IsHit(Point p) => Math.Sqrt(Math.Pow(p.X - Center.X, 2) + Math.Pow(p.Y - Center.Y, 2)) <= Radius;
}

public class CustomPolygon : BaseShape
{
    public List<Point> RelativePoints { get; set; } = new();

    public override Point[] GetVertices() => RelativePoints.Select(p => new Point(Center.X + p.X, Center.Y + p.Y)).ToArray();

    public override void Draw(DrawingContext context)
    {
        var v = GetVertices();
        if (v.Length < 2) return;
        DrawFill(context, v);
        DrawPolygonWithMiter(context, v);
        DrawSelectionEffects(context, v);
    }

    public override bool IsHit(Point p)
    {
        var v = GetVertices();
        if (v.Length < 3) return false;
        bool result = false;
        int j = v.Length - 1;
        for (int i = 0; i < v.Length; i++) {
            if (v[i].Y < p.Y && v[j].Y >= p.Y || v[j].Y < p.Y && v[i].Y >= p.Y)
                if (v[i].X + (p.Y - v[i].Y) / (v[j].Y - v[i].Y) * (v[j].X - v[i].X) < p.X) result = !result;
            j = i;
        }
        return result;
    }
}

public class CompositeShape : BaseShape
{
    public List<BaseShape> SubShapes { get; set; } = new();

    public override Rect GetVisualBounds()
    {
        if (!SubShapes.Any()) return new Rect();
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var sub in SubShapes) {
            var b = sub.GetVisualBounds();
            minX = Math.Min(minX, b.Left); minY = Math.Min(minY, b.Top);
            maxX = Math.Max(maxX, b.Right); maxY = Math.Max(maxY, b.Bottom);
        }
        return new Rect(new Point(minX, minY), new Point(maxX, maxY));
    }

    public override Point[] GetVertices()
    {
        var b = GetVisualBounds();
        return new[] { new Point(b.Left, b.Top), new Point(b.Right, b.Top), new Point(b.Right, b.Bottom), new Point(b.Left, b.Bottom) };
    }

    public override void Draw(DrawingContext context)
    {
        foreach (var shape in SubShapes) shape.Draw(context);
        DrawSelectionEffects(context, GetVertices());
    }

    public override bool IsHit(Point p) => SubShapes.Any(s => s.IsHit(p));
}