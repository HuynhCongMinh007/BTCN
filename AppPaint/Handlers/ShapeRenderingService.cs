using Microsoft.UI.Xaml.Controls;
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
/// Handles rendering shapes from data models to UI canvas
/// </summary>
public class ShapeRenderingService
{
    private readonly DrawingService _drawingService;

    public ShapeRenderingService(DrawingService drawingService)
    {
        _drawingService = drawingService;
    }

    /// <summary>
    /// Render all shapes from data models to canvas
    /// </summary>
    public void RenderAllShapes(IEnumerable<Data.Models.Shape> shapes, Canvas canvas, double scale = 1.0)
    {
        System.Diagnostics.Debug.WriteLine($"🎨 Rendering {shapes.Count()} shapes from data (scale: {scale})");

        // Clear canvas
        canvas.Children.Clear();

        // Render each shape with scale
        foreach (var shape in shapes)
        {
            RenderShape(shape, canvas, scale);
        }

        System.Diagnostics.Debug.WriteLine($"✅ Rendered shapes to canvas");
    }

    /// <summary>
    /// Render shapes with auto-fit scaling to fill the canvas optimally
    /// </summary>
    public void RenderShapesWithAutoFit(IEnumerable<Data.Models.Shape> shapes, Canvas canvas, double padding = 20)
    {
        var shapeList = shapes.ToList();
        if (!shapeList.Any())
        {
            System.Diagnostics.Debug.WriteLine("⚠️ No shapes to render");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"🎨 Auto-fitting {shapeList.Count} shapes to canvas {canvas.Width}x{canvas.Height}");

        // Step 1: Calculate bounds of all shapes
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var shape in shapeList)
        {
            var points = DrawingService.JsonToPoints(shape.PointsData);
            foreach (var point in points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }
        }

        if (minX == double.MaxValue || minY == double.MaxValue)
        {
            System.Diagnostics.Debug.WriteLine("❌ Invalid shape bounds");
            return;
        }

        // Step 2: Calculate content dimensions
        double contentWidth = maxX - minX;
        double contentHeight = maxY - minY;

        System.Diagnostics.Debug.WriteLine($"📐 Content bounds: ({minX},{minY}) to ({maxX},{maxY})");
        System.Diagnostics.Debug.WriteLine($"📏 Content size: {contentWidth} x {contentHeight}");

        // Step 3: Calculate optimal scale to fit canvas with padding
        double availableWidth = canvas.Width - (padding * 2);
        double availableHeight = canvas.Height - (padding * 2);

        double scaleX = availableWidth / contentWidth;
        double scaleY = availableHeight / contentHeight;
        double optimalScale = Math.Min(scaleX, scaleY);

        // Limit scale to reasonable range
        optimalScale = Math.Max(0.1, Math.Min(optimalScale, 5.0));

        System.Diagnostics.Debug.WriteLine($"⚖️ Optimal scale: {optimalScale:F2}");

        // Step 4: Calculate centering offset
        double scaledWidth = contentWidth * optimalScale;
        double scaledHeight = contentHeight * optimalScale;

        double offsetX = (canvas.Width - scaledWidth) / 2 - (minX * optimalScale);
        double offsetY = (canvas.Height - scaledHeight) / 2 - (minY * optimalScale);

        System.Diagnostics.Debug.WriteLine($"🎯 Offset: ({offsetX:F1}, {offsetY:F1})");

        // Step 5: Clear canvas and render shapes with scale and offset
        canvas.Children.Clear();

        foreach (var shape in shapeList)
        {
            var points = DrawingService.JsonToPoints(shape.PointsData);

            // Apply scale and offset to all points
            var transformedPoints = points
                .Select(p => new Point(
                    p.X * optimalScale + offsetX,
                    p.Y * optimalScale + offsetY
                ))
                .ToList();

            // Create transformed shape data
            var transformedShape = new Data.Models.Shape
            {
                Id = shape.Id,
                ShapeType = shape.ShapeType,
                PointsData = DrawingService.PointsToJson(transformedPoints),
                Color = shape.Color,
                StrokeThickness = shape.StrokeThickness * optimalScale,
                StrokeStyle = shape.StrokeStyle,
                IsFilled = shape.IsFilled,
                FillColor = shape.FillColor
            };

            RenderShape(transformedShape, canvas, scale: 1.0);
        }

        System.Diagnostics.Debug.WriteLine($"✅ Auto-fitted {shapeList.Count} shapes successfully");
    }

    /// <summary>
    /// Render a single shape from data model to UI canvas
    /// </summary>
    public UIShape? RenderShape(Data.Models.Shape shape, Canvas canvas, double scale = 1.0)
    {
        try
        {
            // Parse points data
            var points = DrawingService.JsonToPoints(shape.PointsData);
            if (points.Count < 2)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Invalid points data for shape {shape.Id}");
                return null;
            }

            // Apply scale to points
            if (scale != 1.0)
            {
                points = points.Select(p => new Point(p.X * scale, p.Y * scale)).ToList();
            }

            UIShape? uiShape = null;

            switch (shape.ShapeType)
            {
                case ShapeType.Line:
                    if (points.Count >= 2)
                    {
                        uiShape = DrawingService.CreateLine(
                            points[0], points[1],
                            shape.Color,
                            shape.StrokeThickness * scale,
                            shape.StrokeStyle
                        );
                    }
                    break;

                case ShapeType.Rectangle:
                    if (points.Count >= 2)
                    {
                        uiShape = DrawingService.CreateRectangle(
                            points[0], points[1],
                            shape.Color,
                            shape.StrokeThickness * scale,
                            shape.IsFilled,
                            shape.StrokeStyle,
                            shape.FillColor
                        );
                    }
                    break;

                case ShapeType.Circle:
                    if (points.Count >= 2)
                    {
                        uiShape = DrawingService.CreateEllipse(
                            points[0], points[1],
                            shape.Color,
                            shape.StrokeThickness * scale,
                            isCircle: true,
                            shape.IsFilled,
                            shape.StrokeStyle,
                            shape.FillColor
                        );
                    }
                    break;

                case ShapeType.Oval:
                    if (points.Count >= 2)
                    {
                        uiShape = DrawingService.CreateEllipse(
                            points[0], points[1],
                            shape.Color,
                            shape.StrokeThickness * scale,
                            isCircle: false,
                            shape.IsFilled,
                            shape.StrokeStyle,
                            shape.FillColor
                        );
                    }
                    break;

                case ShapeType.Triangle:
                    if (points.Count >= 2)
                    {
                        uiShape = DrawingService.CreateTriangle(
                            points[0], points[1],
                            shape.Color,
                            shape.StrokeThickness * scale,
                            shape.IsFilled,
                            shape.StrokeStyle,
                            shape.FillColor
                        );
                    }
                    break;

                case ShapeType.Polygon:
                    if (points.Count >= 3)
                    {
                        uiShape = DrawingService.CreatePolygon(
                            points,
                            shape.Color,
                            shape.StrokeThickness * scale,
                            shape.IsFilled,
                            shape.StrokeStyle,
                            shape.FillColor
                        );
                    }
                    break;
            }

            if (uiShape != null)
            {
                canvas.Children.Add(uiShape);
                System.Diagnostics.Debug.WriteLine($"✅ Rendered {shape.ShapeType} shape (ID: {shape.Id})");
                return uiShape;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to render {shape.ShapeType} shape (ID: {shape.Id})");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error rendering shape {shape.Id}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Convert UI shape to data model
    /// </summary>
    public Data.Models.Shape? ConvertToDataModel(UIShape uiShape, int templateId)
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
}
