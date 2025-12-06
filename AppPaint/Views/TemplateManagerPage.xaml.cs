using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using AppPaint.ViewModels;
using AppPaint.Services;
using Microsoft.Extensions.DependencyInjection;
using Data.Models;

namespace AppPaint.Views;

public sealed partial class TemplateManagerPage : Page
{
    public TemplateManagerViewModel ViewModel { get; }

    public TemplateManagerPage()
    {
        ViewModel = App.Services.GetRequiredService<TemplateManagerViewModel>();
        this.DataContext = ViewModel;
 
        // Subscribe to events
    ViewModel.LoadTemplateRequested += OnLoadTemplateRequested;
  ViewModel.NavigateBackRequested += OnNavigateBackRequested;
 
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
     // Pass true to load TEMPLATES (IsTemplate = true), not drawings!
        ViewModel.OnNavigatedTo(true);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
    
      // Unsubscribe
        ViewModel.LoadTemplateRequested -= OnLoadTemplateRequested;
        ViewModel.NavigateBackRequested -= OnNavigateBackRequested;
 
        ViewModel.OnNavigatedFrom();
    }

    private void OnLoadTemplateRequested(object? sender, int templateId)
    {
        // Navigate to DrawingCanvas with template ID
        Frame.Navigate(typeof(DrawingCanvasPage), templateId);
    }

    private void OnNavigateBackRequested(object? sender, System.EventArgs e)
    {
        if (Frame.CanGoBack)
        {
      Frame.GoBack();
        }
    }

    private void PreviewCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas && canvas.Tag is DrawingTemplate template)
  {
      RenderTemplatePreview(canvas, template);
     }
    }

    private void RenderTemplatePreview(Canvas canvas, DrawingTemplate template)
    {
        try
        {
            canvas.Children.Clear();

   if (template.Shapes == null || !template.Shapes.Any())
                return;

      // Calculate bounds to fit preview
  double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var shape in template.Shapes)
        {
   var points = DrawingService.JsonToPoints(shape.PointsData);
                foreach (var pt in points)
            {
   minX = Math.Min(minX, pt.X);
        minY = Math.Min(minY, pt.Y);
          maxX = Math.Max(maxX, pt.X);
      maxY = Math.Max(maxY, pt.Y);
         }
  }

       double shapeWidth = maxX - minX;
            double shapeHeight = maxY - minY;
            
        // Calculate scale to fit 200x100 preview
            double scaleX = shapeWidth > 0 ? (canvas.Width - 20) / shapeWidth : 1;
   double scaleY = shapeHeight > 0 ? (canvas.Height - 20) / shapeHeight : 1;
    double scale = Math.Min(scaleX, scaleY);

 // Render each shape scaled and centered
      foreach (var shape in template.Shapes)
            {
        var points = DrawingService.JsonToPoints(shape.PointsData);
 if (points.Count < 2) continue;

                // Scale and translate points
    var scaledPoints = points.Select(pt => new Windows.Foundation.Point(
         (pt.X - minX) * scale + 10,
          (pt.Y - minY) * scale + 10
    )).ToList();

    Microsoft.UI.Xaml.Shapes.Shape? uiShape = shape.ShapeType switch
        {
         ShapeType.Line when scaledPoints.Count >= 2 => 
          DrawingService.CreateLine(scaledPoints[0], scaledPoints[1], 
         shape.Color, shape.StrokeThickness * scale, shape.StrokeStyle),
         ShapeType.Rectangle when scaledPoints.Count >= 2 => 
    DrawingService.CreateRectangle(scaledPoints[0], scaledPoints[1], 
     shape.Color, shape.StrokeThickness * scale, shape.IsFilled, shape.StrokeStyle, shape.FillColor),
  ShapeType.Circle when scaledPoints.Count >= 2 => 
         DrawingService.CreateEllipse(scaledPoints[0], scaledPoints[1], 
    shape.Color, shape.StrokeThickness * scale, true, shape.IsFilled, shape.StrokeStyle, shape.FillColor),
                ShapeType.Oval when scaledPoints.Count >= 2 => 
    DrawingService.CreateEllipse(scaledPoints[0], scaledPoints[1], 
   shape.Color, shape.StrokeThickness * scale, false, shape.IsFilled, shape.StrokeStyle, shape.FillColor),
        ShapeType.Triangle when scaledPoints.Count >= 2 => 
       DrawingService.CreateTriangle(scaledPoints[0], scaledPoints[1], 
       shape.Color, shape.StrokeThickness * scale, shape.IsFilled, shape.StrokeStyle, shape.FillColor),
          ShapeType.Polygon when scaledPoints.Count >= 3 => 
               DrawingService.CreatePolygon(scaledPoints, 
          shape.Color, shape.StrokeThickness * scale, shape.IsFilled, shape.StrokeStyle, shape.FillColor),
       _ => null
      };

       if (uiShape != null)
    {
           canvas.Children.Add(uiShape);
          }
   }
        }
        catch (Exception ex)
 {
            System.Diagnostics.Debug.WriteLine($"❌ Error rendering preview: {ex.Message}");
        }
    }

    private async void InsertTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DrawingTemplate template)
   {
            var dialog = new ContentDialog
     {
     XamlRoot = this.XamlRoot,
         Title = "Insert Template",
          Content = $"This will insert '{template.Name}' ({template.Shapes.Count} shapes) to the active canvas.\n\n" +
             "Note: You must have an active drawing canvas open.",
   PrimaryButtonText = "OK",
       CloseButtonText = "Cancel"
       };

     var result = await dialog.ShowAsync();
   
            if (result == ContentDialogResult.Primary)
        {
   System.Diagnostics.Debug.WriteLine($"Insert template: {template.Name} with {template.Shapes.Count} shapes");
    }
        }
    }

    private async void DeleteTemplate_Click(object sender, RoutedEventArgs e)
    {
    if (sender is Button button && button.Tag is DrawingTemplate template)
        {
        await ShowDeleteConfirmationAndDelete(template);
        }
    }

    private async Task ShowDeleteConfirmationAndDelete(DrawingTemplate template)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
         Title = "Delete Template?",
     Content = $"Are you sure you want to delete template '{template.Name}'?\n\n" +
           $"This will delete {template.Shapes.Count} shape(s).\n\n" +
              $"This action cannot be undone.",
          PrimaryButtonText = "Delete",
   CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
     {
            await ViewModel.DeleteTemplateCommand.ExecuteAsync(template);

var successDialog = new ContentDialog
            {
    XamlRoot = this.XamlRoot,
     Title = "Deleted",
      Content = $"Template '{template.Name}' has been deleted successfully.",
              CloseButtonText = "OK"
            };
            await successDialog.ShowAsync();
  }
    }
}
