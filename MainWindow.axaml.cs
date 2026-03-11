using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MyShapeApp;

public partial class MainWindow : Window
{
    private BaseShape? _selectedShape; 
    private BaseShape? _activeShape;   
    private Point _offset;
    private bool _isDragging = false;
    private bool _isDraggingAnchor = false;
    private bool _isUpdatingCoords = false;

    private List<BaseShape> _multiSelectedShapes = new();
    private List<BaseShape> _savedTemplates = new();
    private CompositeShape? _currentParentGroup = null;

    public MainWindow() => InitializeComponent();

    private void AddRect(object? s, RoutedEventArgs e) => SetupShape(new MyRectangle { Center = GetCanvasCenter(), Anchor = GetCanvasCenter(), ShapeName = "Прямоугольник" }, 4);
    private void AddTriangle(object? s, RoutedEventArgs e) => SetupShape(new MyTriangle { Center = GetCanvasCenter(), Anchor = GetCanvasCenter(), ShapeName = "Треугольник" }, 3);
    private void AddTrapezoid(object? s, RoutedEventArgs e) => SetupShape(new MyTrapezoid { Center = GetCanvasCenter(), Anchor = GetCanvasCenter(), ShapeName = "Трапеция" }, 4);
    private void AddCircle(object? s, RoutedEventArgs e) => SetupShape(new MyCircle { Center = GetCanvasCenter(), Anchor = GetCanvasCenter(), ShapeName = "Круг" }, 1);
    private void AddPentagon(object? s, RoutedEventArgs e) => SetupShape(new MyPentagon { Center = GetCanvasCenter(), Anchor = GetCanvasCenter(), ShapeName = "Пятиугольник" }, 5);

    private void SetupShape(BaseShape shape, int sides) {
        var color = (CurrentColorDisplay.Fill as ISolidColorBrush)?.Color ?? Colors.Red;
        var thick = (double)(ThicknessControl.Value ?? 2);
        for (int i = 0; i < sides; i++) { shape.SideColors.Add(color); shape.Thicknesses.Add(thick); }
        MyCanvas.Shapes.Add(shape);
        
        ClearAllSelections(MyCanvas.Shapes);
        _multiSelectedShapes.Clear();
        shape.IsSelected = true;
        _multiSelectedShapes.Add(shape);
        _activeShape = shape;
        
        MyCanvas.InvalidateVisual();
        UpdateCoordDisplay();
    }

    private void UpdateCoordDisplay() {
        if (_activeShape != null) {
            _isUpdatingCoords = true;
            if (TxtShapeName.Text != _activeShape.ShapeName) TxtShapeName.Text = _activeShape.ShapeName;

            CoordX.Value = (decimal)Math.Round(_activeShape.Anchor.X);
            CoordY.Value = (decimal)Math.Round(_activeShape.Anchor.Y);
            
            var rel = _activeShape.CenterRelativeToAnchor;
            TxtRelCenter.Text = $"Относительно центра: X:{Math.Round(rel.X)} Y:{Math.Round(rel.Y)}";
            TxtTopDist.Text = $"До верхней границы: {Math.Round(_activeShape.DistanceTopToAnchor)}";
            TxtBottomDist.Text = $"До нижней границы: {Math.Round(_activeShape.DistanceBottomToAnchor)}";

            var dists = _activeShape.DistancesFromVerticesToAnchor;
            string info = "";
            for (int i = 0; i < dists.Count; i++) info += $"Угол {i + 1}: X:{Math.Round(dists[i].X)} Y:{Math.Round(dists[i].Y)}\n";
            TxtVerticesDist.Text = info;
            _isUpdatingCoords = false;
        } else {
            _isUpdatingCoords = true;
            TxtShapeName.Text = "";
            TxtRelCenter.Text = "Относительно центра: -"; TxtTopDist.Text = "До верхней границы: -";
            TxtBottomDist.Text = "До нижней границы: -"; TxtVerticesDist.Text = "Нет данных";
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

    private void OnShapeNameChanged(object? sender, TextChangedEventArgs e) {
        if (!_isUpdatingCoords && _activeShape != null && TxtShapeName.Text != null) {
            _activeShape.ShapeName = TxtShapeName.Text;
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

    private void ClearAllSelections(IEnumerable<BaseShape> shapes) {
        foreach(var s in shapes) {
            s.IsSelected = false;
            if (s is CompositeShape cs) ClearAllSelections(cs.SubShapes); 
        }
    }

    // НОВЫЙ МЕТОД: Рекурсивное перемещение фигуры и всех ее внутренних частей
    private void MoveShapeRecursively(BaseShape shape, Point delta) {
        var oldCenter = shape.Center;
        shape.Center = new Point(oldCenter.X + delta.X, oldCenter.Y + delta.Y);
        shape.Anchor = new Point(shape.Anchor.X + delta.X, shape.Anchor.Y + delta.Y);
        
        if (shape is CompositeShape cs) {
            foreach (var sub in cs.SubShapes) {
                MoveShapeRecursively(sub, delta);
            }
        }
    }

    private void OnDown(object? s, PointerPressedEventArgs e) {
        var pos = e.GetPosition(MyCanvas);
        bool isShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool isDeepSelect = e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.KeyModifiers.HasFlag(KeyModifiers.Meta); 

        if (BtnDrawLine.IsChecked == true) {
            if (isShift && MyCanvas.TempPolylinePts.Any()) {
                var last = MyCanvas.TempPolylinePts.Last();
                double angle = Math.Atan2(pos.Y - last.Y, pos.X - last.X);
                angle = Math.Round(angle / (Math.PI / 4)) * (Math.PI / 4);
                double dist = Math.Sqrt(Math.Pow(pos.X - last.X, 2) + Math.Pow(pos.Y - last.Y, 2));
                pos = new Point(last.X + Math.Cos(angle) * dist, last.Y + Math.Sin(angle) * dist);
            }
            if (MyCanvas.TempPolylinePts.Count > 2) {
                if (Math.Sqrt(Math.Pow(pos.X - MyCanvas.TempPolylinePts[0].X, 2) + Math.Pow(pos.Y - MyCanvas.TempPolylinePts[0].Y, 2)) < 15) {
                    FinishDrawingPolygon(); return;
                }
            }
            MyCanvas.TempPolylinePts.Add(pos);
            MyCanvas.InvalidateVisual(); return;
        }

        if (_activeShape != null && _activeShape.IsAnchorHit(pos)) {
            _selectedShape = _activeShape; _isDraggingAnchor = true;
            e.Pointer.Capture(MyCanvas); return;
        }

        BaseShape? hitShape = null;
        _currentParentGroup = null; 

        for (int i = MyCanvas.Shapes.Count - 1; i >= 0; i--) {
            var shape = MyCanvas.Shapes[i];
            if (shape.IsHit(pos)) {
                if (isDeepSelect && shape is CompositeShape cs) {
                    var innerHit = cs.SubShapes.LastOrDefault(s => s.IsHit(pos));
                    if (innerHit != null) { hitShape = innerHit; _currentParentGroup = cs; break; }
                }
                hitShape = shape; break;
            }
        }

        if (isShift && hitShape != null) {
            if (_multiSelectedShapes.Contains(hitShape)) {
                hitShape.IsSelected = false; _multiSelectedShapes.Remove(hitShape);
            } else {
                hitShape.IsSelected = true; _multiSelectedShapes.Add(hitShape);
            }
            MyCanvas.InvalidateVisual(); return;
        }

        ClearAllSelections(MyCanvas.Shapes);
        _multiSelectedShapes.Clear();

        _selectedShape = hitShape;
        
        if (_selectedShape != null) {
            _selectedShape.IsSelected = true; 
            _multiSelectedShapes.Add(_selectedShape);
            _activeShape = _selectedShape; 
            _isDraggingAnchor = false;
            _offset = pos - _selectedShape.Center;
            UpdateCoordDisplay();
            e.Pointer.Capture(MyCanvas);
        } else {
            _activeShape = null;
            UpdateCoordDisplay();
        }
        MyCanvas.InvalidateVisual();
    }

    private void OnMove(object? s, PointerEventArgs e) {
        var pos = e.GetPosition(MyCanvas);

        if (BtnDrawLine.IsChecked == true && MyCanvas.TempPolylinePts.Any()) {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) {
                var last = MyCanvas.TempPolylinePts.Last();
                double angle = Math.Atan2(pos.Y - last.Y, pos.X - last.X);
                angle = Math.Round(angle / (Math.PI / 4)) * (Math.PI / 4);
                double dist = Math.Sqrt(Math.Pow(pos.X - last.X, 2) + Math.Pow(pos.Y - last.Y, 2));
                pos = new Point(last.X + Math.Cos(angle) * dist, last.Y + Math.Sin(angle) * dist);
            }
            MyCanvas.PreviewPoint = pos;
            MyCanvas.InvalidateVisual(); return;
        } else {
            MyCanvas.PreviewPoint = null;
        }

        if (_selectedShape != null && BtnDrawLine.IsChecked == false) {
            if (_isDraggingAnchor) {
                _selectedShape.Anchor = pos;
            } else {
                var newCenter = pos - _offset;
                var delta = newCenter - _selectedShape.Center;

                foreach (var shape in _multiSelectedShapes) {
                    MoveShapeRecursively(shape, delta);
                }
                
                _isDragging = true;
            }
            UpdateCoordDisplay();
            MyCanvas.InvalidateVisual();
        }
    }

    private void OnUp(object? s, PointerReleasedEventArgs e) {
        if (_selectedShape != null && !_isDragging && !_isDraggingAnchor && BtnDrawLine.IsChecked == false) {
            var pos = e.GetPosition(MyCanvas);
            int idx = _selectedShape.GetSideIndex(pos);
            
            if (idx != -1 && idx < _selectedShape.SideColors.Count) {
                if (CurrentColorDisplay.Fill is ISolidColorBrush brush) {
                    _selectedShape.SideColors[idx] = brush.Color;
                }
                _selectedShape.Thicknesses[idx] = (double)(ThicknessControl.Value ?? 2);
                MyCanvas.InvalidateVisual();
            }
        }
        _selectedShape = null; 
        _isDragging = false; 
        _isDraggingAnchor = false;
        e.Pointer.Capture(null);
    }

    private void FinishDrawingPolygon() {
        if (MyCanvas.TempPolylinePts.Count > 2) {
            double cx = MyCanvas.TempPolylinePts.Average(p => p.X), cy = MyCanvas.TempPolylinePts.Average(p => p.Y);
            Point center = new Point(cx, cy);

            var poly = new CustomPolygon { Center = center, Anchor = center, ShapeName = string.IsNullOrWhiteSpace(TxtShapeName.Text) ? "Кастомная" : TxtShapeName.Text };
            var color = (CurrentColorDisplay.Fill as ISolidColorBrush)?.Color ?? Colors.Red;
            var thick = (double)(ThicknessControl.Value ?? 2);

            foreach (var p in MyCanvas.TempPolylinePts) {
                poly.RelativePoints.Add(new Point(p.X - cx, p.Y - cy));
                poly.SideColors.Add(color); poly.Thicknesses.Add(thick);
            }
            MyCanvas.Shapes.Add(poly);
            ClearAllSelections(MyCanvas.Shapes);
            _multiSelectedShapes.Clear();
            poly.IsSelected = true;
            _multiSelectedShapes.Add(poly);
            _activeShape = poly;
        }
        MyCanvas.TempPolylinePts.Clear(); MyCanvas.PreviewPoint = null; BtnDrawLine.IsChecked = false;
        MyCanvas.InvalidateVisual();
    }

    private void OnCancelDrawClick(object? sender, RoutedEventArgs e) {
        MyCanvas.TempPolylinePts.Clear(); MyCanvas.PreviewPoint = null; BtnDrawLine.IsChecked = false;
        MyCanvas.InvalidateVisual();
    }

    private void OnDrawLineToggle(object? sender, RoutedEventArgs e) {
        if (BtnDrawLine.IsChecked == false) { MyCanvas.TempPolylinePts.Clear(); MyCanvas.PreviewPoint = null; MyCanvas.InvalidateVisual(); }
    }

    private void OnGroupClick(object? sender, RoutedEventArgs e) {
        if (_multiSelectedShapes.Count < 2) return;
        var composite = new CompositeShape { ShapeName = string.IsNullOrWhiteSpace(TxtShapeName.Text) ? "Группа" : TxtShapeName.Text };
        double cx = _multiSelectedShapes.Average(s => s.Center.X), cy = _multiSelectedShapes.Average(s => s.Center.Y);
        composite.Center = new Point(cx, cy); composite.Anchor = composite.Center;

        foreach (var shape in _multiSelectedShapes) { composite.SubShapes.Add(shape); MyCanvas.Shapes.Remove(shape); }
        
        MyCanvas.Shapes.Add(composite);
        _multiSelectedShapes.Clear(); _multiSelectedShapes.Add(composite);
        composite.IsSelected = true; _activeShape = composite;
        MyCanvas.InvalidateVisual();
    }

    private void OnUngroupClick(object? sender, RoutedEventArgs e) {
        var toRemove = new List<CompositeShape>();
        foreach (var shape in _multiSelectedShapes.OfType<CompositeShape>()) {
            toRemove.Add(shape);
            foreach (var sub in shape.SubShapes) { MyCanvas.Shapes.Add(sub); sub.IsSelected = true; _multiSelectedShapes.Add(sub); }
        }
        foreach(var c in toRemove) { MyCanvas.Shapes.Remove(c); _multiSelectedShapes.Remove(c); }
        MyCanvas.InvalidateVisual();
    }

    private void OnExtractClick(object? sender, RoutedEventArgs e) {
        if (_activeShape != null && _currentParentGroup != null) {
            _currentParentGroup.SubShapes.Remove(_activeShape);
            MyCanvas.Shapes.Add(_activeShape);
            if (_currentParentGroup.SubShapes.Count == 0) MyCanvas.Shapes.Remove(_currentParentGroup);
            _currentParentGroup = null;
            MyCanvas.InvalidateVisual();
        }
    }

    private BaseShape CloneShape(BaseShape original) {
        BaseShape clone;
        if (original is MyRectangle r) clone = new MyRectangle { Width = r.Width, Height = r.Height };
        else if (original is MyTriangle t) clone = new MyTriangle { Width = t.Width, Height = t.Height };
        else if (original is MyTrapezoid tr) clone = new MyTrapezoid { Width = tr.Width, Height = tr.Height };
        else if (original is MyCircle c) clone = new MyCircle { Radius = c.Radius };
        else if (original is MyPentagon p) clone = new MyPentagon { Radius = p.Radius };
        else if (original is CustomPolygon cp) clone = new CustomPolygon { RelativePoints = new List<Point>(cp.RelativePoints) };
        else if (original is CompositeShape comp) {
            var newComp = new CompositeShape();
            foreach(var sub in comp.SubShapes) newComp.SubShapes.Add(CloneShape(sub));
            clone = newComp;
        } else throw new NotSupportedException($"Невозможно клонировать: {original.GetType().Name}");

        clone.ShapeName = original.ShapeName; clone.FillColor = original.FillColor;
        clone.SideColors = new List<Color>(original.SideColors); clone.Thicknesses = new List<double>(original.Thicknesses);
        clone.Center = original.Center; clone.Anchor = original.Anchor;
        return clone;
    }

    private void OnSaveToLibraryClick(object? sender, RoutedEventArgs e) {
        if (_activeShape != null) {
            string name = string.IsNullOrWhiteSpace(TxtShapeName.Text) ? "Новый шаблон" : TxtShapeName.Text;
            BaseShape templateShape = CloneShape(_activeShape);
            templateShape.ShapeName = name;
            _savedTemplates.Add(templateShape);
            
            var emptyItem = MenuLibrary.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "EmptyLibraryItem");
            if (emptyItem != null) MenuLibrary.Items.Remove(emptyItem);
            
            var menuItem = new MenuItem { Header = name };
            menuItem.Click += (s, args) => {
                var clone = CloneShape(templateShape);
                var center = GetCanvasCenter(); var offset = center - clone.Center;
                clone.Center = center; clone.Anchor += offset;
                
                if (clone is CompositeShape cs) {
                    foreach (var sub in cs.SubShapes) { sub.Center += offset; sub.Anchor += offset; }
                }

                MyCanvas.Shapes.Add(clone);
                ClearAllSelections(MyCanvas.Shapes); _multiSelectedShapes.Clear();
                clone.IsSelected = true; _multiSelectedShapes.Add(clone); _activeShape = clone;
                
                UpdateCoordDisplay(); MyCanvas.InvalidateVisual();
            };
            MenuLibrary.Items.Add(menuItem);
            TxtShapeName.Text = ""; 
        }
    }
}