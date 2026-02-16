using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;

namespace MyShapeApp;

public class MyDrawingCanvas : Control 
{
    public List<BaseShape> Shapes { get; set; } = new List<BaseShape>();

    public override void Render(DrawingContext context)
    {
        // Заливаем фон, чтобы было где рисовать
        context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        foreach (var shape in Shapes)
        {
            shape.Draw(context);
        }
    }
}