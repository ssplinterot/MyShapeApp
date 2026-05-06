using Avalonia;
using Avalonia.Media;
using System;
using System.IO;

namespace MyShapeApp;

public class CompositeShape : BaseShape
{
    public ShapeArray SubShapes { get; set; } = new();

    public override void SaveToStream(StreamWriter writer) {
        base.SaveToStream(writer);
        writer.WriteLine($"Вложенных фигур: {SubShapes.Count}");
        foreach(var s in SubShapes) {
            s.SaveToStream(writer); 
        }
    }
    
    public override void LoadFromStream(StreamReader reader) {
        base.LoadFromStream(reader);
        int count = int.Parse(ParseValue(reader.ReadLine()!));
        SubShapes.Clear();
        for(int i = 0; i < count; i++) {
            var shape = BaseShape.LoadShape(reader);
            if (shape != null) SubShapes.Add(shape);
        }
    }

    public override Rect GetVisualBounds()
    {
        if (SubShapes.Count == 0) return new Rect();
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

    public override bool IsHit(Point p)
    {
        foreach (var s in SubShapes) if (s.IsHit(p)) return true;
        return false;
    }

    // ИСПРАВЛЕНИЕ: Добавили метод, который требовал компилятор
    public override int GetSideIndex(Point p)
    {
        // У самой группы (контейнера) нет сторон, поэтому возвращаем -1
        return -1;
    }
}