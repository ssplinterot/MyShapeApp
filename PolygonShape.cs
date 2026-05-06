using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;

namespace MyShapeApp;

public class PolygonShape : BaseShape
{
    public List<Point> RelativePoints { get; set; } = new();
    public List<Color> SideColors { get; set; } = new();
    public List<double> Thicknesses { get; set; } = new();

    public override void SaveToStream(StreamWriter writer) {
        base.SaveToStream(writer); 
        writer.WriteLine($"Количество точек: {RelativePoints.Count}");
        
        string[] sideNames = { "Верхняя сторона", "Правая сторона", "Нижняя сторона", "Левая сторона" };
        for(int i = 0; i < RelativePoints.Count; i++) {
            writer.WriteLine($"Точка: {RelativePoints[i].X.ToString(CultureInfo.InvariantCulture)};{RelativePoints[i].Y.ToString(CultureInfo.InvariantCulture)}");
            
            string sName = (RelativePoints.Count == 4 && i < 4) ? sideNames[i] : $"Сторона {i + 1}";
            writer.WriteLine($"{sName}: Цвет={SideColors[i]}, Толщина={Thicknesses[i].ToString(CultureInfo.InvariantCulture)}px");
        }
    }
    
    public override void LoadFromStream(StreamReader reader) {
        base.LoadFromStream(reader);
        int count = int.Parse(ParseValue(reader.ReadLine()!));
        RelativePoints.Clear(); SideColors.Clear(); Thicknesses.Clear();
        
        for(int i = 0; i < count; i++) {
            var pts = ParseValue(reader.ReadLine()!).Split(';');
            RelativePoints.Add(new Point(double.Parse(pts[0], CultureInfo.InvariantCulture), double.Parse(pts[1], CultureInfo.InvariantCulture)));
            
            string lineInfo = ParseValue(reader.ReadLine()!); 
            string[] parts = lineInfo.Split(new[] { "Цвет=", ", Толщина=" }, StringSplitOptions.None);
            SideColors.Add(Color.Parse(parts[1]));
            Thicknesses.Add(double.Parse(parts[2].Replace("px", "").Trim(), CultureInfo.InvariantCulture));
        }
    }

    public override Point[] GetVertices() => RelativePoints.Select(p => new Point(Center.X + p.X, Center.Y + p.Y)).ToArray();

    public override Rect GetVisualBounds()
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

    public override int GetSideIndex(Point p)
    {
        var v = GetVertices();
        if (v.Length < 2) return -1;
        for (int i = 0; i < v.Length; i++) {
            double l2 = Math.Pow(v[i].X - v[(i + 1) % v.Length].X, 2) + Math.Pow(v[i].Y - v[(i + 1) % v.Length].Y, 2);
            double t = l2 == 0 ? 0 : Math.Max(0, Math.Min(1, ((p.X - v[i].X) * (v[(i + 1) % v.Length].X - v[i].X) + (p.Y - v[i].Y) * (v[(i + 1) % v.Length].Y - v[i].Y)) / l2));
            double dist = Math.Sqrt(Math.Pow(p.X - (v[i].X + t * (v[(i + 1) % v.Length].X - v[i].X)), 2) + Math.Pow(p.Y - (v[i].Y + t * (v[(i + 1) % v.Length].Y - v[i].Y)), 2));
            if (dist < 25) return i;
        }
        return -1;
    }

    public override void Draw(DrawingContext context) {
        var v = GetVertices();
        if (v.Length < 2) return;
        DrawFill(context, v);

        int n = v.Length;
        for (int i = 0; i < n; i++) {
            int prev = (i + n - 1) % n, next = (i + 1) % n, nnext = (i + 2) % n;
            Point outStart = GetOffsetPoint(v[prev], v[i], v[next], Thicknesses[prev], Thicknesses[i], true);
            Point outEnd = GetOffsetPoint(v[i], v[next], v[nnext], Thicknesses[i], Thicknesses[next], true);
            Point inStart = GetOffsetPoint(v[prev], v[i], v[next], Thicknesses[prev], Thicknesses[i], false);
            Point inEnd = GetOffsetPoint(v[i], v[next], v[nnext], Thicknesses[i], Thicknesses[next], false);

            var path = new StreamGeometry();
            using (var ctx = path.Open()) {
                ctx.BeginFigure(outStart, true);
                ctx.LineTo(outEnd); ctx.LineTo(inEnd); ctx.LineTo(inStart);
                ctx.EndFigure(true);
            }
            context.DrawGeometry(new SolidColorBrush(SideColors[i]), null, path);
        }
        DrawSelectionEffects(context, v);
    }

    private Point GetOffsetPoint(Point pPrev, Point pCurr, Point pNext, double thickPrev, double thickCurr, bool outer) {
        Vector v1 = pCurr - pPrev, v2 = pNext - pCurr;
        Vector n1 = new Vector(-v1.Y, v1.X) / Math.Max(0.1, Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y)) * (thickPrev / 2) * (outer ? 1 : -1);
        Vector n2 = new Vector(-v2.Y, v2.X) / Math.Max(0.1, Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y)) * (thickCurr / 2) * (outer ? 1 : -1);
        double denom = (pCurr.Y + n2.Y - (pNext.Y + n2.Y)) * (pCurr.X + n1.X - (pPrev.X + n1.X)) - (pCurr.X + n2.X - (pNext.X + n2.X)) * (pCurr.Y + n1.Y - (pPrev.Y + n1.Y));
        if (Math.Abs(denom) < 0.0001) return pPrev + n1;
        return new Point((pPrev.X + n1.X) + (((pCurr.X + n2.X - (pNext.X + n2.X)) * ((pPrev.Y + n1.Y) - (pNext.Y + n2.Y)) - (pCurr.Y + n2.Y - (pNext.Y + n2.Y)) * ((pPrev.X + n1.X) - (pNext.X + n2.X))) / denom) * (pCurr.X + n1.X - (pPrev.X + n1.X)), 
                         (pPrev.Y + n1.Y) + (((pCurr.X + n2.X - (pNext.X + n2.X)) * ((pPrev.Y + n1.Y) - (pNext.Y + n2.Y)) - (pCurr.Y + n2.Y - (pNext.Y + n2.Y)) * ((pPrev.X + n1.X) - (pNext.X + n2.X))) / denom) * (pCurr.Y + n1.Y - (pPrev.Y + n1.Y)));
    }

    public override bool IsHit(Point p) {
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