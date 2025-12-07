using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using AppPaint.ViewModels;
using AppPaint.Services;
using AppPaint.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using UIShape = Microsoft.UI.Xaml.Shapes.Shape;

namespace AppPaint.Views;

public sealed partial class DrawingCanvasPage : Page
{
    public DrawingCanvasViewModel ViewModel { get; }

    // Handlers for modular functionality
    private readonly ShapeCreationHandler _creationHandler;
    private readonly ShapeSelectionHandler _selectionHandler;
    private readonly ShapeEditHandler _editHandler;
    private readonly ShapeRenderingService _renderingService;
    private readonly TemplateSaveService _saveService;
    private readonly CanvasEventHandler _canvasEventHandler;
    private readonly UIEventHandler _uiEventHandler;
    private readonly TemplateUIHandler _templateUIHandler;
    private readonly TemplateInsertionHandler _insertionHandler;

    private bool _isToolbarExpanded = true;

    public DrawingCanvasPage()
    {
        ViewModel = App.Services.GetRequiredService<DrawingCanvasViewModel>();
        this.DataContext = ViewModel;

        // Initialize handlers
        var drawingService = App.Services.GetRequiredService<DrawingService>();
        _creationHandler = new ShapeCreationHandler(drawingService);
        _selectionHandler = new ShapeSelectionHandler();
        _editHandler = new ShapeEditHandler();
        _renderingService = new ShapeRenderingService(drawingService);
        _saveService = new TemplateSaveService(App.Services, _renderingService);
        _insertionHandler = new TemplateInsertionHandler(App.Services);
        
        // Initialize event handlers
    _canvasEventHandler = new CanvasEventHandler(
  _creationHandler, 
     _selectionHandler, 
   _editHandler, 
            _insertionHandler,
       ViewModel);
        _uiEventHandler = new UIEventHandler(ViewModel, _selectionHandler, _editHandler, _creationHandler);
        _templateUIHandler = new TemplateUIHandler();

        // Subscribe to handler events
      _creationHandler.PolygonFinished += OnPolygonFinished;
        _selectionHandler.ShapeSelected += OnShapeSelected;
        _selectionHandler.SelectionCleared += OnSelectionCleared;
     _selectionHandler.ResizeHandlePressed += OnResizeHandlePressed;

      // Subscribe to ViewModel events
        ViewModel.NavigateBackRequested += OnNavigateBackRequested;
ViewModel.ClearCanvasRequested += OnClearCanvasRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        this.InitializeComponent();

        // Setup canvas events
        DrawingCanvas.PointerPressed += DrawingCanvas_PointerPressed;
        DrawingCanvas.PointerMoved += DrawingCanvas_PointerMoved;
        DrawingCanvas.PointerReleased += DrawingCanvas_PointerReleased;

        // Keyboard events
      this.KeyDown += DrawingCanvasPage_KeyDown;
   this.KeyUp += DrawingCanvasPage_KeyUp;

        // UI events
        StrokeThicknessSlider.ValueChanged += StrokeThicknessSlider_ValueChanged;
    }

    #region Navigation

    protected override void OnNavigatedTo(NavigationEventArgs e)
{
        System.Diagnostics.Debug.WriteLine($"📍 DrawingCanvasPage.OnNavigatedTo");

        base.OnNavigatedTo(e);
        ViewModel.OnNavigatedTo(e.Parameter);

        // Subscribe to shape collection changes
     ViewModel.Shapes.CollectionChanged += Shapes_CollectionChanged;
        ViewModel.PropertyChanged += ViewModel_BackgroundChanged;

        // Apply background and render shapes
        ApplyBackgroundColor();
   _renderingService.RenderAllShapes(ViewModel.Shapes, DrawingCanvas);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

     // Unsubscribe
     ViewModel.Shapes.CollectionChanged -= Shapes_CollectionChanged;
        ViewModel.PropertyChanged -= ViewModel_BackgroundChanged;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.NavigateBackRequested -= OnNavigateBackRequested;
        ViewModel.ClearCanvasRequested -= OnClearCanvasRequested;

        ViewModel.OnNavigatedFrom();
 }

    private void OnNavigateBackRequested(object? sender, EventArgs e)
    {
        if (Frame.CanGoBack)
  {
            Frame.GoBack();
        }
    }

    #endregion

    #region Shape Collection Management

    private void Shapes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
     {
            foreach (Data.Models.Shape shape in e.NewItems)
     {
     _renderingService.RenderShape(shape, DrawingCanvas);
      }
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            _renderingService.RenderAllShapes(ViewModel.Shapes, DrawingCanvas);
        }
    }

    #endregion

    #region Canvas Events - Drawing & Interaction

    private void DrawingCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _canvasEventHandler.HandlePointerPressed(DrawingCanvas, e, () => 
        {
   FinishPolygonButton.Visibility = Visibility.Visible;
 });
    }

    private void DrawingCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        _canvasEventHandler.HandlePointerMoved(DrawingCanvas, e);
    }

    private void DrawingCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var result = _canvasEventHandler.HandlePointerReleased(DrawingCanvas, e);
        
    if (result.shouldSave)
        {
  SaveShapeToDatabase(result.start, result.end, result.shapeType);
  }
    }

    #endregion

    #region Keyboard Events

  private void DrawingCanvasPage_KeyDown(object sender, KeyRoutedEventArgs e)
{
        _uiEventHandler.HandleKeyDown(e.Key, DrawingCanvas);
    }

    private void DrawingCanvasPage_KeyUp(object sender, KeyRoutedEventArgs e)
    {
   _uiEventHandler.HandleKeyUp(e.Key);
    }

    #endregion

    #region UI Event Handlers

    private void ShapeButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is ToggleButton button && button.Tag is string tag)
      {
            _uiEventHandler.HandleShapeButtonClick(
     button, tag,
             SelectButton, LineButton, RectangleButton, 
     OvalButton, CircleButton, TriangleButton, PolygonButton);
 
            _canvasEventHandler.IsSelectMode = false;
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
if (sender is ToggleButton button)
        {
  _uiEventHandler.HandleSelectButtonClick(
         button,
       LineButton, RectangleButton, OvalButton, 
 CircleButton, TriangleButton, PolygonButton);
    
   _canvasEventHandler.IsSelectMode = true;
        }
    }

    private void StrokeColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        _uiEventHandler.HandleStrokeColorChanged(args.NewColor, StrokeColorPreview);
    }

    private void FillColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        _uiEventHandler.HandleFillColorChanged(args.NewColor, FillColorPreview);
}

    private void StrokeThicknessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
{
        _uiEventHandler.HandleStrokeThicknessChanged(e.NewValue);
    }

 private void StrokeStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
 if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            var style = item.Tag?.ToString() ?? "Solid";
_uiEventHandler.HandleStrokeStyleChanged(style);
     }
    }

    private void ToggleToolbarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Content is FontIcon icon)
     {
       _isToolbarExpanded = _uiEventHandler.HandleToolbarToggle(_isToolbarExpanded, ToolbarContent, icon);
        }
    }

    private async void FinishPolygonButton_Click(object sender, RoutedEventArgs e)
    {
        await _uiEventHandler.HandleFinishPolygonAsync(DrawingCanvas, () => 
     {
     FinishPolygonButton.Visibility = Visibility.Collapsed;
});
    }

    #endregion

    #region Shape Management

    private void OnPolygonFinished(object? sender, EventArgs e)
    {
  FinishPolygonButton.Visibility = Visibility.Collapsed;
    }

    private void OnShapeSelected(object? sender, ShapeSelectedEventArgs e)
    {
        var props = _selectionHandler.GetSelectedShapeProperties();
if (props != null)
        {
            ViewModel.SelectedColor = props.StrokeColor;
     ViewModel.StrokeThickness = props.StrokeThickness;
         ViewModel.IsFilled = props.IsFilled;
            if (props.IsFilled)
  {
      ViewModel.FillColor = props.FillColor;
            }
        }
    }

 private void OnSelectionCleared(object? sender, EventArgs e)
    {
        // Reset toolbar to defaults if needed
    }

    private void OnResizeHandlePressed(object? sender, PointerRoutedEventArgs e)
    {
      if (_selectionHandler.SelectedShape != null)
        {
            var point = e.GetCurrentPoint(DrawingCanvas).Position;
    _editHandler.StartResizing(point, DrawingCanvas, e);
          e.Handled = true;
        }
    }

  private async void SaveShapeToDatabase(Point start, Point end, ShapeType shapeType)
    {
      var points = new System.Collections.Generic.List<Point> { start, end };
   var pointsJson = DrawingService.PointsToJson(points);

    var shape = new Data.Models.Shape
        {
        ShapeType = shapeType,
      PointsData = pointsJson,
  Color = ViewModel.SelectedColor,
       StrokeThickness = ViewModel.StrokeThickness,
         IsFilled = ViewModel.IsFilled,
   FillColor = ViewModel.IsFilled ? ViewModel.FillColor : null,
            TemplateId = ViewModel.CurrentTemplateId,
CreatedAt = DateTime.Now
        };

      await ViewModel.AddShapeCommand.ExecuteAsync(shape);
    }

  #endregion

    #region Save & Template Management

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        int canvasShapeCount = _saveService.GetCanvasShapeCount(DrawingCanvas);
        bool isUpdating = ViewModel.CurrentTemplateId.HasValue;

        string dialogTitle = isUpdating ? "Update Drawing" : "Save Drawing";
    string dialogMessage = isUpdating
            ? $"Update '{ViewModel.TemplateName}'?"
   : "Enter a name for your drawing:";

        var dialog = new ContentDialog
        {
  XamlRoot = this.XamlRoot,
  Title = dialogTitle,
        PrimaryButtonText = isUpdating ? "Update" : "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

   var stackPanel = new StackPanel { Spacing = 12 };
      stackPanel.Children.Add(new TextBlock
        {
            Text = dialogMessage,
         FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        TextBox? nameTextBox = null;
        if (!isUpdating)
        {
          nameTextBox = new TextBox
     {
 PlaceholderText = "My Drawing",
        Text = ViewModel.TemplateName == "New Drawing"
     ? $"Drawing {DateTime.Now:yyyy-MM-dd HH-mm}"
     : ViewModel.TemplateName,
           MaxLength = 200
            };
     stackPanel.Children.Add(nameTextBox);
        }
 else
        {
 stackPanel.Children.Add(new TextBlock
          {
      Text = $"Name: {ViewModel.TemplateName}",
    Opacity = 0.7
   });
        }

 stackPanel.Children.Add(new TextBlock
        {
  Text = $"Canvas Size: {ViewModel.CanvasWidth}x{ViewModel.CanvasHeight}",
            FontSize = 12,
          Opacity = 0.7
        });

     stackPanel.Children.Add(new TextBlock
        {
  Text = $"Shapes on canvas: {canvasShapeCount}",
 FontSize = 12,
Opacity = 0.7
        });

        if (isUpdating)
        {
       stackPanel.Children.Add(new TextBlock
   {
      Text = "⚠️ This will overwrite the existing drawing.",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange),
    FontSize = 12,
          FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
     });
        }

        dialog.Content = stackPanel;
 var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            string name = ViewModel.TemplateName;

            if (!isUpdating && nameTextBox != null)
   {
          name = nameTextBox.Text.Trim();
    if (string.IsNullOrEmpty(name))
            {
            await ShowErrorDialog("Please enter a name for your drawing.");
        return;
   }
         ViewModel.TemplateName = name;
     }

            await SaveTemplateWithCanvasShapes(name, canvasShapeCount, isUpdating);
     }
    }

    private async Task SaveTemplateWithCanvasShapes(string name, int shapeCount, bool isUpdating)
    {
        try
        {
    ViewModel.IsBusy = true;

            var template = await _saveService.SaveTemplateWithShapes(
     ViewModel.CurrentTemplateId,
     name,
     (int)ViewModel.CanvasWidth,
      (int)ViewModel.CanvasHeight,
      ViewModel.BackgroundColor,
      DrawingCanvas
            );

            if (template != null)
       {
                ViewModel.CurrentTemplateId = template.Id;

     string successMessage = isUpdating
           ? $"'{name}' updated successfully with {shapeCount} shapes!"
         : $"'{name}' saved successfully with {shapeCount} shapes!";

                await ShowSuccessDialog(successMessage);
        }
     }
        catch (Exception ex)
        {
  ViewModel.ErrorMessage = $"Error saving: {ex.Message}";
        await ShowErrorDialog($"Failed to save drawing:\n{ex.Message}");
        }
        finally
        {
        ViewModel.IsBusy = false;
        }
    }

    private async void SaveAsTemplateButton_Click(object sender, RoutedEventArgs e)
    {
 if (_selectionHandler.SelectedShape == null)
 {
            await ShowErrorDialog("Please select a shape first (click the Select button, then click on a shape).");
     return;
  }

        var dialog = new ContentDialog
 {
         XamlRoot = this.XamlRoot,
            Title = "Save as Template",
 PrimaryButtonText = "Save",
      CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

   var stackPanel = new StackPanel { Spacing = 12 };
 stackPanel.Children.Add(new TextBlock
     {
            Text = "Template Name:",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
  });

        var nameTextBox = new TextBox
        {
            PlaceholderText = "Star Shape",
            MaxLength = 200,
            Text = $"{_selectionHandler.SelectedShape.GetType().Name} Template"
        };
        stackPanel.Children.Add(nameTextBox);

 stackPanel.Children.Add(new TextBlock
        {
      Text = "This template can be quickly inserted into any drawing.",
         FontSize = 12,
   Opacity = 0.7,
         TextWrapping = TextWrapping.Wrap
        });

      dialog.Content = stackPanel;
        var result = await dialog.ShowAsync();

   if (result == ContentDialogResult.Primary)
      {
     var templateName = nameTextBox.Text.Trim();
  if (string.IsNullOrEmpty(templateName))
     {
          await ShowErrorDialog("Please enter a template name.");
        return;
  }

            await SaveShapeAsTemplate(templateName);
     }
    }

    private async Task SaveShapeAsTemplate(string templateName)
    {
        if (_selectionHandler.SelectedShape == null) return;

 try
   {
            ViewModel.IsBusy = true;

          var template = await _saveService.SaveShapeAsTemplate(templateName, _selectionHandler.SelectedShape);

  if (template != null)
      {
       await ShowSuccessDialog($"'{templateName}' saved successfully!\n\nYou can now find it in Management > Templates.");
                _selectionHandler.ClearSelection(DrawingCanvas);
      }
        }
      catch (Exception ex)
        {
      await ShowErrorDialog($"Failed to save template:\n{ex.Message}");
        }
        finally
      {
      ViewModel.IsBusy = false;
        }
    }

    #endregion

    #region ViewModel & Background

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsFilled) && _selectionHandler.SelectedShape != null && _uiEventHandler.IsSelectMode)
    {
        _editHandler.UpdateShapeFillColor(_selectionHandler.SelectedShape, ViewModel.FillColor, ViewModel.IsFilled);
        }
    }

    private void ViewModel_BackgroundChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
if (e.PropertyName == nameof(ViewModel.BackgroundColor))
      {
        ApplyBackgroundColor();
        }
    }

    private void ApplyBackgroundColor()
    {
      try
        {
     var color = DrawingService.ParseColor(ViewModel.BackgroundColor);
            CanvasBackgroundBrush.Color = color;
     BackgroundColorPreview.Color = color;
        }
    catch (Exception ex)
        {
       System.Diagnostics.Debug.WriteLine($"❌ Error applying background color: {ex.Message}");
      }
    }

    private void OnClearCanvasRequested(object? sender, EventArgs e)
    {
        DrawingCanvas.Children.Clear();
        _creationHandler.ClearPolygon(DrawingCanvas);
  FinishPolygonButton.Visibility = Visibility.Collapsed;
        _selectionHandler.ClearSelection(DrawingCanvas);
    }

    #endregion

    #region Helper Methods

    private async Task ShowErrorDialog(string message)
{
    var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Error",
     Content = message,
    CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

    private async Task ShowSuccessDialog(string message)
    {
   var dialog = new ContentDialog
        {
     XamlRoot = this.XamlRoot,
   Title = "Success",
            Content = message,
   CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

  #endregion

    #region Template Panel

    private void ToggleTemplatePanel_Click(object sender, RoutedEventArgs e)
    {
        _templateUIHandler.TogglePanel(TemplatePanel, ShowTemplatePanelButton);
    }

    private void TemplatePreviewCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas && canvas.Tag is DrawingTemplate template)
        {
   _renderingService.RenderShapesWithAutoFit(template.Shapes, canvas, padding: 10);
   }
    }

    private void Template_DragStarting(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {
        if (sender is Border border && border.DataContext is DrawingTemplate template)
        {
 _templateUIHandler.HandleDragStarting(template, args);
      }
    }

    private void DrawingCanvas_DragOver(object sender, DragEventArgs e)
    {
        _canvasEventHandler.HandleDragOver(e);
    }

    private async void DrawingCanvas_Drop(object sender, DragEventArgs e)
  {
        ViewModel.IsBusy = true;
        
        var result = await _canvasEventHandler.HandleDropAsync(DrawingCanvas, e);
        
        if (!result.success && !string.IsNullOrEmpty(result.error))
        {
     await ShowErrorDialog($"Failed to insert template: {result.error}");
     }
 
        ViewModel.IsBusy = false;
    }

    private async void InsertTemplateButton_Click(object sender, RoutedEventArgs e)
  {
    if (sender is Button button && button.Tag is DrawingTemplate template)
        {
     ViewModel.IsBusy = true;
            
            var result = await _canvasEventHandler.InsertTemplateAtCenterAsync(template.Id);
      
   if (!result.success && !string.IsNullOrEmpty(result.error))
  {
                await ShowErrorDialog($"Failed to insert template: {result.error}");
       }
        
     ViewModel.IsBusy = false;
 }
    }

    private async void TemplatePreview_Click(object sender, RoutedEventArgs e)
    {
   if (sender is Button button && button.Tag is DrawingTemplate template)
 {
            ViewModel.IsBusy = true;
            
            var result = await _canvasEventHandler.InsertTemplateAtCenterAsync(template.Id);

  if (!result.success && !string.IsNullOrEmpty(result.error))
   {
      await ShowErrorDialog($"Failed to insert template: {result.error}");
         }
   
            ViewModel.IsBusy = false;
        }
 }

    private void TemplateCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
{
        if (sender is Border border)
        {
    _templateUIHandler.HandleCardPointerEntered(border);
        }
    }

    private void TemplateCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
        _templateUIHandler.HandleCardPointerExited(border);
        }
    }

    #endregion
}
