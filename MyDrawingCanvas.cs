using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace MyShapeApp;

public class MyDrawingCanvas : Control 
{
    public List<BaseShape> Shapes { get; set; } = new List<BaseShape>();
    public List<Point> TempPolylinePts { get; set; } = new List<Point>();
    public Point? PreviewPoint { get; set; }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
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