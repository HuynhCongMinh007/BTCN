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
    /// <summary>
    /// Create a Line shape
    /// </summary>
    public static Line CreateLine(Point start, Point end, string color, double thickness, string strokeStyle = "Solid")
    {
        var line = new Line
     {
            X1 = start.X,
     Y1 = start.Y,
 X2 = end.X,
   Y2 = end.Y,
    Stroke = new SolidColorBrush(ColorHelper.FromArgb(
255,
  Convert.ToByte(color.Substring(1, 2), 16),
 Convert.ToByte(color.Substring(3, 2), 16),
  Convert.ToByte(color.Substring(5, 2), 16)
            )),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
 StrokeDashArray = GetStrokeDashArray(strokeStyle)
    };
  return line;
    }

    /// <summary>
    /// Create a Rectangle shape
    /// </summary>
    public static Rectangle CreateRectangle(Point start, Point end, string strokeColor, double thickness, bool isFilled, string strokeStyle = "Solid", string? fillColor = null)
    {
        var rect = new Rectangle
        {
     Width = Math.Abs(end.X - start.X),
       Height = Math.Abs(end.Y - start.Y),
    Stroke = new SolidColorBrush(ParseColor(strokeColor)),
  StrokeThickness = thickness,
     StrokeDashArray = GetStrokeDashArray(strokeStyle)
        };

 Canvas.SetLeft(rect, Math.Min(start.X, end.X));
      Canvas.SetTop(rect, Math.Min(start.Y, end.Y));

        if (isFilled && !string.IsNullOrEmpty(fillColor))
        {
       rect.Fill = new SolidColorBrush(ParseColor(fillColor));
      }

        return rect;
    }

    /// <summary>
    /// Create an Ellipse (Circle or Oval)
    /// </summary>
    public static Ellipse CreateEllipse(Point start, Point end, string strokeColor, double thickness, bool isCircle, bool isFilled, string strokeStyle = "Solid", string? fillColor = null)
    {
        double width = Math.Abs(end.X - start.X);
        double height = Math.Abs(end.Y - start.Y);

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
       Stroke = new SolidColorBrush(ParseColor(strokeColor)),
          StrokeThickness = thickness,
            StrokeDashArray = GetStrokeDashArray(strokeStyle)
     };

        Canvas.SetLeft(ellipse, Math.Min(start.X, end.X));
     Canvas.SetTop(ellipse, Math.Min(start.Y, end.Y));

     if (isFilled && !string.IsNullOrEmpty(fillColor))
        {
     ellipse.Fill = new SolidColorBrush(ParseColor(fillColor));
      }

        return ellipse;
    }

    /// <summary>
    /// Create a Triangle shape
    /// </summary>
    public static Polygon CreateTriangle(Point start, Point end, string strokeColor, double thickness, bool isFilled, string strokeStyle = "Solid", string? fillColor = null)
    {
    var triangle = new Polygon
     {
        Stroke = new SolidColorBrush(ParseColor(strokeColor)),
         StrokeThickness = thickness,
          StrokeDashArray = GetStrokeDashArray(strokeStyle),
            Points = new PointCollection
            {
      new Point((start.X + end.X) / 2, start.Y), // Top center
new Point(start.X, end.Y),     // Bottom left
        new Point(end.X, end.Y)       // Bottom right
 }
        };

        if (isFilled && !string.IsNullOrEmpty(fillColor))
 {
    triangle.Fill = new SolidColorBrush(ParseColor(fillColor));
     }

      return triangle;
    }

    /// <summary>
    /// Create a Polygon shape with multiple points
    /// </summary>
  public static Polygon CreatePolygon(List<Point> points, string strokeColor, double thickness, bool isFilled, string strokeStyle = "Solid", string? fillColor = null)
    {
   var polygon = new Polygon
   {
  Stroke = new SolidColorBrush(ParseColor(strokeColor)),
      StrokeThickness = thickness,
         StrokeDashArray = GetStrokeDashArray(strokeStyle)
     };

        // Add points manually
 foreach (var point in points)
        {
polygon.Points.Add(point);
  }

   if (isFilled && !string.IsNullOrEmpty(fillColor))
    {
     polygon.Fill = new SolidColorBrush(ParseColor(fillColor));
      }

        return polygon;
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
}
