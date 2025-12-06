using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;
using AppPaint.ViewModels;
using AppPaint.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.Foundation;
using System.Collections.Generic;
using Data.Models;
using System;
using System.Linq;
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
    private bool _isShiftPressed = false; // Track Shift key for snap-to-angle

    public DrawingCanvasPage()
    {
        ViewModel = App.Services.GetRequiredService<DrawingCanvasViewModel>();
        this.DataContext = ViewModel;
     
        // Subscribe to events
        ViewModel.NavigateBackRequested += OnNavigateBackRequested;
     ViewModel.ClearCanvasRequested += OnClearCanvasRequested;
        
     this.InitializeComponent();

     // Setup canvas events
        DrawingCanvas.PointerPressed += DrawingCanvas_PointerPressed;
        DrawingCanvas.PointerMoved += DrawingCanvas_PointerMoved;
    DrawingCanvas.PointerReleased += DrawingCanvas_PointerReleased;

        // Keyboard events for Shift key
  this.KeyDown += DrawingCanvasPage_KeyDown;
   this.KeyUp += DrawingCanvasPage_KeyUp;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.OnNavigatedTo(e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
   base.OnNavigatedFrom(e);
     
        // Unsubscribe
        ViewModel.NavigateBackRequested -= OnNavigateBackRequested;
        ViewModel.ClearCanvasRequested -= OnClearCanvasRequested;
   
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
   var fillColor = ViewModel.SelectedColor;
    var strokeStyle = ViewModel.StrokeStyle;

   if (isPreview)
      {
        thickness = Math.Max(1, thickness - 1);
     }

      return shapeType switch
     {
            // Pass Shift key state for snap-to-angle on Line
     ShapeType.Line => DrawingService.CreateLine(start, end, strokeColor, thickness, strokeStyle, _isShiftPressed),
         // Pass Shift key state for snap-to-square on Rectangle
       ShapeType.Rectangle => DrawingService.CreateRectangle(start, end, strokeColor, thickness, isFilled, strokeStyle, fillColor, _isShiftPressed),
     // Pass Shift key state for perfect circle
  ShapeType.Circle => DrawingService.CreateEllipse(start, end, strokeColor, thickness, true, isFilled, strokeStyle, fillColor),
  ShapeType.Oval => DrawingService.CreateEllipse(start, end, strokeColor, thickness, false, isFilled, strokeStyle, fillColor),
     ShapeType.Triangle => DrawingService.CreateTriangle(start, end, strokeColor, thickness, isFilled, strokeStyle, fillColor),
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
       FillColor = ViewModel.IsFilled ? ViewModel.SelectedColor : null,
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
  ViewModel.SelectedColor
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
            FillColor = ViewModel.IsFilled ? ViewModel.SelectedColor : null,
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
    }

    private void OnNavigateBackRequested(object? sender, EventArgs e)
    {
        if (Frame.CanGoBack)
   {
Frame.GoBack();
        }
    }

    private void ShapeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button && button.Tag is string tag)
  {
            // Uncheck all other buttons
            LineButton.IsChecked = false;
RectangleButton.IsChecked = false;
  OvalButton.IsChecked = false;
       CircleButton.IsChecked = false;
      TriangleButton.IsChecked = false;
  PolygonButton.IsChecked = false;

            // Check clicked button
            button.IsChecked = true;

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

    private void StrokeColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        var color = args.NewColor;
        ViewModel.SelectedColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void StrokeStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
  {
        var style = item.Tag?.ToString() ?? "Solid";
          ViewModel.StrokeStyle = style;
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

    private void DrawingCanvasPage_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
 if (e.Key == Windows.System.VirtualKey.Shift)
  {
   _isShiftPressed = true;
        }
    }

    private void DrawingCanvasPage_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Shift)
 {
   _isShiftPressed = false;
        }
    }
}
