using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using AppPaint.Services;
using System;
using Windows.Foundation;
using UIShape = Microsoft.UI.Xaml.Shapes.Shape;

namespace AppPaint.Handlers;

/// <summary>
/// Handles editing operations on shapes (move, resize, property changes)
/// </summary>
public class ShapeEditHandler
{
    private bool _isDraggingShape = false;
    private bool _isResizingShape = false;
  private Point _dragStartPoint;
    private Point _shapeStartPosition;

    public bool IsDragging => _isDraggingShape;
    public bool IsResizing => _isResizingShape;

    public void StartDragging(UIShape shape, Point startPoint, Canvas canvas, PointerRoutedEventArgs e)
    {
      _isDraggingShape = true;
        _dragStartPoint = startPoint;
        _shapeStartPosition = GetShapePosition(shape);
        canvas.CapturePointer(e.Pointer);
     System.Diagnostics.Debug.WriteLine("Started dragging shape");
    }

  public void StartResizing(Point startPoint, Canvas canvas, PointerRoutedEventArgs e)
    {
        _isResizingShape = true;
        _dragStartPoint = startPoint;
   canvas.CapturePointer(e.Pointer);
        System.Diagnostics.Debug.WriteLine("Started resizing shape");
    }

    public void DragShape(UIShape shape, Point currentPoint, Canvas canvas)
  {
        if (!_isDraggingShape) return;

        var deltaX = currentPoint.X - _dragStartPoint.X;
        var deltaY = currentPoint.Y - _dragStartPoint.Y;

        MoveShape(shape, _shapeStartPosition.X + deltaX, _shapeStartPosition.Y + deltaY, canvas);
    }

    public void ResizeShape(UIShape shape, Point currentPoint)
    {
        if (!_isResizingShape) return;

        var deltaX = currentPoint.X - _dragStartPoint.X;
        var deltaY = currentPoint.Y - _dragStartPoint.Y;

        if (shape is Line line)
        {
            // For lines, resize by moving the end point (X2, Y2)
   line.X2 += deltaX;
            line.Y2 += deltaY;
            
     _dragStartPoint = currentPoint;
        }
        else if (shape is Rectangle rect)
        {
   var newWidth = Math.Max(20, rect.Width + deltaX);
            var newHeight = Math.Max(20, rect.Height + deltaY);
   rect.Width = newWidth;
            rect.Height = newHeight;
   _dragStartPoint.X += deltaX;
          _dragStartPoint.Y += deltaY;
        }
        else if (shape is Ellipse ellipse)
        {
          var newWidth = Math.Max(20, ellipse.Width + deltaX);
            var newHeight = Math.Max(20, ellipse.Height + deltaY);
      ellipse.Width = newWidth;
 ellipse.Height = newHeight;
    _dragStartPoint.X += deltaX;
      _dragStartPoint.Y += deltaY;
        }
 else if (shape is Polygon polygon)
     {
          var bounds = GetPolygonBounds(polygon.Points);
     var scaleX = Math.Max(0.1, (bounds.Width + deltaX) / bounds.Width);
         var scaleY = Math.Max(0.1, (bounds.Height + deltaY) / bounds.Height);

       var newPoints = new PointCollection();
   foreach (var pt in polygon.Points)
            {
           var relX = (pt.X - bounds.Left) * scaleX;
         var relY = (pt.Y - bounds.Top) * scaleY;
 newPoints.Add(new Point(bounds.Left + relX, bounds.Top + relY));
            }
    polygon.Points = newPoints;
    _dragStartPoint.X += deltaX;
   _dragStartPoint.Y += deltaY;
        }
    }

    public void EndDragging(Canvas canvas, PointerRoutedEventArgs e)
    {
        _isDraggingShape = false;
     canvas.ReleasePointerCapture(e.Pointer);
    System.Diagnostics.Debug.WriteLine("Finished dragging shape");
    }

    public void EndResizing(Canvas canvas, PointerRoutedEventArgs e)
    {
    _isResizingShape = false;
        canvas.ReleasePointerCapture(e.Pointer);
        System.Diagnostics.Debug.WriteLine("Finished resizing shape");
    }

    public void UpdateShapeStrokeColor(UIShape shape, string colorHex)
  {
        var color = DrawingService.ParseColor(colorHex);
        shape.Stroke = new SolidColorBrush(color);
    System.Diagnostics.Debug.WriteLine($"Updated shape stroke color: {colorHex}");
    }

    public void UpdateShapeFillColor(UIShape shape, string colorHex, bool isFilled)
    {
        if (isFilled)
      {
var color = DrawingService.ParseColor(colorHex);
          shape.Fill = new SolidColorBrush(color);
            System.Diagnostics.Debug.WriteLine($"Updated shape fill color: {colorHex}");
        }
        else
        {
        shape.Fill = null;
            System.Diagnostics.Debug.WriteLine("Shape fill removed");
      }
    }

    public void UpdateShapeThickness(UIShape shape, double thickness)
    {
shape.StrokeThickness = thickness;
        System.Diagnostics.Debug.WriteLine($"Updated shape thickness: {thickness}");
    }

    public void UpdateShapeStrokeStyle(UIShape shape, string strokeStyle)
    {
        shape.StrokeDashArray = DrawingService.GetStrokeDashArray(strokeStyle);
      System.Diagnostics.Debug.WriteLine($"Updated shape stroke style: {strokeStyle}");
    }

    public void DeleteShape(UIShape shape, Canvas canvas)
    {
      canvas.Children.Remove(shape);
        System.Diagnostics.Debug.WriteLine("Shape deleted");
    }

    private void MoveShape(UIShape shape, double newX, double newY, Canvas canvas)
    {
        // Clamp to canvas bounds
     newX = Math.Max(0, Math.Min(canvas.Width - GetShapeWidth(shape), newX));
      newY = Math.Max(0, Math.Min(canvas.Height - GetShapeHeight(shape), newY));

        if (shape is Line line)
        {
     var width = Math.Abs(line.X2 - line.X1);
 var height = Math.Abs(line.Y2 - line.Y1);
       var wasInverted = line.X2 < line.X1;

    line.X1 = newX;
       line.Y1 = newY;
            line.X2 = wasInverted ? newX - width : newX + width;
         line.Y2 = newY + height;
        }
        else if (shape is Rectangle || shape is Ellipse)
   {
     Canvas.SetLeft(shape, newX);
    Canvas.SetTop(shape, newY);
        }
else if (shape is Polygon polygon)
      {
          var bounds = GetPolygonBounds(polygon.Points);
            var offsetX = newX - bounds.Left;
        var offsetY = newY - bounds.Top;

    var newPoints = new PointCollection();
            foreach (var pt in polygon.Points)
    {
           newPoints.Add(new Point(pt.X + offsetX, pt.Y + offsetY));
            }
polygon.Points = newPoints;
        }
    }

    private Point GetShapePosition(UIShape shape)
    {
 if (shape is Line line)
        {
  return new Point(Math.Min(line.X1, line.X2), Math.Min(line.Y1, line.Y2));
    }
        else if (shape is Polygon polygon)
      {
     var bounds = GetPolygonBounds(polygon.Points);
    return new Point(bounds.Left, bounds.Top);
        }
        else
     {
        return new Point(Canvas.GetLeft(shape), Canvas.GetTop(shape));
        }
    }

    private double GetShapeWidth(UIShape shape)
    {
        if (shape is Line line) return Math.Abs(line.X2 - line.X1);
     if (shape is Rectangle rect) return rect.Width;
        if (shape is Ellipse ellipse) return ellipse.Width;
        if (shape is Polygon polygon) return GetPolygonBounds(polygon.Points).Width;
        return 0;
    }

    private double GetShapeHeight(UIShape shape)
    {
        if (shape is Line line) return Math.Abs(line.Y2 - line.Y1);
if (shape is Rectangle rect) return rect.Height;
if (shape is Ellipse ellipse) return ellipse.Height;
  if (shape is Polygon polygon) return GetPolygonBounds(polygon.Points).Height;
        return 0;
    }

    private Rect GetPolygonBounds(PointCollection points)
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

    public void Reset()
    {
     _isDraggingShape = false;
        _isResizingShape = false;
    }
}
