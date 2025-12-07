using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using AppPaint.Services;
using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using UIShape = Microsoft.UI.Xaml.Shapes.Shape;

namespace AppPaint.Handlers;

/// <summary>
/// Handles creation of new shapes on the canvas
/// </summary>
public class ShapeCreationHandler
{
    private readonly DrawingService _drawingService;
    
    private Point _startPoint;
    private UIShape? _previewShape;
  private List<Point> _polygonPoints = new();
    private List<Line> _polygonPreviewLines = new();
    private bool _isDrawing = false;
    private bool _isShiftPressed = false;

    public bool IsDrawing => _isDrawing;
    public bool IsShiftPressed => _isShiftPressed;
    public List<Point> PolygonPoints => _polygonPoints;

    public event EventHandler<ShapeCreatedEventArgs>? ShapeCreated;
    public event EventHandler? PolygonFinished;

    public ShapeCreationHandler(DrawingService drawingService)
    {
    _drawingService = drawingService;
    }

    public void SetShiftPressed(bool pressed)
 {
        _isShiftPressed = pressed;
    }

    public void StartDrawing(Point point, ShapeType shapeType, Microsoft.UI.Xaml.Controls.Canvas canvas)
    {
        // Ensure point is within canvas bounds
        if (point.X < 0 || point.X > canvas.Width || point.Y < 0 || point.Y > canvas.Height)
        {
      return;
  }

  _startPoint = point;
     _isDrawing = true;

  if (shapeType == ShapeType.Polygon)
        {
    HandlePolygonPoint(point, canvas);
  }
    }

    private void HandlePolygonPoint(Point point, Microsoft.UI.Xaml.Controls.Canvas canvas)
    {
        _polygonPoints.Add(point);

   // Draw a small circle to mark the point
   var marker = new Ellipse
        {
            Width = 8,
            Height = 8,
  Fill = new SolidColorBrush(Microsoft.UI.Colors.Blue)
        };
        Microsoft.UI.Xaml.Controls.Canvas.SetLeft(marker, point.X - 4);
        Microsoft.UI.Xaml.Controls.Canvas.SetTop(marker, point.Y - 4);
        canvas.Children.Add(marker);

        // Draw line from previous point to current point
      if (_polygonPoints.Count > 1)
      {
     var prevPoint = _polygonPoints[_polygonPoints.Count - 2];
   var line = new Line
  {
     X1 = prevPoint.X,
                Y1 = prevPoint.Y,
         X2 = point.X,
    Y2 = point.Y,
     Stroke = new SolidColorBrush(Microsoft.UI.Colors.Blue),
     StrokeThickness = 2
    };
   canvas.Children.Add(line);
          _polygonPreviewLines.Add(line);
        }
    }

    public UIShape? UpdatePreview(Point currentPoint, ShapeType shapeType, string color, 
        double thickness, bool isFilled, string fillColor, string strokeStyle, 
        Microsoft.UI.Xaml.Controls.Canvas canvas)
    {
        if (!_isDrawing || shapeType == ShapeType.Polygon)
        {
     return null;
        }

        // Clamp to canvas bounds
        currentPoint.X = Math.Max(0, Math.Min(canvas.Width, currentPoint.X));
   currentPoint.Y = Math.Max(0, Math.Min(canvas.Height, currentPoint.Y));

    // Remove previous preview
        if (_previewShape != null)
        {
   canvas.Children.Remove(_previewShape);
        }

        // Create preview shape with reduced thickness
        var previewThickness = Math.Max(1, thickness - 1);
     _previewShape = CreateShape(_startPoint, currentPoint, shapeType, color, 
 previewThickness, isFilled, strokeStyle, fillColor);

        if (_previewShape != null)
        {
     canvas.Children.Add(_previewShape);
   }

     return _previewShape;
    }

    public (Point start, Point end)? FinishDrawing(Point endPoint, ShapeType shapeType, 
        Microsoft.UI.Xaml.Controls.Canvas canvas)
    {
      if (!_isDrawing || shapeType == ShapeType.Polygon)
  {
       return null;
        }

        // Clamp to canvas bounds
        endPoint.X = Math.Max(0, Math.Min(canvas.Width, endPoint.X));
        endPoint.Y = Math.Max(0, Math.Min(canvas.Height, endPoint.Y));

        _isDrawing = false;

     // Remove preview
  if (_previewShape != null)
    {
         canvas.Children.Remove(_previewShape);
     _previewShape = null;
        }

      return (_startPoint, endPoint);
    }

    public List<Point>? FinishPolygon(Microsoft.UI.Xaml.Controls.Canvas canvas)
    {
        if (_polygonPoints.Count < 3)
        {
System.Diagnostics.Debug.WriteLine("Need at least 3 points to create polygon");
    return null;
        }

        // Remove preview markers and lines
        foreach (var line in _polygonPreviewLines)
   {
 canvas.Children.Remove(line);
        }
        _polygonPreviewLines.Clear();

        // Remove markers (small circles)
        var markersToRemove = canvas.Children
  .OfType<Ellipse>()
 .Where(e => e.Width == 8 && e.Height == 8)
      .ToList();
        foreach (var marker in markersToRemove)
{
            canvas.Children.Remove(marker);
        }

        var points = new List<Point>(_polygonPoints);
        _polygonPoints.Clear();

   PolygonFinished?.Invoke(this, EventArgs.Empty);

        return points;
    }

  public void ClearPolygon(Microsoft.UI.Xaml.Controls.Canvas canvas)
    {
        foreach (var line in _polygonPreviewLines)
        {
     canvas.Children.Remove(line);
     }
        _polygonPreviewLines.Clear();

        var markersToRemove = canvas.Children
     .OfType<Ellipse>()
       .Where(e => e.Width == 8 && e.Height == 8)
            .ToList();
    foreach (var marker in markersToRemove)
      {
canvas.Children.Remove(marker);
}

     _polygonPoints.Clear();
    }

    private UIShape? CreateShape(Point start, Point end, ShapeType shapeType, string color,
        double thickness, bool isFilled, string strokeStyle, string fillColor)
    {
        return shapeType switch
        {
 ShapeType.Line => DrawingService.CreateLine(start, end, color, thickness, strokeStyle, _isShiftPressed),
  ShapeType.Rectangle => DrawingService.CreateRectangle(start, end, color, thickness, isFilled, strokeStyle, fillColor, _isShiftPressed),
         ShapeType.Circle => DrawingService.CreateEllipse(start, end, color, thickness, true, isFilled, strokeStyle, fillColor),
      ShapeType.Oval => DrawingService.CreateEllipse(start, end, color, thickness, false, isFilled, strokeStyle, fillColor),
    ShapeType.Triangle => DrawingService.CreateTriangle(start, end, color, thickness, isFilled, strokeStyle, fillColor),
            _ => null
        };
    }

    public void Reset()
    {
        _isDrawing = false;
        _previewShape = null;
        _polygonPoints.Clear();
        _polygonPreviewLines.Clear();
    }
}

public class ShapeCreatedEventArgs : EventArgs
{
    public required Data.Models.Shape Shape { get; init; }
}
