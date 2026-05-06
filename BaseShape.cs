using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;

namespace MyShapeApp;

public abstract class BaseShape
{
    public string ShapeName { get; set; } = "Фигура";
    public Point Center { get; set; } 
    public Point Anchor { get; set; } 
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
    public abstract int GetSideIndex(Point p);
    public abstract Rect GetVisualBounds();

    public virtual void SaveToStream(StreamWriter writer)
    {
        writer.WriteLine($"Тип: {GetType().Name}");
        writer.WriteLine($"Имя: {ShapeName}");
        writer.WriteLine($"Центр (X;Y): {Center.X.ToString(CultureInfo.InvariantCulture)};{Center.Y.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Якорь (X;Y): {Anchor.X.ToString(CultureInfo.InvariantCulture)};{Anchor.Y.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Заливка: {(FillColor.HasValue ? FillColor.Value.ToString() : "Нет")}");
    }

    public virtual void LoadFromStream(StreamReader reader)
    {
        ShapeName = ParseValue(reader.ReadLine()!);
        var c = ParseValue(reader.ReadLine()!).Split(';'); 
        Center = new Point(double.Parse(c[0], CultureInfo.InvariantCulture), double.Parse(c[1], CultureInfo.InvariantCulture));
        var a = ParseValue(reader.ReadLine()!).Split(';'); 
        Anchor = new Point(double.Parse(a[0], CultureInfo.InvariantCulture), double.Parse(a[1], CultureInfo.InvariantCulture));
        
        string fill = ParseValue(reader.ReadLine()!);
        FillColor = fill == "Нет" ? null : Color.Parse(fill);
    }

    protected string ParseValue(string line) => line.Substring(line.IndexOf(':') + 1).Trim();

    public static BaseShape? LoadShape(StreamReader reader)
    {
        string? typeLine = reader.ReadLine();
        while (typeLine != null && !typeLine.StartsWith("Тип:")) typeLine = reader.ReadLine();
        if (typeLine == null) return null;

        string typeName = typeLine.Substring(typeLine.IndexOf(':') + 1).Trim();
        Type? shapeType = Type.GetType($"MyShapeApp.{typeName}");
        if (shapeType == null) throw new Exception($"Класс '{typeName}' не найден в проекте!");

        BaseShape shape = (BaseShape)Activator.CreateInstance(shapeType)!;
        shape.LoadFromStream(reader); 
        return shape;
    }

    public bool IsAnchorHit(Point p) => Math.Sqrt(Math.Pow(p.X - Anchor.X, 2) + Math.Pow(p.Y - Anchor.Y, 2)) < 15;

    // ВОТ ОН: Единственный и правильный метод с пометкой virtual!
    protected virtual void DrawSelectionEffects(DrawingContext context, Point[] vertices)
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
        using (var ctx = geometry.Open()) {
            ctx.BeginFigure(vertices[0], true);
            for (int i = 1; i < vertices.Length; i++) ctx.LineTo(vertices[i]);
            ctx.EndFigure(true);
        }
        context.DrawGeometry(new SolidColorBrush(FillColor.Value), null, geometry);
    }
}