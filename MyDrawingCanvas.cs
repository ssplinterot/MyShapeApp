using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MyShapeApp;

// НОВЫЙ КЛАСС: Собственная реализация массива с функцией сдвига
public class ShapeArray : IEnumerable<BaseShape>
{
    private BaseShape[] _items = new BaseShape[1000]; // Массив на 1000 элементов
    public int Count { get; private set; } = 0;

    public void Add(BaseShape item)
    {
        if (Count < _items.Length)
        {
            _items[Count] = item; // Добавление в конец
            Count++;
        }
    }

    public void Remove(BaseShape item)
    {
        int index = Array.IndexOf(_items, item, 0, Count);
        if (index != -1)
        {
            // Процедура сдвига элементов влево, чтобы не было "пустых дыр"
            for (int i = index; i < Count - 1; i++)
            {
                _items[i] = _items[i + 1];
            }
            _items[Count - 1] = null!; // Зачищаем последний элемент
            Count--;
        }
    }

    public void Clear()
    {
        Array.Clear(_items, 0, Count);
        Count = 0;
    }

    public BaseShape this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public IEnumerator<BaseShape> GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return _items[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class MyDrawingCanvas : Control 
{
    // Заменили стандартный List на наш массив
    public ShapeArray Shapes { get; set; } = new ShapeArray();
    public List<Point> TempPolylinePts { get; set; } = new List<Point>();
    public Point? PreviewPoint { get; set; }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        // Процедура отображения: цикл пройдет только по заполненным элементам массива
        foreach (var shape in Shapes) shape.Draw(context);

        if (TempPolylinePts.Count > 0)
        {
            var pen = new Pen(Brushes.Black, 2);
            for (int i = 0; i < TempPolylinePts.Count - 1; i++) context.DrawLine(pen, TempPolylinePts[i], TempPolylinePts[i + 1]);
            foreach(var pt in TempPolylinePts) context.DrawEllipse(Brushes.Red, null, pt, 3, 3);

            if (PreviewPoint.HasValue)
            {
                var lastPt = TempPolylinePts[TempPolylinePts.Count - 1];
                var dashPen = new Pen(Brushes.Gray, 2, new DashStyle(new[] { 4.0, 4.0 }, 0));
                context.DrawLine(dashPen, lastPt, PreviewPoint.Value);

                double dx = PreviewPoint.Value.X - lastPt.X;
                double dy = PreviewPoint.Value.Y - lastPt.Y;
                double length = Math.Sqrt(dx * dx + dy * dy);
                double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

                var text = new FormattedText($"Длина: {Math.Round(length)}  Угол: {Math.Round(angle)}°",
                    System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Blue);
                context.DrawText(text, new Point(PreviewPoint.Value.X + 15, PreviewPoint.Value.Y + 15));

                if (TempPolylinePts.Count > 2)
                {
                    double distToStart = Math.Sqrt(Math.Pow(PreviewPoint.Value.X - TempPolylinePts[0].X, 2) + Math.Pow(PreviewPoint.Value.Y - TempPolylinePts[0].Y, 2));
                    if (distToStart < 15) context.DrawEllipse(null, new Pen(Brushes.Green, 2), TempPolylinePts[0], 7, 7);
                }
            }
        }
    }
}