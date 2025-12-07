using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using UIShape = Microsoft.UI.Xaml.Shapes.Shape;

namespace AppPaint.Handlers;

/// <summary>
/// Handles shape selection and selection UI
/// </summary>
public class ShapeSelectionHandler
{
    private UIShape? _selectedShape = null;
    private Rectangle? _selectionBorder = null; // ✅ Changed from Border to Rectangle
    private Ellipse? _resizeHandle;

    public UIShape? SelectedShape => _selectedShape;
    public bool HasSelection => _selectedShape != null;

    public event EventHandler<ShapeSelectedEventArgs>? ShapeSelected;
    public event EventHandler? SelectionCleared;
    public event EventHandler<PointerRoutedEventArgs>? ResizeHandlePressed;

    public void SelectShapeAtPoint(Point point, Canvas canvas)
    {
        // Find shape at point (reverse order - top shape first)
        for (int i = canvas.Children.Count - 1; i >= 0; i--)
        {
            var element = canvas.Children[i];

            if (element is UIShape shape && IsPointInShape(shape, point))
            {
                // Clear previous selection first
                ClearSelection(canvas);

                _selectedShape = shape;
                ShowSelectionBorder(shape, canvas);
                ShapeSelected?.Invoke(this, new ShapeSelectedEventArgs { Shape = shape });
                System.Diagnostics.Debug.WriteLine($"Selected shape: {shape.GetType().Name}");
                return;
            }
        }

        // No shape found at point - clear selection
        ClearSelection(canvas);
        System.Diagnostics.Debug.WriteLine("Clicked on empty area - selection cleared");
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

        if (lengthSquared == 0)
            return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));

        double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;
        t = Math.Max(0, Math.Min(1, t));

        double nearestX = lineStart.X + t * dx;
        double nearestY = lineStart.Y + t * dy;

        return Math.Sqrt(Math.Pow(point.X - nearestX, 2) + Math.Pow(point.Y - nearestY, 2));
    }

    private bool IsPointInPolygon(Point point, PointCollection points)
    {
        bool inside = false;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            if ((points[i].Y > point.Y) != (points[j].Y > point.Y) &&
                     point.X < (points[j].X - points[i].X) * (point.Y - points[i].Y) /
                 (points[j].Y - points[i].Y) + points[i].X)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private void ShowSelectionBorder(UIShape shape, Canvas canvas)
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
            left = bounds.Left;
            top = bounds.Top;
            width = bounds.Width;
            height = bounds.Height;
        }

        // ✅ Create selection border as Rectangle with dashed stroke
        _selectionBorder = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 2 }, // ✅ Dashed pattern (4px dash, 2px gap)
            RadiusX = 4,
            RadiusY = 4,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(_selectionBorder, left);
        Canvas.SetTop(_selectionBorder, top);
        canvas.Children.Add(_selectionBorder);

        // Add resize handle (bottom-right corner) for ALL shapes including Lines
        _resizeHandle = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
            StrokeThickness = 2
        };

        // For lines, place handle at the end point (X2, Y2)
        if (shape is Line lineShape)
        {
            Canvas.SetLeft(_resizeHandle, lineShape.X2 - 6);
            Canvas.SetTop(_resizeHandle, lineShape.Y2 - 6);
        }
        else
        {
            Canvas.SetLeft(_resizeHandle, left + width - 6);
            Canvas.SetTop(_resizeHandle, top + height - 6);
        }

        Canvas.SetZIndex(_resizeHandle, 1000);
        canvas.Children.Add(_resizeHandle);

        _resizeHandle.PointerPressed += (s, e) =>
               {
                   ResizeHandlePressed?.Invoke(this, e);
               };
    }

    public void UpdateSelectionBorder(Canvas canvas)
    {
        if (_selectedShape == null || _selectionBorder == null) return;

        var left = 0.0;
        var top = 0.0;
        var width = 0.0;
        var height = 0.0;

        if (_selectedShape is Line line)
        {
            left = Math.Min(line.X1, line.X2) - 5;
            top = Math.Min(line.Y1, line.Y2) - 5;
            width = Math.Abs(line.X2 - line.X1) + 10;
            height = Math.Abs(line.Y2 - line.Y1) + 10;
        }
        else if (_selectedShape is Polygon polygon)
        {
            var bounds = GetPolygonBounds(polygon.Points);
            left = bounds.Left;
            top = bounds.Top;
            width = bounds.Width;
            height = bounds.Height;
        }
        else
        {
            left = Canvas.GetLeft(_selectedShape);
            top = Canvas.GetTop(_selectedShape);
            width = GetShapeWidth(_selectedShape);
            height = GetShapeHeight(_selectedShape);
        }

        _selectionBorder.Width = width;
        _selectionBorder.Height = height;
        Canvas.SetLeft(_selectionBorder, left);
        Canvas.SetTop(_selectionBorder, top);

        if (_resizeHandle != null)
        {
            // For lines, update handle to follow the end point (X2, Y2)
            if (_selectedShape is Line lineShape)
            {
                Canvas.SetLeft(_resizeHandle, lineShape.X2 - 6);
                Canvas.SetTop(_resizeHandle, lineShape.Y2 - 6);
            }
            else
            {
                Canvas.SetLeft(_resizeHandle, left + width - 6);
                Canvas.SetTop(_resizeHandle, top + height - 6);
            }
        }
    }

    public void ClearSelection(Canvas canvas)
    {
        if (_selectionBorder != null)
        {
            canvas.Children.Remove(_selectionBorder);
            _selectionBorder = null;
        }
        if (_resizeHandle != null)
        {
            canvas.Children.Remove(_resizeHandle);
            _resizeHandle = null;
        }
        _selectedShape = null;
        SelectionCleared?.Invoke(this, EventArgs.Empty);
    }

    public ShapeProperties? GetSelectedShapeProperties()
    {
        if (_selectedShape == null) return null;

        var props = new ShapeProperties();

        // Get stroke color
        if (_selectedShape.Stroke is SolidColorBrush strokeBrush)
        {
            var color = strokeBrush.Color;
            props.StrokeColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        // Get thickness
        props.StrokeThickness = _selectedShape.StrokeThickness;

        // Get fill status and color
        props.IsFilled = _selectedShape.Fill != null;
        if (_selectedShape.Fill is SolidColorBrush fillBrush)
        {
            var fillColor = fillBrush.Color;
            props.FillColor = $"#{fillColor.R:X2}{fillColor.G:X2}{fillColor.B:X2}";
        }

        return props;
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
}

public class ShapeSelectedEventArgs : EventArgs
{
    public required UIShape Shape { get; init; }
}

public class ShapeProperties
{
    public string StrokeColor { get; set; } = "#000000";
    public double StrokeThickness { get; set; }
    public bool IsFilled { get; set; }
    public string FillColor { get; set; } = "#FFFFFF";
}
