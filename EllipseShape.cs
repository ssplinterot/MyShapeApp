using Avalonia;
using Avalonia.Media;
using System;
using System.IO;
using System.Globalization;

namespace MyShapeApp;

public class EllipseShape : BaseShape {
    public double RadiusX { get; set; } = 60;
    public double RadiusY { get; set; } = 40;
    public Color StrokeColor { get; set; } = Colors.Black;
    public double StrokeThickness { get; set; } = 2.0;
    public double Angle { get; set; } = 0; 
    
    public override void SaveToStream(StreamWriter writer) {
        base.SaveToStream(writer);
        writer.WriteLine($"Радиус X: {RadiusX.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Радиус Y: {RadiusY.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Угол: {Angle.ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine($"Цвет обводки: {StrokeColor}");
        writer.WriteLine($"Толщина обводки: {StrokeThickness.ToString(CultureInfo.InvariantCulture)}");
    }
    
    public override void LoadFromStream(StreamReader reader) {
        base.LoadFromStream(reader);
        RadiusX = double.Parse(ParseValue(reader.ReadLine()!), CultureInfo.InvariantCulture);
        RadiusY = double.Parse(ParseValue(reader.ReadLine()!), CultureInfo.InvariantCulture);
        Angle = double.Parse(ParseValue(reader.ReadLine()!), CultureInfo.InvariantCulture);
        StrokeColor = Color.Parse(ParseValue(reader.ReadLine()!));
        StrokeThickness = double.Parse(ParseValue(reader.ReadLine()!), CultureInfo.InvariantCulture);
    }

    public override Point[] GetVertices() => new[] {
        new Point(Center.X - RadiusX, Center.Y - RadiusY), new Point(Center.X + RadiusX, Center.Y - RadiusY),
        new Point(Center.X + RadiusX, Center.Y + RadiusY), new Point(Center.X - RadiusX, Center.Y + RadiusY)
    };
    
    public override Rect GetVisualBounds() {
        double offset = StrokeThickness / 2;
        return new Rect(Center.X - RadiusX - offset, Center.Y - RadiusY - offset, RadiusX * 2 + StrokeThickness, RadiusY * 2 + StrokeThickness);
    }

    public Point[] GetFoci() {
        double c = Math.Sqrt(Math.Abs(RadiusX * RadiusX - RadiusY * RadiusY));
        double dx = RadiusX >= RadiusY ? c : 0;
        double dy = RadiusX >= RadiusY ? 0 : c;

        double rad = Angle * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);

        Point f1 = new Point(Center.X + (dx * cos - dy * sin), Center.Y + (dx * sin + dy * cos));
        Point f2 = new Point(Center.X - (dx * cos - dy * sin), Center.Y - (dx * sin + dy * cos));
        return new[] { f1, f2 };
    }
    
    public override int GetSideIndex(Point p) {
        double rad = -Angle * Math.PI / 180.0;
        double rotX = (p.X - Center.X) * Math.Cos(rad) - (p.Y - Center.Y) * Math.Sin(rad);
        double rotY = (p.X - Center.X) * Math.Sin(rad) + (p.Y - Center.Y) * Math.Cos(rad);

        double a = Math.Max(0.1, RadiusX);
        double b = Math.Max(0.1, RadiusY);
        double val = (rotX * rotX) / (a * a) + (rotY * rotY) / (b * b);
        if (val >= 0.6 && val <= 1.4) return 0;
        return -1;
    }
    
    public override void Draw(DrawingContext context) {
        using (context.PushTransform(Matrix.CreateTranslation(-Center.X, -Center.Y) * Matrix.CreateRotation(Angle * Math.PI / 180.0) * Matrix.CreateTranslation(Center.X, Center.Y)))
        {
            if (FillColor != null) context.DrawEllipse(new SolidColorBrush(FillColor.Value), null, Center, RadiusX, RadiusY);
            context.DrawEllipse(null, new Pen(new SolidColorBrush(StrokeColor), StrokeThickness), Center, RadiusX, RadiusY);
        }
        DrawSelectionEffects(context, GetVertices());
    }

    protected override void DrawSelectionEffects(DrawingContext context, Point[] vertices) {
        if (!IsSelected) return;
        
        var bounds = GetVisualBounds();
        
        using (context.PushTransform(Matrix.CreateTranslation(-Center.X, -Center.Y) * Matrix.CreateRotation(Angle * Math.PI / 180.0) * Matrix.CreateTranslation(Center.X, Center.Y)))
        {
            var dashPen = new Pen(Brushes.SkyBlue, 2, new DashStyle(new[] { 4.0, 2.0 }, 0));
            context.DrawRectangle(null, dashPen, bounds);

            var handlePen = new Pen(Brushes.DodgerBlue, 1.5);
            double hSize = 6, offset = hSize / 2;

            // Теперь рисуем 8 маркеров выделения
            Point[] handles = {
                new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Top),
                new Point(bounds.Right, bounds.Bottom), new Point(bounds.Left, bounds.Bottom),
                new Point((bounds.Left + bounds.Right) / 2, bounds.Top), // Верхний центр
                new Point(bounds.Right, (bounds.Top + bounds.Bottom) / 2), // Правый центр
                new Point((bounds.Left + bounds.Right) / 2, bounds.Bottom), // Нижний центр
                new Point(bounds.Left, (bounds.Top + bounds.Bottom) / 2) // Левый центр
            };

            foreach(var pt in handles) {
                context.DrawRectangle(Brushes.White, handlePen, new Rect(pt.X - offset, pt.Y - offset, hSize, hSize));
            }
        }

        context.DrawEllipse(Brushes.Orange, new Pen(Brushes.White, 2), Anchor, 7, 7);

        var foci = GetFoci();
        var focusPen = new Pen(Brushes.DarkRed, 2);
        context.DrawEllipse(Brushes.Yellow, focusPen, foci[0], 6, 6);
        context.DrawEllipse(Brushes.Yellow, focusPen, foci[1], 6, 6);
    }
    
    public override bool IsHit(Point p) {
        double rad = -Angle * Math.PI / 180.0;
        double rotX = (p.X - Center.X) * Math.Cos(rad) - (p.Y - Center.Y) * Math.Sin(rad);
        double rotY = (p.X - Center.X) * Math.Sin(rad) + (p.Y - Center.Y) * Math.Cos(rad);

        double a = Math.Max(0.1, RadiusX);
        double b = Math.Max(0.1, RadiusY);
        return ((rotX * rotX) / (a * a) + (rotY * rotY) / (b * b)) <= 1;
    }

    public int CheckResizeHandle(Point p) {
        double rad = -Angle * Math.PI / 180.0;
        double rotX = Center.X + (p.X - Center.X) * Math.Cos(rad) - (p.Y - Center.Y) * Math.Sin(rad);
        double rotY = Center.Y + (p.X - Center.X) * Math.Sin(rad) + (p.Y - Center.Y) * Math.Cos(rad);
        
        var bounds = GetVisualBounds();
        Point[] handles = {
            new Point(bounds.Left, bounds.Top), new Point(bounds.Right, bounds.Top),
            new Point(bounds.Right, bounds.Bottom), new Point(bounds.Left, bounds.Bottom),
            new Point((bounds.Left + bounds.Right) / 2, bounds.Top), 
            new Point(bounds.Right, (bounds.Top + bounds.Bottom) / 2),
            new Point((bounds.Left + bounds.Right) / 2, bounds.Bottom), 
            new Point(bounds.Left, (bounds.Top + bounds.Bottom) / 2)
        };

        for (int i = 0; i < handles.Length; i++) {
            if (Math.Sqrt(Math.Pow(rotX - handles[i].X, 2) + Math.Pow(rotY - handles[i].Y, 2)) <= 10) return i;
        }
        return -1; // -1 значит никуда не попали
    }

    // Принимаем handleIndex, чтобы знать, ЧТО именно мы растягиваем
    public void Resize(Point p, int handleIndex) {
        double rad = -Angle * Math.PI / 180.0;
        double rotX = Center.X + (p.X - Center.X) * Math.Cos(rad) - (p.Y - Center.Y) * Math.Sin(rad);
        double rotY = Center.Y + (p.X - Center.X) * Math.Sin(rad) + (p.Y - Center.Y) * Math.Cos(rad);

        double offset = StrokeThickness / 2;
        double newRx = Math.Max(5, Math.Abs(rotX - Center.X) - offset);
        double newRy = Math.Max(5, Math.Abs(rotY - Center.Y) - offset);

        // Индексы 0-3: это 4 угла. Меняем оба радиуса.
        if (handleIndex >= 0 && handleIndex <= 3) {
            RadiusX = newRx;
            RadiusY = newRy;
        } 
        // Индексы 4 и 6: это Верх и Низ. Меняем только высоту (RadiusY).
        else if (handleIndex == 4 || handleIndex == 6) {
            RadiusY = newRy;
        } 
        // Индексы 5 и 7: это Право и Лево. Меняем только ширину (RadiusX).
        else if (handleIndex == 5 || handleIndex == 7) {
            RadiusX = newRx;
        }
    }
}