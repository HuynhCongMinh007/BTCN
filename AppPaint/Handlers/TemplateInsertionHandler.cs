using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Windows.Foundation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using AppPaint.Services;

namespace AppPaint.Handlers;

/// <summary>
/// Handles template insertion operations on canvas
/// </summary>
public class TemplateInsertionHandler
{
    private readonly IServiceProvider _serviceProvider;

    public TemplateInsertionHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Insert a template's shapes at the specified position on the canvas
    /// </summary>
    public async Task<(bool success, int shapeCount, string error)> InsertTemplateAtPosition(
        int templateId, 
        Point position, 
        int? currentTemplateId,
        Func<Data.Models.Shape, Task> addShapeCallback)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
         var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();

            // Load template with shapes
 var template = await templateService.GetTemplateByIdAsync(templateId);
            if (template == null || template.Shapes == null || !template.Shapes.Any())
            {
   return (false, 0, "Template is empty or not found.");
 }

    System.Diagnostics.Debug.WriteLine($"📦 Inserting template '{template.Name}' with {template.Shapes.Count} shapes");

      // Calculate template bounds
       var points = template.Shapes
      .SelectMany(s => DrawingService.JsonToPoints(s.PointsData))
     .ToList();

          if (!points.Any())
            {
        return (false, 0, "Template has no valid shapes.");
        }

    double minX = points.Min(p => p.X);
       double minY = points.Min(p => p.Y);
     double maxX = points.Max(p => p.X);
       double maxY = points.Max(p => p.Y);

   double templateWidth = maxX - minX;
      double templateHeight = maxY - minY;

  // Calculate offset to position template center at drop point
  double offsetX = position.X - (minX + templateWidth / 2);
  double offsetY = position.Y - (minY + templateHeight / 2);

       System.Diagnostics.Debug.WriteLine($"📐 Template bounds: ({minX}, {minY}) to ({maxX}, {maxY})");
      System.Diagnostics.Debug.WriteLine($"📍 Offset: ({offsetX}, {offsetY})");

            // Insert shapes with offset
   int insertedCount = 0;
            foreach (var templateShape in template.Shapes)
            {
var shapePoints = DrawingService.JsonToPoints(templateShape.PointsData);

          // Apply offset to all points
       var offsetPoints = shapePoints.Select(p => new Point(p.X + offsetX, p.Y + offsetY)).ToList();

        // Create new shape with offset points
     var newShape = new Data.Models.Shape
        {
      ShapeType = templateShape.ShapeType,
         PointsData = DrawingService.PointsToJson(offsetPoints),
Color = templateShape.Color,
                 StrokeThickness = templateShape.StrokeThickness,
     StrokeStyle = templateShape.StrokeStyle,
           IsFilled = templateShape.IsFilled,
            FillColor = templateShape.FillColor,
             TemplateId = currentTemplateId,
       CreatedAt = DateTime.Now
                };

    // Add to database and UI
    await addShapeCallback(newShape);
  insertedCount++;
       }

            System.Diagnostics.Debug.WriteLine($"✅ Inserted {insertedCount} shapes from template '{template.Name}'");
    return (true, insertedCount, string.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error inserting template: {ex.Message}");
            return (false, 0, ex.Message);
        }
    }
}
