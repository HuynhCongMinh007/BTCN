using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using AppPaint.ViewModels;
using AppPaint.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.Foundation;
using System.Collections.Generic;
using Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using UIShape = Microsoft.UI.Xaml.Shapes.Shape;

namespace AppPaint.Views;

public sealed partial class DrawingCanvasPage : Page
{
    public DrawingCanvasViewModel ViewModel { get; }

    private Point _startPoint;
    private UIShape? _previewShape;
    private List<Point> _polygonPoints = new();
    private List<Line> _polygonPreviewLines = new();
    private bool _isDrawing = false;
    private bool _isToolbarExpanded = true;
    private bool _isShiftPressed = false;

    // Selection & Edit mode
    private bool _isSelectMode = false;
    private UIShape? _selectedShape = null;
    private Border? _selectionBorder = null;
    private string? _selectedShapeOriginalStrokeColor = null;
    private double _selectedShapeOriginalThickness = 0;

    public DrawingCanvasPage()
    {
        ViewModel = App.Services.GetRequiredService<DrawingCanvasViewModel>();
        this.DataContext = ViewModel;

        // Subscribe to events
        ViewModel.NavigateBackRequested += OnNavigateBackRequested;
        ViewModel.ClearCanvasRequested += OnClearCanvasRequested;
        
        // Subscribe to ViewModel property changes
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        this.InitializeComponent();

        // Setup canvas events
        DrawingCanvas.PointerPressed += DrawingCanvas_PointerPressed;
        DrawingCanvas.PointerMoved += DrawingCanvas_PointerMoved;
        DrawingCanvas.PointerReleased += DrawingCanvas_PointerReleased;

        // Keyboard events
        this.KeyDown += DrawingCanvasPage_KeyDown;
        this.KeyUp += DrawingCanvasPage_KeyUp;
      
        // Thickness slider event
  StrokeThicknessSlider.ValueChanged += StrokeThicknessSlider_ValueChanged;
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
 System.Diagnostics.Debug.WriteLine($"📍 DrawingCanvasPage.OnNavigatedTo - Parameter: {e.Parameter?.GetType().Name ?? "null"}");
  
        base.OnNavigatedTo(e);
        ViewModel.OnNavigatedTo(e.Parameter);
        
      System.Diagnostics.Debug.WriteLine("✅ ViewModel.OnNavigatedTo called");
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        // Unsubscribe
        ViewModel.NavigateBackRequested -= OnNavigateBackRequested;
        ViewModel.ClearCanvasRequested -= OnClearCanvasRequested;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

        ViewModel.OnNavigatedFrom();
    }

    private void DrawingCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(DrawingCanvas).Position;

        // Ensure point is within canvas bounds
        if (point.X < 0 || point.X > DrawingCanvas.Width ||
point.Y < 0 || point.Y > DrawingCanvas.Height)
        {
            return;
        }

        // Selection mode - check if clicking on existing shape
        if (_isSelectMode)
        {
            SelectShapeAtPoint(point);
            return;
        }

        _startPoint = point;
        _isDrawing = true;

        if (ViewModel.SelectedShapeType == ShapeType.Polygon)
        {
            // Add point to polygon
            _polygonPoints.Add(point);

            // Draw a small circle to mark the point
            var marker = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
           DrawingService.ParseColor(ViewModel.SelectedColor))
            };
            Canvas.SetLeft(marker, point.X - 4);
            Canvas.SetTop(marker, point.Y - 4);
            DrawingCanvas.Children.Add(marker);

            // Draw line from previous point to current point
            if (_polygonPoints.Count > 1)
            {
                var prevPoint = _polygonPoints[_polygonPoints.Count - 2];
                var line = DrawingService.CreateLine(
                  prevPoint,
          point,
                ViewModel.SelectedColor,
         ViewModel.StrokeThickness,
           ViewModel.StrokeStyle
              );
                DrawingCanvas.Children.Add(line);
                _polygonPreviewLines.Add(line);
            }

            // Show Finish button
            FinishPolygonButton.Visibility = Visibility.Visible;
        }
    }

    private void SelectShapeAtPoint(Point point)
    {
        // Clear previous selection
        ClearSelection();

        // Find shape at point (reverse order - top shape first)
        for (int i = DrawingCanvas.Children.Count - 1; i >= 0; i--)
        {
            var element = DrawingCanvas.Children[i];

            if (element is UIShape shape && IsPointInShape(shape, point))
            {
                _selectedShape = shape;
                ShowSelectionBorder(shape);
                LoadShapeProperties(shape);
                ShowPropertyEditor();
                System.Diagnostics.Debug.WriteLine($"Selected shape: {shape.GetType().Name}");
                return;
            }
        }
    }

    private bool IsPointInShape(UIShape shape, Point point)
    {
        double left = Canvas.GetLeft(shape);
        double top = Canvas.GetTop(shape);

        if (shape is Line line)
        {
            // Line hit test with tolerance
            double tolerance = 5;
            return IsPointNearLine(point, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2), tolerance);
        }
        else if (shape is Rectangle rect)
        {
            return point.X >= left && point.X <= left + rect.Width &&
                  point.Y >= top && point.Y <= top + rect.Height;
        }
        else if (shape is Ellipse ellipse)
        {
            double centerX = left + ellipse.Width / 2;
            double centerY = top + ellipse.Height / 2;
            double dx = (point.X - centerX) / (ellipse.Width / 2);
            double dy = (point.Y - centerY) / (ellipse.Height / 2);
            return (dx * dx + dy * dy) <= 1;
        }
        else if (shape is Polygon polygon)
        {
            // Point in polygon test
            return IsPointInPolygon(point, polygon.Points);
        }

        return false;
    }

    private bool IsPointNearLine(Point point, Point lineStart, Point lineEnd, double tolerance)
    {
        double distance = DistanceFromPointToLine(point, lineStart, lineEnd);
        return distance <= tolerance;
    }

    private double DistanceFromPointToLine(Point point, Point lineStart, Point lineEnd)
    {
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;
        double lengthSquared = dx * dx + dy * dy;

        if (lengthSquared == 0) return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));

        double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;
        t = Math.Max(0, Math.Min(1, t));

        double nearestX = lineStart.X + t * dx;
        double nearestY = lineStart.Y + t * dy;

        return Math.Sqrt(Math.Pow(point.X - nearestX, 2) + Math.Pow(point.Y - nearestY, 2));
    }

    private bool IsPointInPolygon(Point point, Microsoft.UI.Xaml.Media.PointCollection points)
    {
        bool inside = false;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            if ((points[i].Y > point.Y) != (points[j].Y > point.Y) &&
                      point.X < (points[j].X - points[i].X) * (point.Y - points[i].Y) / (points[j].Y - points[i].Y) + points[i].X)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private void ShowSelectionBorder(UIShape shape)
    {
        double left = Canvas.GetLeft(shape);
        double top = Canvas.GetTop(shape);
        double width = 0;
        double height = 0;

        if (shape is Line line)
        {
            left = Math.Min(line.X1, line.X2) - 5;
            top = Math.Min(line.Y1, line.Y2) - 5;
            width = Math.Abs(line.X2 - line.X1) + 10;
            height = Math.Abs(line.Y2 - line.Y1) + 10;
        }
        else if (shape is Rectangle rect)
        {
            width = rect.Width;
            height = rect.Height;
        }
        else if (shape is Ellipse ellipse)
        {
            width = ellipse.Width;
            height = ellipse.Height;
        }
        else if (shape is Polygon polygon)
        {
            var bounds = GetPolygonBounds(polygon.Points);
            left = bounds.Left - 5;
            top = bounds.Top - 5;
            width = bounds.Width + 10;
            height = bounds.Height + 10;
        }

        _selectionBorder = new Border
        {
            Width = width,
            Height = height,
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(2),
            IsHitTestVisible = false
        };

        Canvas.SetLeft(_selectionBorder, left);
        Canvas.SetTop(_selectionBorder, top);
        DrawingCanvas.Children.Add(_selectionBorder);
    }

    private Rect GetPolygonBounds(Microsoft.UI.Xaml.Media.PointCollection points)
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var point in points)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private void ClearSelection()
    {
        if (_selectionBorder != null)
        {
            DrawingCanvas.Children.Remove(_selectionBorder);
            _selectionBorder = null;
        }
        _selectedShape = null;
        HidePropertyEditor();
    }

    private void LoadShapeProperties(UIShape shape)
    {
      // Get current stroke color
     if (shape.Stroke is SolidColorBrush strokeBrush)
      {
            var color = strokeBrush.Color;
 _selectedShapeOriginalStrokeColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
     System.Diagnostics.Debug.WriteLine($"Shape Stroke Color: {_selectedShapeOriginalStrokeColor}");
        }

        // Get current thickness
        _selectedShapeOriginalThickness = shape.StrokeThickness;
        System.Diagnostics.Debug.WriteLine($"Shape Thickness: {_selectedShapeOriginalThickness}");
    
      // Get current fill status and color
        bool isFilled = shape.Fill != null;
        if (shape.Fill is SolidColorBrush fillBrush)
      {
            var fillColor = fillBrush.Color;
          ViewModel.FillColor = $"#{fillColor.R:X2}{fillColor.G:X2}{fillColor.B:X2}";
       
   // Update fill color preview button
  if (FillColorPreview != null)
    {
   FillColorPreview.Color = fillColor;
  }
 
     System.Diagnostics.Debug.WriteLine($"Shape Fill Color: {ViewModel.FillColor}");
    }
     else
   {
     // Keep current ViewModel fill color if shape has no fill
     System.Diagnostics.Debug.WriteLine($"Shape not filled, keeping ViewModel.FillColor: {ViewModel.FillColor}");
        }
  
 // Load properties to ViewModel
    ViewModel.SelectedColor = _selectedShapeOriginalStrokeColor ?? "#000000";
  ViewModel.StrokeThickness = _selectedShapeOriginalThickness;
        ViewModel.IsFilled = isFilled;
        
    System.Diagnostics.Debug.WriteLine($"Shape IsFilled: {isFilled}");
}

    private void ShowPropertyEditor()
    {
   // For now, properties shown in toolbar
        System.Diagnostics.Debug.WriteLine("Shape selected - properties loaded into toolbar");
 }

    private void HidePropertyEditor()
    {
        // Reset toolbar to default
    }

    private void EditStrokeColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Will implement with UI later
    }

    private void EditStrokeThicknessSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        // Will implement with UI later
    }

    private void ApplyShapeEdit_Click(object sender, RoutedEventArgs e)
  {
        // Will implement with UI later
    }

    private void DeleteShapeButton_Click(object sender, RoutedEventArgs e)
    {
        // Will implement with UI later
    }

    private void DrawingCanvasPage_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Shift)
        {
            _isShiftPressed = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Delete && _selectedShape != null)
        {
            // Delete selected shape
            DeleteSelectedShape();
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            // Cancel selection
            ClearSelection();
        }
    }

    private void DeleteSelectedShape()
    {
        if (_selectedShape != null)
        {
            DrawingCanvas.Children.Remove(_selectedShape);
            ClearSelection();
            System.Diagnostics.Debug.WriteLine("Shape deleted");
        }
    }

    private void ShapeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button && button.Tag is string tag)
        {
            // Uncheck all other buttons
            SelectButton.IsChecked = false;
            LineButton.IsChecked = false;
            RectangleButton.IsChecked = false;
            OvalButton.IsChecked = false;
            CircleButton.IsChecked = false;
            TriangleButton.IsChecked = false;
            PolygonButton.IsChecked = false;

            // Check clicked button
            button.IsChecked = true;

            // Disable select mode
            _isSelectMode = false;

            // Set shape type in ViewModel
            ViewModel.SelectedShapeType = tag switch
            {
                "Line" => ShapeType.Line,
                "Rectangle" => ShapeType.Rectangle,
                "Oval" => ShapeType.Oval,
                "Circle" => ShapeType.Circle,
                "Triangle" => ShapeType.Triangle,
                "Polygon" => ShapeType.Polygon,
                _ => ShapeType.Line
            };
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            // Uncheck all shape buttons
            LineButton.IsChecked = false;
            RectangleButton.IsChecked = false;
            OvalButton.IsChecked = false;
            CircleButton.IsChecked = false;
            TriangleButton.IsChecked = false;
            PolygonButton.IsChecked = false;

            // Check select button
            button.IsChecked = true;

            // Enable select mode
            _isSelectMode = true;
            System.Diagnostics.Debug.WriteLine("Select mode enabled");
        }
    }

    private void StrokeColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        var color = args.NewColor;
        ViewModel.SelectedColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        
        // Update preview button
  if (StrokeColorPreview != null)
  {
  StrokeColorPreview.Color = color;
        }
        
  // If shape is selected, update its color
 if (_selectedShape != null && _isSelectMode)
        {
_selectedShape.Stroke = new SolidColorBrush(color);
      System.Diagnostics.Debug.WriteLine($"Updated selected shape stroke color: {ViewModel.SelectedColor}");
        }

 System.Diagnostics.Debug.WriteLine($"Stroke color changed: {ViewModel.SelectedColor}");
    }

    private void FillColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
   var color = args.NewColor;
        ViewModel.FillColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        
        // Update preview button
    if (FillColorPreview != null)
        {
     FillColorPreview.Color = color;
        }
        
        // If shape is selected and filled, update its fill color
        if (_selectedShape != null && _isSelectMode && ViewModel.IsFilled)
        {
            _selectedShape.Fill = new SolidColorBrush(color);
       System.Diagnostics.Debug.WriteLine($"Updated selected shape fill color: {ViewModel.FillColor}");
        }
        
        System.Diagnostics.Debug.WriteLine($"Fill color changed: {ViewModel.FillColor}");
    }

    private void DrawingCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing) return;
        if (ViewModel.SelectedShapeType == ShapeType.Polygon) return;

        var currentPoint = e.GetCurrentPoint(DrawingCanvas).Position;

        // Clamp to canvas bounds
        currentPoint.X = Math.Max(0, Math.Min(DrawingCanvas.Width, currentPoint.X));
        currentPoint.Y = Math.Max(0, Math.Min(DrawingCanvas.Height, currentPoint.Y));

        // Remove previous preview
        if (_previewShape != null)
        {
            DrawingCanvas.Children.Remove(_previewShape);
        }

        // Create preview shape
        _previewShape = CreateShape(_startPoint, currentPoint, ViewModel.SelectedShapeType, true);
        if (_previewShape != null)
        {
            DrawingCanvas.Children.Add(_previewShape);
        }
    }

    private void DrawingCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing) return;
        if (ViewModel.SelectedShapeType == ShapeType.Polygon) return;

        var endPoint = e.GetCurrentPoint(DrawingCanvas).Position;

        // Clamp to canvas bounds
        endPoint.X = Math.Max(0, Math.Min(DrawingCanvas.Width, endPoint.X));
        endPoint.Y = Math.Max(0, Math.Min(DrawingCanvas.Height, endPoint.Y));

        _isDrawing = false;

        // Remove preview
        if (_previewShape != null)
        {
            DrawingCanvas.Children.Remove(_previewShape);
            _previewShape = null;
        }

        // Create final shape
        var finalShape = CreateShape(_startPoint, endPoint, ViewModel.SelectedShapeType, false);
        if (finalShape != null)
        {
            DrawingCanvas.Children.Add(finalShape);

            // Save to ViewModel/Database
            SaveShapeToDatabase(_startPoint, endPoint, ViewModel.SelectedShapeType);
        }
    }

    private UIShape? CreateShape(Point start, Point end, ShapeType shapeType, bool isPreview)
    {
        var strokeColor = ViewModel.SelectedColor;
        var thickness = ViewModel.StrokeThickness;
        var isFilled = ViewModel.IsFilled;
        var fillColor = ViewModel.FillColor;
        var strokeStyle = ViewModel.StrokeStyle;

        if (isPreview)
        {
            thickness = Math.Max(1, thickness - 1);
        }

        return shapeType switch
        {
            ShapeType.Line => DrawingService.CreateLine(start, end, strokeColor, thickness, strokeStyle, _isShiftPressed),
            ShapeType.Rectangle => DrawingService.CreateRectangle(start, end, strokeColor, thickness, isFilled, strokeStyle, fillColor, _isShiftPressed),
            ShapeType.Circle => DrawingService.CreateEllipse(start, end, strokeColor, thickness, true, isFilled, strokeStyle, fillColor),
            ShapeType.Oval => DrawingService.CreateEllipse(start, end, strokeColor, thickness, false, isFilled, strokeStyle, fillColor),
            ShapeType.Triangle => DrawingService.CreateTriangle(start, end, strokeColor, thickness, isFilled, strokeStyle, fillColor),
            ShapeType.Polygon => null, // Handled in polygon special case
            _ => null
        };
    }

    private async void SaveShapeToDatabase(Point start, Point end, ShapeType shapeType)
    {
        var points = new List<Point> { start, end };
        var pointsJson = DrawingService.PointsToJson(points);

        var shape = new Data.Models.Shape
        {
            ShapeType = shapeType,
            PointsData = pointsJson,
            Color = ViewModel.SelectedColor,
            StrokeThickness = ViewModel.StrokeThickness,
            IsFilled = ViewModel.IsFilled,
            FillColor = ViewModel.IsFilled ? ViewModel.FillColor : null,
            TemplateId = ViewModel.CurrentTemplateId,
            CreatedAt = DateTime.Now
        };

        await ViewModel.AddShapeCommand.ExecuteAsync(shape);
    }

    private void FinishPolygonButton_Click(object sender, RoutedEventArgs e)
    {
        if (_polygonPoints.Count < 3)
        {
            System.Diagnostics.Debug.WriteLine("Need at least 3 points to create polygon");
            return;
        }

        // Remove preview markers and lines
        foreach (var line in _polygonPreviewLines)
        {
            DrawingCanvas.Children.Remove(line);
        }
        _polygonPreviewLines.Clear();

        // Remove markers (small circles)
        var markersToRemove = DrawingCanvas.Children
                  .OfType<Ellipse>()
            .Where(e => e.Width == 8 && e.Height == 8)
         .ToList();
        foreach (var marker in markersToRemove)
        {
            DrawingCanvas.Children.Remove(marker);
        }

        // Create final polygon
        var polygon = DrawingService.CreatePolygon(
      _polygonPoints,
       ViewModel.SelectedColor,
       ViewModel.StrokeThickness,
   ViewModel.IsFilled,
    ViewModel.StrokeStyle,
         ViewModel.FillColor
     );

        DrawingCanvas.Children.Add(polygon);

        // Save to database
        var pointsJson = DrawingService.PointsToJson(_polygonPoints);
        var shape = new Data.Models.Shape
        {
            ShapeType = ShapeType.Polygon,
            PointsData = pointsJson,
            Color = ViewModel.SelectedColor,
            StrokeThickness = ViewModel.StrokeThickness,
            IsFilled = ViewModel.IsFilled,
            FillColor = ViewModel.IsFilled ? ViewModel.FillColor : null,
            TemplateId = ViewModel.CurrentTemplateId,
            CreatedAt = DateTime.Now
        };

        ViewModel.AddShapeCommand.ExecuteAsync(shape);

        // Reset
        _polygonPoints.Clear();
        FinishPolygonButton.Visibility = Visibility.Collapsed;
    }

    private void OnClearCanvasRequested(object? sender, EventArgs e)
    {
        DrawingCanvas.Children.Clear();
        _polygonPoints.Clear();
        _polygonPreviewLines.Clear();
        FinishPolygonButton.Visibility = Visibility.Collapsed;
        ClearSelection();
    }

    private void OnNavigateBackRequested(object? sender, EventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void StrokeStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
  {
      var style = item.Tag?.ToString() ?? "Solid";
ViewModel.StrokeStyle = style;
          
 // If shape is selected, update its style
          if (_selectedShape != null && _isSelectMode)
      {
   _selectedShape.StrokeDashArray = DrawingService.GetStrokeDashArray(style);
    System.Diagnostics.Debug.WriteLine($"Updated selected shape stroke style: {style}");
     }
          
System.Diagnostics.Debug.WriteLine($"Stroke style changed: {style}");
    }
    }

    private void CanvasSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            var size = item.Tag?.ToString() ?? "800x600";
            var parts = size.Split('x');

            if (parts.Length == 2 &&
              double.TryParse(parts[0], out double width) &&
                double.TryParse(parts[1], out double height))
            {
                ViewModel.CanvasWidth = width;
                ViewModel.CanvasHeight = height;
                ViewModel.SelectedCanvasSize = size;

                System.Diagnostics.Debug.WriteLine($"Canvas size changed: {width}x{height}");
            }
        }
    }

    private void ToggleToolbarButton_Click(object sender, RoutedEventArgs e)
    {
        _isToolbarExpanded = !_isToolbarExpanded;

        // Toggle toolbar content visibility
        ToolbarContent.Visibility = _isToolbarExpanded ? Visibility.Visible : Visibility.Collapsed;

        // Update button icon
        var button = sender as Button;
        if (button?.Content is FontIcon icon)
        {
            // E700 = GlobalNavigationButton (hamburger)
            // E76C = ChevronUp
            icon.Glyph = _isToolbarExpanded ? "\uE76C" : "\uE700";
        }

        System.Diagnostics.Debug.WriteLine($"Toolbar {(_isToolbarExpanded ? "expanded" : "collapsed")}");
    }

    private void BackgroundColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        var color = args.NewColor;
        ViewModel.BackgroundColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        // Update canvas background brush
        CanvasBackgroundBrush.Color = color;

        // Update button preview color
        BackgroundColorPreview.Color = color;

        System.Diagnostics.Debug.WriteLine($"Background color changed: {ViewModel.BackgroundColor}");
    }

 private void StrokeThicknessSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
 {
        if (_selectedShape != null && _isSelectMode)
   {
   _selectedShape.StrokeThickness = e.NewValue;
 System.Diagnostics.Debug.WriteLine($"Updated selected shape thickness: {e.NewValue}");
  }
    }

    private void DrawingCanvasPage_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Shift)
        {
            _isShiftPressed = false;
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
 // Show dialog to input template name
        var dialog = new ContentDialog
        {
  XamlRoot = this.XamlRoot,
     Title = "Save Drawing",
    PrimaryButtonText = "Save",
     CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary
  };

 // Create input textbox
 var stackPanel = new StackPanel { Spacing = 12 };
        
        stackPanel.Children.Add(new TextBlock 
{ 
   Text = "Enter a name for your drawing:", 
    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold 
  });

      var nameTextBox = new TextBox
{
   PlaceholderText = "My Drawing",
 Text = ViewModel.TemplateName,
  MaxLength = 200
  };
        
 // Auto-generate name if "New Drawing"
   if (ViewModel.TemplateName == "New Drawing")
  {
       nameTextBox.Text = $"Drawing {DateTime.Now:yyyy-MM-dd HH-mm}";
}

        stackPanel.Children.Add(nameTextBox);
      
  // Count shapes on canvas
      int canvasShapeCount = DrawingCanvas.Children.OfType<UIShape>().Count();
    
        // Additional info
    stackPanel.Children.Add(new TextBlock
{
   Text = $"Canvas Size: {ViewModel.CanvasWidth}x{ViewModel.CanvasHeight}",
     FontSize = 12,
      Opacity = 0.7
  });
  
  stackPanel.Children.Add(new TextBlock
 {
   Text = $"Shapes on canvas: {canvasShapeCount}",
    FontSize = 12,
  Opacity = 0.7
        });

   dialog.Content = stackPanel;

  // Show dialog
   var result = await dialog.ShowAsync();

   if (result == ContentDialogResult.Primary)
   {
    var name = nameTextBox.Text.Trim();
            
  if (string.IsNullOrEmpty(name))
       {
       // Show error
        var errorDialog = new ContentDialog
      {
      XamlRoot = this.XamlRoot,
   Title = "Error",
    Content = "Please enter a name for your drawing.",
  CloseButtonText = "OK"
    };
    await errorDialog.ShowAsync();
     return;
      }

    // Update template name
   ViewModel.TemplateName = name;
            
 // Save template and all shapes from canvas
    await SaveTemplateWithCanvasShapes();

  // Show success message
   var successDialog = new ContentDialog
            {
    XamlRoot = this.XamlRoot,
    Title = "Success",
          Content = $"Drawing '{name}' saved with {canvasShapeCount} shapes!",
       CloseButtonText = "OK"
    };
            await successDialog.ShowAsync();
        }
 }

    private async Task SaveTemplateWithCanvasShapes()
    {
        try
        {
    ViewModel.IsBusy = true;

       using var scope = App.Services.CreateScope();
          var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
            var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();

            // Create or update template
      DrawingTemplate template;
        if (ViewModel.CurrentTemplateId.HasValue)
 {
       // Update existing template
  var existingTemplate = await templateService.GetTemplateByIdAsync(ViewModel.CurrentTemplateId.Value);
                if (existingTemplate != null)
      {
                 existingTemplate.Name = ViewModel.TemplateName;
             existingTemplate.Width = ViewModel.CanvasWidth;
            existingTemplate.Height = ViewModel.CanvasHeight;
        existingTemplate.BackgroundColor = ViewModel.BackgroundColor;
          template = await templateService.UpdateTemplateAsync(existingTemplate);
   }
          else
       {
template = await CreateNewTemplate(templateService);
       }
     }
            else
      {
           template = await CreateNewTemplate(templateService);
        }

ViewModel.CurrentTemplateId = template.Id;

 // Clear old shapes if updating
            if (template.Shapes.Any())
       {
     foreach (var oldShape in template.Shapes.ToList())
   {
     await shapeService.DeleteShapeAsync(oldShape.Id);
          }
   }

        // Save all shapes from canvas to database
        var canvasShapes = DrawingCanvas.Children.OfType<UIShape>().ToList();
     foreach (var uiShape in canvasShapes)
     {
                var shape = ConvertUIShapeToDataModel(uiShape, template.Id);
      if (shape != null)
      {
        await shapeService.CreateShapeAsync(shape);
                }
      }

  System.Diagnostics.Debug.WriteLine($"Saved template '{template.Name}' with {canvasShapes.Count} shapes");
   }
   catch (Exception ex)
        {
         ViewModel.ErrorMessage = $"Error saving: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Save error: {ex}");
     }
        finally
        {
 ViewModel.IsBusy = false;
        }
    }

    private async Task<DrawingTemplate> CreateNewTemplate(ITemplateService templateService)
    {
        var template = new DrawingTemplate
        {
        Name = ViewModel.TemplateName,
  Width = ViewModel.CanvasWidth,
            Height = ViewModel.CanvasHeight,
            BackgroundColor = ViewModel.BackgroundColor
        };
        return await templateService.CreateTemplateAsync(template);
    }

    private Data.Models.Shape? ConvertUIShapeToDataModel(UIShape uiShape, int templateId)
    {
        var shape = new Data.Models.Shape
        {
      TemplateId = templateId,
            CreatedAt = DateTime.Now
        };

     // Get stroke properties
        if (uiShape.Stroke is SolidColorBrush strokeBrush)
        {
            var color = strokeBrush.Color;
     shape.Color = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
  }

        shape.StrokeThickness = uiShape.StrokeThickness;

        // Get stroke style from StrokeDashArray
        shape.StrokeStyle = GetStrokeStyleFromDashArray(uiShape.StrokeDashArray);

        // Get fill properties
        shape.IsFilled = uiShape.Fill != null;
     if (uiShape.Fill is SolidColorBrush fillBrush)
        {
       var fillColor = fillBrush.Color;
            shape.FillColor = $"#{fillColor.R:X2}{fillColor.G:X2}{fillColor.B:X2}";
        }

        // Convert based on shape type
        if (uiShape is Line line)
        {
       shape.ShapeType = ShapeType.Line;
     var points = new List<Point> 
      { 
      new Point(line.X1, line.Y1), 
         new Point(line.X2, line.Y2) 
            };
         shape.PointsData = DrawingService.PointsToJson(points);
        }
   else if (uiShape is Microsoft.UI.Xaml.Shapes.Rectangle rect)
        {
        shape.ShapeType = ShapeType.Rectangle;
            double left = Canvas.GetLeft(rect);
   double top = Canvas.GetTop(rect);
       var points = new List<Point>
    {
          new Point(left, top),
           new Point(left + rect.Width, top + rect.Height)
         };
    shape.PointsData = DrawingService.PointsToJson(points);
  }
        else if (uiShape is Ellipse ellipse)
 {
double left = Canvas.GetLeft(ellipse);
       double top = Canvas.GetTop(ellipse);
          
            // Detect if it's a circle (width == height)
            bool isCircle = Math.Abs(ellipse.Width - ellipse.Height) < 0.1;
            shape.ShapeType = isCircle ? ShapeType.Circle : ShapeType.Oval;
            
            var points = new List<Point>
  {
 new Point(left, top),
     new Point(left + ellipse.Width, top + ellipse.Height)
 };
    shape.PointsData = DrawingService.PointsToJson(points);
        }
        else if (uiShape is Microsoft.UI.Xaml.Shapes.Polygon polygon)
        {
          // Check if it's a triangle (3 points) or polygon
       shape.ShapeType = polygon.Points.Count == 3 ? ShapeType.Triangle : ShapeType.Polygon;

 var points = new List<Point>();
        foreach (var point in polygon.Points)
       {
     points.Add(point);
 }
            shape.PointsData = DrawingService.PointsToJson(points);
        }
        else
        {
 return null; // Unknown shape type
      }

        return shape;
    }

    private string GetStrokeStyleFromDashArray(DoubleCollection? dashArray)
    {
        if (dashArray == null || dashArray.Count == 0)
 return "Solid";

        // Match against known patterns
        if (dashArray.Count == 2 && Math.Abs(dashArray[0] - 4) < 0.1 && Math.Abs(dashArray[1] - 2) < 0.1)
            return "Dash";

if (dashArray.Count == 2 && Math.Abs(dashArray[0] - 1) < 0.1 && Math.Abs(dashArray[1] - 2) < 0.1)
  return "Dot";

        if (dashArray.Count == 4)
    return "DashDot";

        return "Solid"; // Default
    }

private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(ViewModel.IsFilled))
    {
        OnIsFilledChanged();
    }
}

    private void OnIsFilledChanged()
 {
        System.Diagnostics.Debug.WriteLine($"IsFilled changed to: {ViewModel.IsFilled}");
 
        if (_selectedShape != null && _isSelectMode)
   {
    if (ViewModel.IsFilled)
     {
        // Apply fill color from ViewModel
     var fillColor = DrawingService.ParseColor(ViewModel.FillColor);
_selectedShape.Fill = new SolidColorBrush(fillColor);
       System.Diagnostics.Debug.WriteLine($"Shape filled with color: {ViewModel.FillColor}");
      }
       else
 {
  // Remove fill
     _selectedShape.Fill = null;
   System.Diagnostics.Debug.WriteLine("Shape fill removed");
       }
  }
    }
}
