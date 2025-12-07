using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
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

    private bool _isToolbarExpanded = true;
    private bool _isSelectMode = false;

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
        var point = e.GetCurrentPoint(DrawingCanvas).Position;

        // Selection mode
        if (_isSelectMode)
        {
            // Check if we're clicking on the currently selected shape
            bool clickedOnSelectedShape = _selectionHandler.SelectedShape != null &&
                 _selectionHandler.SelectedShape is UIShape selectedShape &&
              IsPointInSelectedShape(selectedShape, point);

            if (clickedOnSelectedShape &&
        !_editHandler.IsDragging &&
        !_editHandler.IsResizing)
            {
                // Start dragging the selected shape
                _editHandler.StartDragging(_selectionHandler.SelectedShape!, point, DrawingCanvas, e);
                e.Handled = true;
            }
            else
            {
                // Try to select a shape at this point (or clear selection if clicking empty area)
                _selectionHandler.SelectShapeAtPoint(point, DrawingCanvas);
            }
            return;
        }

        // Drawing mode
        _creationHandler.StartDrawing(point, ViewModel.SelectedShapeType, DrawingCanvas);

        // Show finish button for polygon
        if (ViewModel.SelectedShapeType == ShapeType.Polygon && _creationHandler.PolygonPoints.Count > 0)
        {
            FinishPolygonButton.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Helper method to check if a point is within the selected shape
    /// </summary>
    private bool IsPointInSelectedShape(UIShape shape, Point point)
    {
        double left = Canvas.GetLeft(shape);
        double top = Canvas.GetTop(shape);

        if (shape is Microsoft.UI.Xaml.Shapes.Line line)
        {
            // Line hit test with tolerance
            double tolerance = 5;
            return IsPointNearLine(point, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2), tolerance);
        }
        else if (shape is Microsoft.UI.Xaml.Shapes.Rectangle rect)
        {
            return point.X >= left && point.X <= left + rect.Width &&
                   point.Y >= top && point.Y <= top + rect.Height;
        }
        else if (shape is Microsoft.UI.Xaml.Shapes.Ellipse ellipse)
        {
            double centerX = left + ellipse.Width / 2;
            double centerY = top + ellipse.Height / 2;
            double dx = (point.X - centerX) / (ellipse.Width / 2);
            double dy = (point.Y - centerY) / (ellipse.Height / 2);
            return (dx * dx + dy * dy) <= 1;
        }
        else if (shape is Microsoft.UI.Xaml.Shapes.Polygon polygon)
        {
            return IsPointInPolygon(point, polygon.Points);
        }

        return false;
    }

    private bool IsPointNearLine(Point point, Point lineStart, Point lineEnd, double tolerance)
    {
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;
        double lengthSquared = dx * dx + dy * dy;

        if (lengthSquared == 0)
            return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2)) <= tolerance;

        double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;
        t = Math.Max(0, Math.Min(1, t));

        double nearestX = lineStart.X + t * dx;
        double nearestY = lineStart.Y + t * dy;

        double distance = Math.Sqrt(Math.Pow(point.X - nearestX, 2) + Math.Pow(point.Y - nearestY, 2));
        return distance <= tolerance;
    }

    private bool IsPointInPolygon(Point point, Microsoft.UI.Xaml.Media.PointCollection points)
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

    private void DrawingCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(DrawingCanvas).Position;

        // Handle resizing
        if (_editHandler.IsResizing && _selectionHandler.SelectedShape != null)
        {
            _editHandler.ResizeShape(_selectionHandler.SelectedShape, currentPoint);
            _selectionHandler.UpdateSelectionBorder(DrawingCanvas);
            e.Handled = true;
            return;
        }

        // Handle dragging
        if (_editHandler.IsDragging && _selectionHandler.SelectedShape != null)
        {
            _editHandler.DragShape(_selectionHandler.SelectedShape, currentPoint, DrawingCanvas);
            _selectionHandler.UpdateSelectionBorder(DrawingCanvas);
            e.Handled = true;
            return;
        }

        // Handle preview during drawing
        if (_creationHandler.IsDrawing)
        {
            _creationHandler.UpdatePreview(currentPoint, ViewModel.SelectedShapeType,
   ViewModel.SelectedColor, ViewModel.StrokeThickness, ViewModel.IsFilled,
               ViewModel.FillColor, ViewModel.StrokeStyle, DrawingCanvas);
        }
    }

    private void DrawingCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Handle end of resizing
        if (_editHandler.IsResizing)
        {
            _editHandler.EndResizing(DrawingCanvas, e);
            e.Handled = true;
            return;
        }

        // Handle end of dragging
        if (_editHandler.IsDragging)
        {
            _editHandler.EndDragging(DrawingCanvas, e);
            e.Handled = true;
            return;
        }

        // Handle end of drawing
        if (_creationHandler.IsDrawing && ViewModel.SelectedShapeType != ShapeType.Polygon)
        {
            var endPoint = e.GetCurrentPoint(DrawingCanvas).Position;
            var result = _creationHandler.FinishDrawing(endPoint, ViewModel.SelectedShapeType, DrawingCanvas);

            if (result.HasValue)
            {
                SaveShapeToDatabase(result.Value.start, result.Value.end, ViewModel.SelectedShapeType);
            }
        }
    }

    #endregion

    #region Keyboard Events

    private void DrawingCanvasPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Shift)
        {
            _creationHandler.SetShiftPressed(true);
        }
        else if (e.Key == Windows.System.VirtualKey.Delete && _selectionHandler.HasSelection)
        {
            DeleteSelectedShape();
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            _selectionHandler.ClearSelection(DrawingCanvas);
        }
    }

    private void DrawingCanvasPage_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Shift)
        {
            _creationHandler.SetShiftPressed(false);
        }
    }

    #endregion

    #region UI Event Handlers

    private void ShapeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button && button.Tag is string tag)
        {
            // Uncheck all buttons
            SelectButton.IsChecked = false;
            LineButton.IsChecked = false;
            RectangleButton.IsChecked = false;
            OvalButton.IsChecked = false;
            CircleButton.IsChecked = false;
            TriangleButton.IsChecked = false;
            PolygonButton.IsChecked = false;

            button.IsChecked = true;
            _isSelectMode = false;

            ViewModel.SelectedShapeType = tag switch
            {
                "Line" => ShapeType.Line,
                "Rectangle" => ShapeType.Rectangle,
                "Oval" => ShapeType.Oval,
                "Circle" => ShapeType.Circle,
                "Triangle" => ShapeType.Triangle,
                "Polygon" => ShapeType.Polygon,
                _ => ShapeType.Line
            };
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            // Uncheck all shape buttons
            LineButton.IsChecked = false;
            RectangleButton.IsChecked = false;
            OvalButton.IsChecked = false;
            CircleButton.IsChecked = false;
            TriangleButton.IsChecked = false;
            PolygonButton.IsChecked = false;

            button.IsChecked = true;
            _isSelectMode = true;
            System.Diagnostics.Debug.WriteLine("Select mode enabled");
        }
    }

    private void StrokeColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        var color = args.NewColor;
        ViewModel.SelectedColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        if (StrokeColorPreview != null)
        {
            StrokeColorPreview.Color = color;
        }

        if (_selectionHandler.SelectedShape != null && _isSelectMode)
        {
            _editHandler.UpdateShapeStrokeColor(_selectionHandler.SelectedShape, ViewModel.SelectedColor);
        }
    }

    private void FillColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        var color = args.NewColor;
        ViewModel.FillColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        if (FillColorPreview != null)
        {
            FillColorPreview.Color = color;
        }

        if (_selectionHandler.SelectedShape != null && _isSelectMode)
        {
            _editHandler.UpdateShapeFillColor(_selectionHandler.SelectedShape, ViewModel.FillColor, ViewModel.IsFilled);
        }
    }

    private void StrokeThicknessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_selectionHandler.SelectedShape != null && _isSelectMode)
        {
            _editHandler.UpdateShapeThickness(_selectionHandler.SelectedShape, e.NewValue);
        }
    }

    private void StrokeStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            var style = item.Tag?.ToString() ?? "Solid";
            ViewModel.StrokeStyle = style;

            if (_selectionHandler.SelectedShape != null && _isSelectMode)
            {
                _editHandler.UpdateShapeStrokeStyle(_selectionHandler.SelectedShape, style);
            }
        }
    }

    private void ToggleToolbarButton_Click(object sender, RoutedEventArgs e)
    {
        _isToolbarExpanded = !_isToolbarExpanded;
        ToolbarContent.Visibility = _isToolbarExpanded ? Visibility.Visible : Visibility.Collapsed;

        if (sender is Button button && button.Content is FontIcon icon)
        {
            icon.Glyph = _isToolbarExpanded ? "\uE76C" : "\uE700";
        }
    }

    private void FinishPolygonButton_Click(object sender, RoutedEventArgs e)
    {
        var points = _creationHandler.FinishPolygon(DrawingCanvas);
        if (points != null && points.Count >= 3)
        {
            var shape = new Data.Models.Shape
            {
                ShapeType = ShapeType.Polygon,
                PointsData = DrawingService.PointsToJson(points),
                Color = ViewModel.SelectedColor,
                StrokeThickness = ViewModel.StrokeThickness,
                IsFilled = ViewModel.IsFilled,
                FillColor = ViewModel.IsFilled ? ViewModel.FillColor : null,
                TemplateId = ViewModel.CurrentTemplateId,
                CreatedAt = DateTime.Now
            };

            ViewModel.AddShapeCommand.ExecuteAsync(shape);
        }

        FinishPolygonButton.Visibility = Visibility.Collapsed;
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

    private void DeleteSelectedShape()
    {
        if (_selectionHandler.SelectedShape != null)
        {
            _editHandler.DeleteShape(_selectionHandler.SelectedShape, DrawingCanvas);
            _selectionHandler.ClearSelection(DrawingCanvas);
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
        if (e.PropertyName == nameof(ViewModel.IsFilled) && _selectionHandler.SelectedShape != null && _isSelectMode)
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
        if (TemplatePanel.Visibility == Visibility.Visible)
        {
            // Hide panel
            TemplatePanel.Visibility = Visibility.Collapsed;
  ShowTemplatePanelButton.Visibility = Visibility.Visible;
        }
        else
        {
            // Show panel
   TemplatePanel.Visibility = Visibility.Visible;
   ShowTemplatePanelButton.Visibility = Visibility.Collapsed;
   }
    }

    private void TemplatePreviewCanvas_Loaded(object sender, RoutedEventArgs e)
    {
      if (sender is Canvas canvas && canvas.Tag is DrawingTemplate template)
{
            // Use auto-fit to scale shapes optimally to fill the preview area
_renderingService.RenderShapesWithAutoFit(template.Shapes, canvas, padding: 10);
      }
    }

    private void Template_DragStarting(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {
     if (sender is Border border && border.DataContext is DrawingTemplate template)
   {
       args.Data.Properties.Add("TemplateId", template.Id);
      args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
          System.Diagnostics.Debug.WriteLine($"🎯 Drag started: Template '{template.Name}' (ID: {template.Id})");
   }
    }

    private void DrawingCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop to insert template";
        e.DragUIOverride.IsCaptionVisible = true;
  e.DragUIOverride.IsGlyphVisible = true;
    }

  private async void DrawingCanvas_Drop(object sender, DragEventArgs e)
{
        try
  {
     if (e.DataView.Properties.TryGetValue("TemplateId", out var templateIdObj) && templateIdObj is int templateId)
            {
 var dropPoint = e.GetPosition(DrawingCanvas);
System.Diagnostics.Debug.WriteLine($"📍 Dropped at: {dropPoint.X}, {dropPoint.Y}");

           await InsertTemplateAtPosition(templateId, dropPoint);
    }
        }
        catch (Exception ex)
{
   await ShowErrorDialog($"Failed to insert template: {ex.Message}");
      }
    }

    private async void InsertTemplateButton_Click(object sender, RoutedEventArgs e)
{
        if (sender is Button button && button.Tag is DrawingTemplate template)
   {
       // Insert at center of canvas
          var centerPoint = new Point(ViewModel.CanvasWidth / 2, ViewModel.CanvasHeight / 2);
  await InsertTemplateAtPosition(template.Id, centerPoint);
        }
    }

    private async Task InsertTemplateAtPosition(int templateId, Point position)
    {
  try
        {
         ViewModel.IsBusy = true;

 using var scope = App.Services.CreateScope();
  var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
  var shapeService = scope.ServiceProvider.GetRequiredService<IShapeService>();

   var template = await templateService.GetTemplateByIdAsync(templateId);
     if (template == null || template.Shapes.Count == 0)
   {
 await ShowErrorDialog("Template is empty or not found.");
    return;
   }

            // Calculate offset to center template at drop position
      var templateShapes = template.Shapes.ToList();
    var points = new List<Point>();
   
   foreach (var shape in templateShapes)
          {
   var shapePoints = DrawingService.JsonToPoints(shape.PointsData);
 points.AddRange(shapePoints);
        }

    if (points.Count == 0) return;

            // Find template bounds
            double minX = points.Min(p => p.X);
  double minY = points.Min(p => p.Y);
       double maxX = points.Max(p => p.X);
  double maxY = points.Max(p => p.Y);
      
 double templateCenterX = (minX + maxX) / 2;
 double templateCenterY = (minY + maxY) / 2;

   double offsetX = position.X - templateCenterX;
            double offsetY = position.Y - templateCenterY;

            // Insert shapes with offset
   foreach (var originalShape in templateShapes)
 {
    var shapePoints = DrawingService.JsonToPoints(originalShape.PointsData);
        var offsetPoints = shapePoints.Select(p => new Point(p.X + offsetX, p.Y + offsetY)).ToList();

 var newShape = new Data.Models.Shape
  {
       ShapeType = originalShape.ShapeType,
        PointsData = DrawingService.PointsToJson(offsetPoints),
    Color = originalShape.Color,
     StrokeThickness = originalShape.StrokeThickness,
         StrokeStyle = originalShape.StrokeStyle,
 IsFilled = originalShape.IsFilled,
 FillColor = originalShape.FillColor,
   TemplateId = ViewModel.CurrentTemplateId,
      CreatedAt = DateTime.Now
   };

   await ViewModel.AddShapeCommand.ExecuteAsync(newShape);
            }

 await ShowSuccessDialog($"Inserted template '{template.Name}' successfully!");
        }
        catch (Exception ex)
      {
   await ShowErrorDialog($"Failed to insert template: {ex.Message}");
        }
        finally
{
     ViewModel.IsBusy = false;
        }
    }

    #endregion
}
