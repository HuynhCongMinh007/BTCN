using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using AppPaint.Services;
using Data.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using UIShape = Microsoft.UI.Xaml.Shapes.Shape;

namespace AppPaint.Handlers;

/// <summary>
/// Handles saving and updating drawing templates
/// </summary>
public class TemplateSaveService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ShapeRenderingService _renderingService;

    public TemplateSaveService(IServiceProvider serviceProvider, ShapeRenderingService renderingService)
    {
        _serviceProvider = serviceProvider;
        _renderingService = renderingService;
    }

    /// <summary>
    /// Save or update template with all shapes from canvas
    /// </summary>
    public async Task<DrawingTemplate?> SaveTemplateWithShapes(
     int? currentTemplateId,
  string templateName,
     int canvasWidth,
        int canvasHeight,
    string backgroundColor,
        Canvas canvas)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
            var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();

            DrawingTemplate template;
            bool isUpdating = currentTemplateId.HasValue;

            if (isUpdating)
            {
                // UPDATE existing template
                System.Diagnostics.Debug.WriteLine($"📝 Updating existing drawing ID: {currentTemplateId.Value}");

                var existingTemplate = await templateService.GetTemplateByIdAsync(currentTemplateId.Value);
                if (existingTemplate != null)
                {
                    existingTemplate.Name = templateName;
                    existingTemplate.Width = canvasWidth;
                    existingTemplate.Height = canvasHeight;
                    existingTemplate.BackgroundColor = backgroundColor;
                    existingTemplate.ModifiedAt = DateTime.Now;

                    template = await templateService.UpdateTemplateAsync(existingTemplate);
                    System.Diagnostics.Debug.WriteLine($"✅ Updated template: {template.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Template {currentTemplateId.Value} not found, creating new");
                    template = await CreateNewTemplate(templateService, templateName, canvasWidth, canvasHeight, backgroundColor, false);
                    isUpdating = false;
                }
            }
            else
            {
                // CREATE new template
                System.Diagnostics.Debug.WriteLine($"📝 Creating new drawing: {templateName}");
                template = await CreateNewTemplate(templateService, templateName, canvasWidth, canvasHeight, backgroundColor, false);
            }

            // Clear old shapes if updating
            if (isUpdating && template.Shapes != null && template.Shapes.Any())
            {
                System.Diagnostics.Debug.WriteLine($"🗑️ Clearing {template.Shapes.Count} old shapes");
                foreach (var oldShape in template.Shapes.ToList())
                {
                    await shapeService.DeleteShapeAsync(oldShape.Id);
                }
            }

            // Save all shapes from canvas
            var canvasShapes = canvas.Children.OfType<UIShape>().ToList();
            System.Diagnostics.Debug.WriteLine($"💾 Saving {canvasShapes.Count} shapes to database");

            int savedCount = 0;
            foreach (var uiShape in canvasShapes)
            {
                var shape = _renderingService.ConvertToDataModel(uiShape, template.Id);
                if (shape != null)
                {
                    await shapeService.CreateShapeAsync(shape);
                    savedCount++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"✅ {(isUpdating ? "Updated" : "Saved")} template '{template.Name}' with {savedCount} shapes");
            return template;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Save error: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Save a single shape as a reusable template
    /// </summary>
    public async Task<DrawingTemplate?> SaveShapeAsTemplate(string templateName, UIShape shape)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
            var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();

            // Create template with IsTemplate = true (reusable template)
            var template = await CreateNewTemplate(templateService, templateName, 200, 200, "#FFFFFF", true);
            System.Diagnostics.Debug.WriteLine($"✅ Created template: {templateName} (ID: {template.Id})");

            // Convert UI shape to data model and save
            var shapeData = _renderingService.ConvertToDataModel(shape, template.Id);
            if (shapeData != null)
            {
                await shapeService.CreateShapeAsync(shapeData);
                System.Diagnostics.Debug.WriteLine($"✅ Saved shape to template");
            }

            return template;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error saving template: {ex}");
            throw;
        }
    }

    private async Task<DrawingTemplate> CreateNewTemplate(
           ITemplateService templateService,
       string name,
           int width,
         int height,
           string backgroundColor,
           bool isTemplate)
    {
        var template = new DrawingTemplate
        {
            Name = name,
            Width = width,
            Height = height,
            BackgroundColor = backgroundColor,
            IsTemplate = isTemplate
        };

        var savedTemplate = await templateService.CreateTemplateAsync(template);
        System.Diagnostics.Debug.WriteLine($"✅ Created {(isTemplate ? "TEMPLATE" : "DRAWING")}: {savedTemplate.Name} (ID: {savedTemplate.Id})");

        return savedTemplate;
    }

    /// <summary>
    /// Get count of shapes on canvas
    /// </summary>
    public int GetCanvasShapeCount(Canvas canvas)
    {
        return canvas.Children.OfType<UIShape>().Count();
    }
}
