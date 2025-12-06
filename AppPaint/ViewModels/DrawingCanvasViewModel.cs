using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppPaint.Services;
using Data.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI;
using Windows.UI;
using Microsoft.Extensions.DependencyInjection;

namespace AppPaint.ViewModels;

public partial class DrawingCanvasViewModel : BaseViewModel
{
    [ObservableProperty]
 private ObservableCollection<Shape> _shapes = new();

    [ObservableProperty]
    private ShapeType _selectedShapeType = ShapeType.Line;

    [ObservableProperty]
    private string _selectedColor = "#000000";

    [ObservableProperty]
    private string _backgroundColor = "#FFFFFF";

 [ObservableProperty]
    private double _strokeThickness = 2.0;

[ObservableProperty]
    private bool _isFilled;

    [ObservableProperty]
    private string _strokeStyle = "Solid"; // New: Solid, Dash, Dot, DashDot

  [ObservableProperty]
 private double _canvasWidth = 800;

 [ObservableProperty]
    private double _canvasHeight = 600;

    [ObservableProperty]
 private Shape? _selectedShape;

    [ObservableProperty]
    private string _templateName = "New Drawing";

 [ObservableProperty]
    private int? _currentTemplateId;

    // Navigation events
    public event EventHandler? NavigateBackRequested;
    public event EventHandler? ClearCanvasRequested;

    public DrawingCanvasViewModel()
    {
   Title = "Drawing Canvas";
        
        // Set default color
   SelectedColor = "#000000";
    }

    public override async void OnNavigatedTo(object? parameter = null)
  {
 base.OnNavigatedTo(parameter);

        if (parameter is int templateId)
  {
       await LoadTemplateAsync(templateId);
  }
 }

    [RelayCommand]
    private async Task LoadTemplateAsync(int templateId)
    {
   try
        {
      IsBusy = true;
   
        using var scope = App.Services.CreateScope();
     var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
        
   var template = await templateService.GetTemplateByIdAsync(templateId);
   if (template != null)
   {
   CurrentTemplateId = template.Id;
    TemplateName = template.Name;
       CanvasWidth = template.Width;
    CanvasHeight = template.Height;
    BackgroundColor = template.BackgroundColor;

  Shapes.Clear();
   foreach (var shape in template.Shapes)
       {
        Shapes.Add(shape);
    }
   }
}
   catch (Exception ex)
   {
ErrorMessage = $"Error loading template: {ex.Message}";
   }
   finally
{
  IsBusy = false;
   }
    }

    [RelayCommand]
    private async Task SaveAsTemplateAsync()
    {
   try
   {
 IsBusy = true;

   using var scope = App.Services.CreateScope();
    var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
   var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();

 var template = new DrawingTemplate
   {
Name = TemplateName,
     Width = CanvasWidth,
  Height = CanvasHeight,
     BackgroundColor = BackgroundColor
   };

   var savedTemplate = await templateService.CreateTemplateAsync(template);
   CurrentTemplateId = savedTemplate.Id;

   // Update all shapes to belong to this template
   foreach (var shape in Shapes)
  {
    shape.TemplateId = savedTemplate.Id;
    await shapeService.UpdateShapeAsync(shape);
      }
}
   catch (Exception ex)
    {
   ErrorMessage = $"Error saving template: {ex.Message}";
    }
   finally
   {
      IsBusy = false;
   }
    }

    [RelayCommand]
    private async Task AddShapeAsync(Shape shape)
    {
  try
        {
        using var scope = App.Services.CreateScope();
        var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();

     shape.TemplateId = CurrentTemplateId;
       var savedShape = await shapeService.CreateShapeAsync(shape);
       Shapes.Add(savedShape);
        }
   catch (Exception ex)
   {
   ErrorMessage = $"Error adding shape: {ex.Message}";
        }
}

    [RelayCommand]
 private async Task DeleteSelectedShapeAsync()
{
   if (SelectedShape == null) return;

   try
   {
        using var scope = App.Services.CreateScope();
 var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();
        
   var success = await shapeService.DeleteShapeAsync(SelectedShape.Id);
   if (success)
   {
   Shapes.Remove(SelectedShape);
    SelectedShape = null;
 }
 }
   catch (Exception ex)
        {
   ErrorMessage = $"Error deleting shape: {ex.Message}";
   }
    }

    [RelayCommand]
    private void ClearCanvas()
{
   Shapes.Clear();
CurrentTemplateId = null;
   TemplateName = "New Drawing";
        
        // Raise event to clear UI canvas
  ClearCanvasRequested?.Invoke(this, EventArgs.Empty);
    }

 [RelayCommand]
    private void SelectShape(ShapeType shapeType)
    {
   SelectedShapeType = shapeType;
 }

    [RelayCommand]
 private void GoBack()
    {
   NavigateBackRequested?.Invoke(this, EventArgs.Empty);
    }
}
