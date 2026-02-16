using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using System;
using System.Linq;

namespace MyShapeApp;

public partial class MainWindow : Window
{
    private BaseShape? _selectedShape; 
    private BaseShape? _activeShape;   
    private Point _offset;
    private bool _isDragging = false;
    private bool _isDraggingAnchor = false;
    private bool _isUpdatingCoords = false;

    public MainWindow() => InitializeComponent();

    // Методы создания фигур (теперь все классы существуют)
    private void AddRect(object? s, RoutedEventArgs e) => SetupShape(new MyRectangle { Center = GetCanvasCenter(), Anchor = GetCanvasCenter() }, 4);
    private void AddTriangle(object? s, RoutedEventArgs e) => SetupShape(new MyTriangle { Center = GetCanvasCenter(), Anchor = GetCanvasCenter() }, 3);
    private void AddTrapezoid(object? s, RoutedEventArgs e) => SetupShape(new MyTrapezoid { Center = GetCanvasCenter(), Anchor = GetCanvasCenter() }, 4);
    private void AddCircle(object? s, RoutedEventArgs e) => SetupShape(new MyCircle { Center = GetCanvasCenter(), Anchor = GetCanvasCenter() }, 1);
    private void AddPentagon(object? s, RoutedEventArgs e) => SetupShape(new MyPentagon { Center = GetCanvasCenter(), Anchor = GetCanvasCenter() }, 5);

    private void SetupShape(BaseShape shape, int sides) {
        var color = (CurrentColorDisplay.Fill as ISolidColorBrush)?.Color ?? Colors.Red;
        var thick = (double)(ThicknessControl.Value ?? 2);
        for (int i = 0; i < sides; i++) { shape.SideColors.Add(color); shape.Thicknesses.Add(thick); }
        MyCanvas.Shapes.Add(shape);
        _activeShape = shape;
        MyCanvas.InvalidateVisual();
        UpdateCoordDisplay();
    }

    private void UpdateCoordDisplay() {
        if (_activeShape != null) {
            _isUpdatingCoords = true;
            CoordX.Value = (decimal)Math.Round(_activeShape.Anchor.X);
            CoordY.Value = (decimal)Math.Round(_activeShape.Anchor.Y);
            
            // Расчет ваших новых свойств
            var rel = _activeShape.CenterRelativeToAnchor;
            TxtRelCenter.Text = $"Относ. центра: X:{Math.Round(rel.X)} Y:{Math.Round(rel.Y)}";
            TxtTopDist.Text = $"Верхняя гр. до якоря: {Math.Round(_activeShape.DistanceTopToAnchor)}";
            TxtBottomDist.Text = $"Нижняя гр. до якоря: {Math.Round(_activeShape.DistanceBottomToAnchor)}";
            
            _isUpdatingCoords = false;
        }
    }

    private void OnCoordChanged(object? sender, NumericUpDownValueChangedEventArgs e) {
        if (!_isUpdatingCoords && _activeShape != null) {
            _activeShape.Anchor = new Point((double)(CoordX.Value ?? 0), (double)(CoordY.Value ?? 0));
            MyCanvas.InvalidateVisual();
            UpdateCoordDisplay();
        }
    }

    private Point GetCanvasCenter() => new(MyCanvas.Bounds.Width / 2, MyCanvas.Bounds.Height / 2);

    private void SetColor(object? sender, RoutedEventArgs e) {
        if (sender is Button btn) CurrentColorDisplay.Fill = btn.Background;
    }

    private void OnFillClick(object? sender, RoutedEventArgs e) {
        if (_activeShape != null && CurrentColorDisplay.Fill is ISolidColorBrush brush) {
            _activeShape.FillColor = brush.Color;
            MyCanvas.InvalidateVisual();
        }
    }

    private void OnDown(object? s, PointerPressedEventArgs e) {
        var pos = e.GetPosition(MyCanvas);
        if (_activeShape != null && _activeShape.IsAnchorHit(pos)) {
            _selectedShape = _activeShape;
            _isDraggingAnchor = true;
            e.Pointer.Capture(MyCanvas);
            return;
        }

        foreach (var shape in MyCanvas.Shapes) shape.IsSelected = false;
        _selectedShape = MyCanvas.Shapes.LastOrDefault(x => x.IsHit(pos));
        
        if (_selectedShape != null) {
            _selectedShape.IsSelected = true;
            _activeShape = _selectedShape;
            _isDraggingAnchor = false;
            _offset = pos - _selectedShape.Center;
            UpdateCoordDisplay();
            e.Pointer.Capture(MyCanvas);
        }
        MyCanvas.InvalidateVisual();
    }

    private void OnMove(object? s, PointerEventArgs e) {
        if (_selectedShape != null) {
            var pos = e.GetPosition(MyCanvas);
            if (_isDraggingAnchor) {
                _selectedShape.Anchor = pos;
            } else {
                var oldCenter = _selectedShape.Center;
                _selectedShape.Center = pos - _offset;
                _selectedShape.Anchor += (_selectedShape.Center - oldCenter);
                _isDragging = true;
            }
            UpdateCoordDisplay();
            MyCanvas.InvalidateVisual();
        }
    }

    private void OnUp(object? s, PointerReleasedEventArgs e) {
        if (_selectedShape != null && !_isDragging && !_isDraggingAnchor) {
            int idx = _selectedShape.GetSideIndex(e.GetPosition(MyCanvas));
            if (idx != -1 && CurrentColorDisplay.Fill is ISolidColorBrush brush) {
                _selectedShape.SideColors[idx] = brush.Color;
                MyCanvas.InvalidateVisual();
            }
        }
        _selectedShape = null; _isDragging = false; _isDraggingAnchor = false;
        e.Pointer.Capture(null);
    }
}