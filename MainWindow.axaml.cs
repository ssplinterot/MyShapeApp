using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;
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
    private bool _isRotatingEllipse = false; 
    private bool _isResizingEllipse = false; 
    private int _resizeHandleIndex = -1; // ЗАПОМИНАЕТ ИНДЕКС МАРКЕРА
    private bool _isUpdatingCoords = false;

    private List<BaseShape> _multiSelectedShapes = new();
    private List<BaseShape> _savedTemplates = new();
    private CompositeShape? _currentParentGroup = null;

    public MainWindow() => InitializeComponent();

    private void AddRect(object? s, RoutedEventArgs e) {
        var poly = new PolygonShape { ShapeName = "Прямоугольник", Center = GetCanvasCenter(), Anchor = GetCanvasCenter() };
        poly.RelativePoints = new List<Point> { new(-75, -50), new(75, -50), new(75, 50), new(-75, 50) };
        SetupPolygon(poly);
    }
    private void AddTriangle(object? s, RoutedEventArgs e) {
        var poly = new PolygonShape { ShapeName = "Треугольник", Center = GetCanvasCenter(), Anchor = GetCanvasCenter() };
        poly.RelativePoints = new List<Point> { new(0, -50), new(75, 50), new(-75, 50) };
        SetupPolygon(poly);
    }
    private void AddTrapezoid(object? s, RoutedEventArgs e) {
        var poly = new PolygonShape { ShapeName = "Трапеция", Center = GetCanvasCenter(), Anchor = GetCanvasCenter() };
        poly.RelativePoints = new List<Point> { new(-60, -50), new(60, -50), new(100, 50), new(-100, 50) };
        SetupPolygon(poly);
    }
    private void AddPentagon(object? s, RoutedEventArgs e) {
        var poly = new PolygonShape { ShapeName = "Пятиугольник", Center = GetCanvasCenter(), Anchor = GetCanvasCenter() };
        for (int i = 0; i < 5; i++) {
            double a = i * 2 * Math.PI / 5 - Math.PI / 2;
            poly.RelativePoints.Add(new Point(80 * Math.Cos(a), 80 * Math.Sin(a)));
        }
        SetupPolygon(poly);
    }
    
    private void AddCircle(object? s, RoutedEventArgs e) {
        var color = (CurrentColorDisplay.Fill as ISolidColorBrush)?.Color ?? Colors.Red;
        var thick = (double)(ThicknessControl.Value ?? 2);
        var circ = new EllipseShape { ShapeName = "Круг", Center = GetCanvasCenter(), Anchor = GetCanvasCenter(), RadiusX = 60, RadiusY = 60, StrokeColor = color, StrokeThickness = thick };
        FinalizeShapeSetup(circ);
    }

    private void AddEllipse(object? s, RoutedEventArgs e) {
        var color = (CurrentColorDisplay.Fill as ISolidColorBrush)?.Color ?? Colors.Red;
        var thick = (double)(ThicknessControl.Value ?? 2);
        var el = new EllipseShape { ShapeName = "Эллипс", Center = GetCanvasCenter(), Anchor = GetCanvasCenter(), RadiusX = 80, RadiusY = 40, StrokeColor = color, StrokeThickness = thick };
        FinalizeShapeSetup(el);
    }

    private void SetupPolygon(PolygonShape shape) {
        var color = (CurrentColorDisplay.Fill as ISolidColorBrush)?.Color ?? Colors.Red;
        var thick = (double)(ThicknessControl.Value ?? 2);
        for (int i = 0; i < shape.RelativePoints.Count; i++) { 
            shape.SideColors.Add(color); 
            shape.Thicknesses.Add(thick); 
        }
        FinalizeShapeSetup(shape);
    }

    private void FinalizeShapeSetup(BaseShape shape) {
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

            if (_activeShape is CompositeShape cs) {
                string content = GetGroupContentString(cs, 1);
                TxtGroupContent.Text = string.IsNullOrWhiteSpace(content) ? "Группа пуста" : content;
            } else {
                TxtGroupContent.Text = "Это одиночная фигура.";
            }

            _isUpdatingCoords = false;
        } else {
            _isUpdatingCoords = true;
            TxtShapeName.Text = "";
            TxtRelCenter.Text = "Относительно центра: -"; TxtTopDist.Text = "До верхней границы: -";
            TxtBottomDist.Text = "До нижней границы: -"; TxtVerticesDist.Text = "Нет данных";
            TxtGroupContent.Text = "Не выбрана фигура"; 
            _isUpdatingCoords = false;
        }
    }

    private string GetGroupContentString(CompositeShape group, int level) {
        string result = "";
        string indent = new string('-', level) + " "; 
        foreach(var shape in group.SubShapes) {
            string typeName = shape.GetType().Name; 
            if (typeName == "CompositeShape") typeName = "Группа";
            else if (typeName == "PolygonShape") typeName = "Многоугольник";
            else if (typeName == "EllipseShape") typeName = "Круг/Эллипс";
            
            result += $"{indent}{shape.ShapeName} ({typeName})\n";
            if (shape is CompositeShape subGroup) result += GetGroupContentString(subGroup, level + 2); 
        }
        return result.TrimEnd();
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

    private void ClearAllSelections(IEnumerable<BaseShape> shapes) {
        foreach(var s in shapes) {
            s.IsSelected = false;
            if (s is CompositeShape cs) ClearAllSelections(cs.SubShapes); 
        }
    }

    private void MoveShapeRecursively(BaseShape shape, Point delta) {
        var oldCenter = shape.Center;
        shape.Center = new Point(oldCenter.X + delta.X, oldCenter.Y + delta.Y);
        shape.Anchor = new Point(shape.Anchor.X + delta.X, shape.Anchor.Y + delta.Y);
        
        if (shape is CompositeShape cs) {
            foreach (var sub in cs.SubShapes) MoveShapeRecursively(sub, delta);
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

        if (_activeShape is EllipseShape ellipseForResize) {
            int handle = ellipseForResize.CheckResizeHandle(pos);
            if (handle != -1) {
                _selectedShape = ellipseForResize;
                _isResizingEllipse = true;
                _resizeHandleIndex = handle; // Сохраняем, за какую из 8 точек тянем!
                e.Pointer.Capture(MyCanvas);
                return;
            }
        }

        if (_activeShape is EllipseShape activeEllipse) {
            var foci = activeEllipse.GetFoci();
            if (Math.Sqrt(Math.Pow(pos.X - foci[0].X, 2) + Math.Pow(pos.Y - foci[0].Y, 2)) < 10 || 
                Math.Sqrt(Math.Pow(pos.X - foci[1].X, 2) + Math.Pow(pos.Y - foci[1].Y, 2)) < 10) 
            {
                _selectedShape = activeEllipse;
                _isRotatingEllipse = true;
                e.Pointer.Capture(MyCanvas);
                return;
            }
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
            
            if (_isResizingEllipse && _selectedShape is EllipseShape ellipseToResize) {
                // Передаем индекс в функцию Resize!
                ellipseToResize.Resize(pos, _resizeHandleIndex);
                UpdateCoordDisplay();
                MyCanvas.InvalidateVisual();
                return;
            }

            if (_isRotatingEllipse && _selectedShape is EllipseShape ellipseToRotate) {
                double angleRad = Math.Atan2(pos.Y - ellipseToRotate.Center.Y, pos.X - ellipseToRotate.Center.X);
                ellipseToRotate.Angle = angleRad * 180.0 / Math.PI;
                MyCanvas.InvalidateVisual();
                return;
            }
            
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
        if (_selectedShape != null && !_isDragging && !_isDraggingAnchor && !_isRotatingEllipse && !_isResizingEllipse && BtnDrawLine.IsChecked == false) {
            var pos = e.GetPosition(MyCanvas);
            var color = (CurrentColorDisplay.Fill as ISolidColorBrush)?.Color ?? Colors.Red;
            var thick = (double)(ThicknessControl.Value ?? 2);

            if (_selectedShape is PolygonShape poly) {
                int idx = poly.GetSideIndex(pos);
                if (idx != -1 && idx < poly.SideColors.Count) {
                    poly.SideColors[idx] = color;
                    poly.Thicknesses[idx] = thick;
                    MyCanvas.InvalidateVisual();
                }
            }
            else if (_selectedShape is EllipseShape circ) {
                if (circ.GetSideIndex(pos) != -1) {
                    circ.StrokeColor = color;
                    circ.StrokeThickness = thick;
                    MyCanvas.InvalidateVisual();
                }
            }
        }
        _selectedShape = null; 
        _isDragging = false; 
        _isDraggingAnchor = false;
        _isRotatingEllipse = false;
        _isResizingEllipse = false;
        _resizeHandleIndex = -1; // Сбрасываем индекс
        e.Pointer.Capture(null);
    }

    private void FinishDrawingPolygon() {
        if (MyCanvas.TempPolylinePts.Count > 2) {
            double cx = MyCanvas.TempPolylinePts.Average(p => p.X), cy = MyCanvas.TempPolylinePts.Average(p => p.Y);
            Point center = new Point(cx, cy);

            var poly = new PolygonShape { Center = center, Anchor = center, ShapeName = string.IsNullOrWhiteSpace(TxtShapeName.Text) ? "Кастомная" : TxtShapeName.Text };
            var color = (CurrentColorDisplay.Fill as ISolidColorBrush)?.Color ?? Colors.Red;
            var thick = (double)(ThicknessControl.Value ?? 2);

            foreach (var p in MyCanvas.TempPolylinePts) {
                poly.RelativePoints.Add(new Point(p.X - cx, p.Y - cy));
                poly.SideColors.Add(color); poly.Thicknesses.Add(thick);
            }
            FinalizeShapeSetup(poly);
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
        
        UpdateCoordDisplay(); 
        MyCanvas.InvalidateVisual();
    }

    private void OnUngroupClick(object? sender, RoutedEventArgs e) {
        var toRemove = new List<CompositeShape>();
        foreach (var shape in _multiSelectedShapes.OfType<CompositeShape>().ToList()) {
            toRemove.Add(shape);
            foreach (var sub in shape.SubShapes) { MyCanvas.Shapes.Add(sub); sub.IsSelected = true; _multiSelectedShapes.Add(sub); }
        }
        foreach(var c in toRemove) { MyCanvas.Shapes.Remove(c); _multiSelectedShapes.Remove(c); }

        if (_activeShape is CompositeShape activeGroup && toRemove.Contains(activeGroup)) {
            _activeShape = _multiSelectedShapes.LastOrDefault();
        }

        UpdateCoordDisplay(); 
        MyCanvas.InvalidateVisual();
    }

    private void OnExtractClick(object? sender, RoutedEventArgs e) {
        if (_activeShape != null && _currentParentGroup != null) {
            _currentParentGroup.SubShapes.Remove(_activeShape);
            MyCanvas.Shapes.Add(_activeShape);
            if (_currentParentGroup.SubShapes.Count == 0) MyCanvas.Shapes.Remove(_currentParentGroup);
            _currentParentGroup = null;
            UpdateCoordDisplay();
            MyCanvas.InvalidateVisual();
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs? e) {
        if (!_multiSelectedShapes.Any() && _activeShape == null) return;

        if (_currentParentGroup != null && _activeShape != null) {
            _currentParentGroup.SubShapes.Remove(_activeShape);
            if (_currentParentGroup.SubShapes.Count == 0) MyCanvas.Shapes.Remove(_currentParentGroup);
        } else {
            foreach (var shape in _multiSelectedShapes.ToList()) MyCanvas.Shapes.Remove(shape);
        }

        _multiSelectedShapes.Clear();
        _activeShape = null; _selectedShape = null; _currentParentGroup = null;
        
        UpdateCoordDisplay();
        MyCanvas.InvalidateVisual();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e) {
        if (TxtShapeName.IsFocused) return;
        if (e.Key == Key.Back || e.Key == Key.Delete) OnDeleteClick(null, null);
    }

// ==========================================
    // СОХРАНЕНИЕ И ЗАГРУЗКА ВСЕЙ СЦЕНЫ
    // ==========================================
    private async void OnSaveClick(object? sender, RoutedEventArgs e) {
        var topLevel = TopLevel.GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Сохранить всю сцену",
            SuggestedFileName = "scene.txt",
            DefaultExtension = "txt",
            FileTypeChoices = new[] { new FilePickerFileType("Текстовые файлы") { Patterns = new[] { "*.txt" } } }
        });
        if (file != null) {
            using var stream = await file.OpenWriteAsync();
            stream.SetLength(0); 
            using var writer = new StreamWriter(stream);
            writer.WriteLine($"Всего фигур: {MyCanvas.Shapes.Count}");
            writer.WriteLine("=========================================");
            foreach (var shape in MyCanvas.Shapes) {
                shape.SaveToStream(writer); 
                writer.WriteLine("-----------------------------------------"); 
            }
            TxtShapeName.Text = "Сцена сохранена!";
        }
    }

    private async void OnLoadClick(object? sender, RoutedEventArgs e) {
        var topLevel = TopLevel.GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Загрузить сцену (ВНИМАНИЕ: СТЕРЕТЬ ТЕКУЩУЮ)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Текстовые файлы") { Patterns = new[] { "*.txt" } } }
        });
        if (files.Count >= 1) {
            try {
                using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                
                string? firstLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(firstLine)) throw new Exception("Файл пуст!");
                int count = int.Parse(firstLine.Substring(firstLine.IndexOf(':') + 1).Trim());
                
                // Очищаем сцену перед загрузкой!
                MyCanvas.Shapes.Clear();
                ClearAllSelections(MyCanvas.Shapes);
                _multiSelectedShapes.Clear();
                _activeShape = null; _selectedShape = null; _currentParentGroup = null;

                for (int i = 0; i < count; i++) {
                    var shape = BaseShape.LoadShape(reader); 
                    if (shape != null) MyCanvas.Shapes.Add(shape);
                }
                UpdateCoordDisplay();
                MyCanvas.InvalidateVisual();
                TxtShapeName.Text = "Сцена загружена!";
            } 
            catch (Exception ex) {
                TxtShapeName.Text = "Ошибка загрузки!"; 
            }
        }
    }

    private async void OnExportShapeClick(object? sender, RoutedEventArgs e) {
        if (_activeShape == null) {
            TxtShapeName.Text = "Сначала выделите фигуру!";
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Экспортировать фигуру",
            SuggestedFileName = $"{_activeShape.ShapeName}.txt",
            DefaultExtension = "txt",
            FileTypeChoices = new[] { new FilePickerFileType("Текстовые файлы") { Patterns = new[] { "*.txt" } } }
        });
        if (file != null) {
            using var stream = await file.OpenWriteAsync();
            stream.SetLength(0); 
            using var writer = new StreamWriter(stream);
            writer.WriteLine($"Всего фигур: 1"); // Мы сохраняем только одну фигуру (даже если это группа)
            writer.WriteLine("=========================================");
            _activeShape.SaveToStream(writer); 
            writer.WriteLine("-----------------------------------------"); 
            TxtShapeName.Text = "Фигура экспортирована!";
        }
    }

    private async void OnImportShapeClick(object? sender, RoutedEventArgs e) {
        var topLevel = TopLevel.GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Импортировать фигуру на сцену",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Текстовые файлы") { Patterns = new[] { "*.txt" } } }
        });
        if (files.Count >= 1) {
            try {
                using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                
                string? firstLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(firstLine)) throw new Exception("Файл пуст!");
                int count = int.Parse(firstLine.Substring(firstLine.IndexOf(':') + 1).Trim());
                
                // ВНИМАНИЕ: Мы НЕ очищаем сцену MyCanvas.Shapes.Clear() !
                // Мы только снимаем выделение со старых фигур
                ClearAllSelections(MyCanvas.Shapes);
                _multiSelectedShapes.Clear();
                _activeShape = null; _selectedShape = null; _currentParentGroup = null;

                for (int i = 0; i < count; i++) {
                    var shape = BaseShape.LoadShape(reader); 
                    if (shape != null) {
                        MyCanvas.Shapes.Add(shape); // Добавляем в конец массива!
                        
                        // Сразу выделяем импортированную фигуру, чтобы ее было удобно подвинуть
                        shape.IsSelected = true;
                        _multiSelectedShapes.Add(shape);
                        _activeShape = shape; 
                    }
                }
                UpdateCoordDisplay();
                MyCanvas.InvalidateVisual();
                TxtShapeName.Text = "Фигура добавлена!";
            } 
            catch (Exception ex) {
                TxtShapeName.Text = "Ошибка импорта!"; 
            }
        }
    }

    private BaseShape CloneShape(BaseShape original) {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        original.SaveToStream(writer);
        writer.Flush();
        ms.Position = 0;
        using var reader = new StreamReader(ms);
        return BaseShape.LoadShape(reader)!;
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