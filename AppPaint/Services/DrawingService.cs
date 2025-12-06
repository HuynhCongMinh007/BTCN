using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;
using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AppPaint.Services;

/// <summary>
/// Service to handle drawing shapes on canvas
/// </summary>
public class DrawingService
{
    // Brush cache for performance
    private static readonly Dictionary<string, SolidColorBrush> _brushCache = new();

    /// <summary>
    /// Get or create cached brush for color
    /// </summary>
    private static SolidColorBrush GetBrush(string hexColor)
    {
        if (_brushCache.TryGetValue(hexColor, out var cachedBrush))
        {
            return cachedBrush;
        }

        var brush = new SolidColorBrush(ParseColor(hexColor));
        _brushCache[hexColor] = brush;
        return brush;
    }

    /// <summary>
    /// Create a Line shape with optimizations
    /// </summary>
    public static Line CreateLine(Point start, Point end, string color, double thickness, string strokeStyle = "Solid", bool snapToAngle = false)
    {
        // Snap to 45-degree angles if enabled (hold Shift key)
        if (snapToAngle)
        {
            end = SnapToAngle(start, end);
        }

        // Validate points - skip zero-length lines for performance
        if (Math.Abs(end.X - start.X) < 0.01 && Math.Abs(end.Y - start.Y) < 0.01)
        {
            // Return minimal line for very short distances
            var minLine = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = start.X + 0.1,
                Y2 = start.Y + 0.1,
                Stroke = GetBrush(color),
                StrokeThickness = thickness,
                Opacity = 0.5
            };
            return minLine;
        }

        var line = new Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = GetBrush(color), // Use cached brush for performance
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round, // Smooth line joins
            StrokeDashArray = GetStrokeDashArray(strokeStyle),
            UseLayoutRounding = false // Anti-aliasing for smoother lines
        };

        return line;
    }

    /// <summary>
    /// Create arrow head at specific point and angle
    /// </summary>
    private static Polygon CreateArrowHead(Point position, double angleDegrees, double size, string color, double thickness)
    {
        double angleRadians = angleDegrees * Math.PI / 180.0;

        // Arrow points (relative to position)
        double arrowAngle = 25 * Math.PI / 180.0; // 25 degree arrow

        Point tip = position;
        Point left = new Point(
            position.X - size * Math.Cos(angleRadians - arrowAngle),
            position.Y - size * Math.Sin(angleRadians - arrowAngle)
        );
        Point right = new Point(
            position.X - size * Math.Cos(angleRadians + arrowAngle),
            position.Y - size * Math.Sin(angleRadians + arrowAngle)
        );

        var arrow = new Polygon
        {
            Points = new PointCollection { tip, left, right },
            Fill = GetBrush(color),
            Stroke = GetBrush(color),
            StrokeThickness = thickness / 2,
            StrokeLineJoin = PenLineJoin.Round
        };

        return arrow;
    }

    /// <summary>
    /// Snap line to nearest 45-degree angle (0°, 45°, 90°, 135°, 180°, etc.)
    /// Useful when holding Shift key
    /// </summary>
    public static Point SnapToAngle(Point start, Point end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 0.01) return end;

        // Calculate angle
        double angle = Math.Atan2(dy, dx);

        // Snap to nearest 45-degree increment
        double snapAngle = Math.Round(angle / (Math.PI / 4)) * (Math.PI / 4);

        // Calculate new endpoint
        double newX = start.X + distance * Math.Cos(snapAngle);
        double newY = start.Y + distance * Math.Sin(snapAngle);

        return new Point(newX, newY);
    }

    /// <summary>
    /// Calculate line length
    /// </summary>
    public static double GetLineLength(Point start, Point end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Get line angle in degrees (0-360)
    /// </summary>
    public static double GetLineAngle(Point start, Point end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double radians = Math.Atan2(dy, dx);
        double degrees = radians * (180.0 / Math.PI);

        // Normalize to 0-360
        if (degrees < 0) degrees += 360;

        return degrees;
    }

    /// <summary>
    /// Format line info for display (length and angle)
    /// </summary>
    public static string GetLineInfo(Point start, Point end)
    {
        double length = GetLineLength(start, end);
        double angle = GetLineAngle(start, end);
        return $"L: {length:F1}px  ∠: {angle:F1}°";
    }

    /// <summary>
    /// Create a Rectangle shape with optimizations
    /// </summary>
    public static Rectangle CreateRectangle(Point start, Point end, string strokeColor, double thickness, bool isFilled, string strokeStyle = "Solid", string? fillColor = null, bool snapToSquare = false, double cornerRadius = 0)
    {
        // Snap to square (equal width/height) if enabled (Shift key)
        if (snapToSquare)
        {
            end = SnapToSquare(start, end);
        }

        double width = Math.Abs(end.X - start.X);
        double height = Math.Abs(end.Y - start.Y);

        // Validate minimum size - skip too small rectangles
        if (width < 1 || height < 1)
        {
            // Return minimal rectangle for very small sizes
            var minRect = new Rectangle
            {
                Width = Math.Max(width, 1),
                Height = Math.Max(height, 1),
                Stroke = GetBrush(strokeColor),
                StrokeThickness = thickness,
                Opacity = 0.5,
                RadiusX = 0,
                RadiusY = 0
            };
            Canvas.SetLeft(minRect, Math.Min(start.X, end.X));
            Canvas.SetTop(minRect, Math.Min(start.Y, end.Y));
            return minRect;
        }

        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = GetBrush(strokeColor), // Use cached brush for performance
            StrokeThickness = thickness,
            StrokeDashArray = GetStrokeDashArray(strokeStyle),
            StrokeLineJoin = PenLineJoin.Round, // Smooth corners
            RadiusX = cornerRadius,
            RadiusY = cornerRadius,
            UseLayoutRounding = false // Anti-aliasing
        };

        Canvas.SetLeft(rect, Math.Min(start.X, end.X));
        Canvas.SetTop(rect, Math.Min(start.Y, end.Y));

        if (isFilled && !string.IsNullOrEmpty(fillColor))
        {
            rect.Fill = GetBrush(fillColor); // Use cached brush
        }

        return rect;
    }

    /// <summary>
    /// Snap rectangle to square (equal width and height)
    /// Useful when holding Shift key
    /// </summary>
    public static Point SnapToSquare(Point start, Point end)
    {
        double width = Math.Abs(end.X - start.X);
        double height = Math.Abs(end.Y - start.Y);
        double size = Math.Max(width, height);

        // Maintain drag direction
        double newX = start.X + (end.X > start.X ? size : -size);
        double newY = start.Y + (end.Y > start.Y ? size : -size);

        return new Point(newX, newY);
    }

    /// <summary>
    /// Get rectangle dimensions
    /// </summary>
    public static (double Width, double Height) GetRectangleDimensions(Point start, Point end)
    {
        return (Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
    }

    /// <summary>
    /// Get rectangle area
    /// </summary>
    public static double GetRectangleArea(Point start, Point end)
    {
        var (width, height) = GetRectangleDimensions(start, end);
        return width * height;
    }

    /// <summary>
    /// Get rectangle perimeter
    /// </summary>
    public static double GetRectanglePerimeter(Point start, Point end)
    {
        var (width, height) = GetRectangleDimensions(start, end);
        return 2 * (width + height);
    }

    /// <summary>
    /// Format rectangle info for display
    /// </summary>
    public static string GetRectangleInfo(Point start, Point end)
    {
        var (width, height) = GetRectangleDimensions(start, end);
        double area = width * height;
        return $"W: {width:F1}px  H: {height:F1}px  A: {area:F0}px²";
    }

    /// <summary>
    /// Create an Ellipse (Circle or Oval) with optimizations
    /// </summary>
    public static Ellipse CreateEllipse(Point start, Point end, string strokeColor, double thickness, bool isCircle, bool isFilled, string strokeStyle = "Solid", string? fillColor = null)
    {
        double width = Math.Abs(end.X - start.X);
        double height = Math.Abs(end.Y - start.Y);

        // Validate minimum size
        if (width < 1 || height < 1)
        {
            var minEllipse = new Ellipse
            {
                Width = Math.Max(width, 1),
                Height = Math.Max(height, 1),
                Stroke = GetBrush(strokeColor),
                StrokeThickness = thickness,
                Opacity = 0.5
            };
            Canvas.SetLeft(minEllipse, Math.Min(start.X, end.X));
            Canvas.SetTop(minEllipse, Math.Min(start.Y, end.Y));
            return minEllipse;
        }

        if (isCircle)
        {
            // Make it a perfect circle
            double size = Math.Max(width, height);
            width = height = size;
        }

        var ellipse = new Ellipse
        {
            Width = width,
            Height = height,
            Stroke = GetBrush(strokeColor), // Use cached brush
            StrokeThickness = thickness,
            StrokeDashArray = GetStrokeDashArray(strokeStyle),
            UseLayoutRounding = false // Anti-aliasing
        };

        Canvas.SetLeft(ellipse, Math.Min(start.X, end.X));
        Canvas.SetTop(ellipse, Math.Min(start.Y, end.Y));

        if (isFilled && !string.IsNullOrEmpty(fillColor))
        {
            ellipse.Fill = GetBrush(fillColor); // Use cached brush
        }

        return ellipse;
    }

    /// <summary>
    /// Get ellipse/circle dimensions
    /// </summary>
    public static (double Width, double Height) GetEllipseDimensions(Point start, Point end, bool isCircle)
    {
        double width = Math.Abs(end.X - start.X);
        double height = Math.Abs(end.Y - start.Y);

        if (isCircle)
        {
            double size = Math.Max(width, height);
            return (size, size);
        }

        return (width, height);
    }

    /// <summary>
    /// Get circle/ellipse area (approximation)
    /// </summary>
    public static double GetEllipseArea(Point start, Point end, bool isCircle)
    {
        var (width, height) = GetEllipseDimensions(start, end, isCircle);
        double radiusX = width / 2;
        double radiusY = height / 2;
        return Math.PI * radiusX * radiusY;
    }

    /// <summary>
    /// Get circle/ellipse circumference (approximation)
    /// </summary>
    public static double GetEllipseCircumference(Point start, Point end, bool isCircle)
    {
        var (width, height) = GetEllipseDimensions(start, end, isCircle);

        if (isCircle)
        {
            // Perfect circle: C = 2πr
            return Math.PI * width;
        }

        // Ramanujan approximation for ellipse
        double a = width / 2;
        double b = height / 2;
        return Math.PI * (3 * (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
    }

    /// <summary>
    /// Format ellipse/circle info for display
    /// </summary>
    public static string GetEllipseInfo(Point start, Point end, bool isCircle)
    {
        var (width, height) = GetEllipseDimensions(start, end, isCircle);
        double area = GetEllipseArea(start, end, isCircle);

        if (isCircle)
        {
            double diameter = width;
            double radius = width / 2;
            return $"⌀: {diameter:F1}px  R: {radius:F1}px  A: {area:F0}px²";
        }

        return $"W: {width:F1}px  H: {height:F1}px  A: {area:F0}px²";
    }

    /// <summary>
    /// Create a Triangle shape with optimizations
    /// </summary>
    public static Polygon CreateTriangle(Point start, Point end, string strokeColor, double thickness, bool isFilled, string strokeStyle = "Solid", string? fillColor = null, bool isEquilateral = false)
    {
        // Calculate triangle points
        var points = CalculateTrianglePoints(start, end, isEquilateral);

        var triangle = new Polygon
        {
            Stroke = GetBrush(strokeColor), // Use cached brush
            StrokeThickness = thickness,
            StrokeDashArray = GetStrokeDashArray(strokeStyle),
            StrokeLineJoin = PenLineJoin.Round, // Smooth corners
            Points = new PointCollection(),
            UseLayoutRounding = false // Anti-aliasing
        };

        // Add points
        foreach (var point in points)
        {
            triangle.Points.Add(point);
        }

        if (isFilled && !string.IsNullOrEmpty(fillColor))
        {
            triangle.Fill = GetBrush(fillColor); // Use cached brush
        }

        return triangle;
    }

    /// <summary>
    /// Calculate triangle points (isosceles by default, equilateral if flag set)
    /// </summary>
    private static List<Point> CalculateTrianglePoints(Point start, Point end, bool isEquilateral)
    {
        if (!isEquilateral)
        {
            // Isosceles triangle (default)
            return new List<Point>
            {
                new Point((start.X + end.X) / 2, start.Y), // Top center
                new Point(start.X, end.Y),         // Bottom left
                new Point(end.X, end.Y)        // Bottom right
            };
        }

        // Equilateral triangle
        double width = Math.Abs(end.X - start.X);
        double height = width * Math.Sqrt(3) / 2; // Equilateral height

        double centerX = (start.X + end.X) / 2;
        double topY = Math.Min(start.Y, end.Y);

        return new List<Point>
        {
            new Point(centerX, topY),    // Top center
            new Point(start.X, topY + height),  // Bottom left
            new Point(end.X, topY + height)        // Bottom right
        };
    }

    /// <summary>
    /// Get triangle area
    /// </summary>
    public static double GetTriangleArea(Point start, Point end)
    {
        double baseWidth = Math.Abs(end.X - start.X);
        double height = Math.Abs(end.Y - start.Y);
        return (baseWidth * height) / 2;
    }

    /// <summary>
    /// Get triangle perimeter
    /// </summary>
    public static double GetTrianglePerimeter(Point start, Point end)
    {
        var points = CalculateTrianglePoints(start, end, false);

        // Calculate distances between points
        double side1 = GetLineLength(points[0], points[1]);
        double side2 = GetLineLength(points[1], points[2]);
        double side3 = GetLineLength(points[2], points[0]);

        return side1 + side2 + side3;
    }

    /// <summary>
    /// Format triangle info for display
    /// </summary>
    public static string GetTriangleInfo(Point start, Point end)
    {
        double area = GetTriangleArea(start, end);
        double baseWidth = Math.Abs(end.X - start.X);
        double height = Math.Abs(end.Y - start.Y);

        return $"Base: {baseWidth:F1}px  H: {height:F1}px  A: {area:F0}px²";
    }

    /// <summary>
    /// Get StrokeDashArray based on stroke style
    /// </summary>
    public static DoubleCollection? GetStrokeDashArray(string strokeStyle)
    {
        return strokeStyle switch
        {
            "Dash" => new DoubleCollection { 4, 2 },      // Dashed line
            "Dot" => new DoubleCollection { 1, 2 },       // Dotted line
            "DashDot" => new DoubleCollection { 4, 2, 1, 2 }, // Dash-dot line
            "DashDotDot" => new DoubleCollection { 4, 2, 1, 2, 1, 2 }, // Dash-dot-dot
            _ => null  // Solid (default)
        };
    }

    /// <summary>
    /// Parse color string (#RRGGBB) to Windows.UI.Color
    /// </summary>
    public static Windows.UI.Color ParseColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || hexColor.Length != 7 || hexColor[0] != '#')
        {
            return Microsoft.UI.Colors.Black;
        }

        try
        {
            byte r = Convert.ToByte(hexColor.Substring(1, 2), 16);
            byte g = Convert.ToByte(hexColor.Substring(3, 2), 16);
            byte b = Convert.ToByte(hexColor.Substring(5, 2), 16);
            return Windows.UI.Color.FromArgb(255, r, g, b);
        }
        catch
        {
            return Microsoft.UI.Colors.Black;
        }
    }

    /// <summary>
    /// Convert Points to JSON string for database storage
    /// </summary>
    public static string PointsToJson(List<Point> points)
    {
        var pointsData = points.Select(p => new { X = p.X, Y = p.Y });
        return System.Text.Json.JsonSerializer.Serialize(pointsData);
    }

    /// <summary>
    /// Parse JSON string to Points list
    /// </summary>
    public static List<Point> JsonToPoints(string json)
    {
        try
        {
            var pointsData = System.Text.Json.JsonSerializer.Deserialize<List<PointData>>(json);
            return pointsData?.Select(p => new Point(p.X, p.Y)).ToList() ?? new List<Point>();
        }
        catch
        {
            return new List<Point>();
        }
    }

    private class PointData
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// Create a Polygon shape with multiple points
    /// </summary>
    public static Polygon CreatePolygon(List<Point> points, string strokeColor, double thickness, bool isFilled, string strokeStyle = "Solid", string? fillColor = null)
    {
        var polygon = new Polygon
        {
          Stroke = GetBrush(strokeColor), // Use cached brush
   StrokeThickness = thickness,
     StrokeDashArray = GetStrokeDashArray(strokeStyle),
   StrokeLineJoin = PenLineJoin.Round, // Smooth corners
            UseLayoutRounding = false // Anti-aliasing
        };

        // Add points manually
        foreach (var point in points)
        {
   polygon.Points.Add(point);
        }

        if (isFilled && !string.IsNullOrEmpty(fillColor))
        {
        polygon.Fill = GetBrush(fillColor); // Use cached brush
        }

        return polygon;
    }

    /// <summary>
    /// Get polygon area (using shoelace formula)
    /// </summary>
    public static double GetPolygonArea(List<Point> points)
 {
   if (points.Count < 3) return 0;

        double area = 0;
        for (int i = 0; i < points.Count; i++)
      {
   int j = (i + 1) % points.Count;
            area += points[i].X * points[j].Y;
area -= points[j].X * points[i].Y;
   }

     return Math.Abs(area / 2);
    }

  /// <summary>
    /// Get polygon perimeter
    /// </summary>
    public static double GetPolygonPerimeter(List<Point> points)
    {
        if (points.Count < 2) return 0;

   double perimeter = 0;
   for (int i = 0; i < points.Count; i++)
  {
     int j = (i + 1) % points.Count;
        perimeter += GetLineLength(points[i], points[j]);
        }

        return perimeter;
  }
}
