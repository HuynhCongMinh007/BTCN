using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Data.Models;
using AppPaint.ViewModels;
using AppPaint.Services;

namespace AppPaint.Handlers;

/// <summary>
/// Handles UI control events (buttons, sliders, color pickers, etc.)
/// </summary>
public class UIEventHandler
{
    private readonly DrawingCanvasViewModel _viewModel;
    private readonly ShapeSelectionHandler _selectionHandler;
    private readonly ShapeEditHandler _editHandler;
    private readonly ShapeCreationHandler _creationHandler;

    private bool _isSelectMode = false;

    public UIEventHandler(
DrawingCanvasViewModel viewModel,
      ShapeSelectionHandler selectionHandler,
        ShapeEditHandler editHandler,
    ShapeCreationHandler creationHandler)
    {
        _viewModel = viewModel;
        _selectionHandler = selectionHandler;
        _editHandler = editHandler;
        _creationHandler = creationHandler;
    }

    /// <summary>
    /// Handle shape tool button clicks
    /// </summary>
    public void HandleShapeButtonClick(ToggleButton button, string tag,
        ToggleButton selectButton,
        ToggleButton lineButton,
        ToggleButton rectangleButton,
        ToggleButton ovalButton,
        ToggleButton circleButton,
        ToggleButton triangleButton,
        ToggleButton polygonButton)
    {
        // Uncheck all buttons
        selectButton.IsChecked = false;
        lineButton.IsChecked = false;
        rectangleButton.IsChecked = false;
        ovalButton.IsChecked = false;
        circleButton.IsChecked = false;
        triangleButton.IsChecked = false;
        polygonButton.IsChecked = false;

        button.IsChecked = true;
        _isSelectMode = false;

        _viewModel.SelectedShapeType = tag switch
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

    /// <summary>
    /// Handle select button click
    /// </summary>
    public void HandleSelectButtonClick(ToggleButton button,
        ToggleButton lineButton,
   ToggleButton rectangleButton,
        ToggleButton ovalButton,
        ToggleButton circleButton,
      ToggleButton triangleButton,
      ToggleButton polygonButton)
    {
        // Uncheck all shape buttons
        lineButton.IsChecked = false;
        rectangleButton.IsChecked = false;
        ovalButton.IsChecked = false;
        circleButton.IsChecked = false;
        triangleButton.IsChecked = false;
        polygonButton.IsChecked = false;

        button.IsChecked = true;
        _isSelectMode = true;
        System.Diagnostics.Debug.WriteLine("Select mode enabled");
    }

    /// <summary>
    /// Get select mode state
    /// </summary>
    public bool IsSelectMode => _isSelectMode;

    /// <summary>
    /// Handle stroke color changed
    /// </summary>
    public void HandleStrokeColorChanged(Windows.UI.Color color, SolidColorBrush? previewBrush)
    {
        _viewModel.SelectedColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        if (previewBrush != null)
        {
            previewBrush.Color = color;
        }

        if (_selectionHandler.SelectedShape != null && _isSelectMode)
        {
            _editHandler.UpdateShapeStrokeColor(_selectionHandler.SelectedShape, _viewModel.SelectedColor);
        }
    }

    /// <summary>
    /// Handle fill color changed
    /// </summary>
    public void HandleFillColorChanged(Windows.UI.Color color, SolidColorBrush? previewBrush)
    {
        _viewModel.FillColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        if (previewBrush != null)
        {
            previewBrush.Color = color;
        }

        if (_selectionHandler.SelectedShape != null && _isSelectMode)
        {
            _editHandler.UpdateShapeFillColor(_selectionHandler.SelectedShape, _viewModel.FillColor, _viewModel.IsFilled);
        }
    }

    /// <summary>
    /// Handle stroke thickness changed
    /// </summary>
    public void HandleStrokeThicknessChanged(double newValue)
    {
        if (_selectionHandler.SelectedShape != null && _isSelectMode)
        {
            _editHandler.UpdateShapeThickness(_selectionHandler.SelectedShape, newValue);
        }
    }

    /// <summary>
    /// Handle stroke style changed
    /// </summary>
    public void HandleStrokeStyleChanged(string style)
    {
        _viewModel.StrokeStyle = style;

        if (_selectionHandler.SelectedShape != null && _isSelectMode)
        {
            _editHandler.UpdateShapeStrokeStyle(_selectionHandler.SelectedShape, style);
        }
    }

    /// <summary>
    /// Handle toolbar toggle
    /// </summary>
    public bool HandleToolbarToggle(bool currentState, FrameworkElement content, FontIcon icon)
    {
        bool newState = !currentState;
        content.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
        icon.Glyph = newState ? "\uE76C" : "\uE700";
        return newState;
    }

    /// <summary>
    /// Handle finish polygon button
    /// </summary>
    public async Task<bool> HandleFinishPolygonAsync(Canvas canvas, Action hideButton)
    {
        var points = _creationHandler.FinishPolygon(canvas);
        if (points != null && points.Count >= 3)
        {
            var shape = new Data.Models.Shape
            {
                ShapeType = ShapeType.Polygon,
                PointsData = DrawingService.PointsToJson(points),
                Color = _viewModel.SelectedColor,
                StrokeThickness = _viewModel.StrokeThickness,
                StrokeStyle = _viewModel.StrokeStyle, // ✅ Fixed: Save stroke style for polygon
                IsFilled = _viewModel.IsFilled,
                FillColor = _viewModel.IsFilled ? _viewModel.FillColor : null,
                TemplateId = _viewModel.CurrentTemplateId,
                CreatedAt = DateTime.Now
            };

            await _viewModel.AddShapeCommand.ExecuteAsync(shape);
            hideButton();
            return true;
        }

        hideButton();
        return false;
    }

    /// <summary>
    /// Handle keyboard down events
    /// </summary>
    public void HandleKeyDown(Windows.System.VirtualKey key, Canvas canvas)
    {
        if (key == Windows.System.VirtualKey.Shift)
        {
            _creationHandler.SetShiftPressed(true);
        }
        else if (key == Windows.System.VirtualKey.Delete && _selectionHandler.HasSelection)
        {
            if (_selectionHandler.SelectedShape != null)
            {
                _editHandler.DeleteShape(_selectionHandler.SelectedShape, canvas);
                _selectionHandler.ClearSelection(canvas);
            }
        }
        else if (key == Windows.System.VirtualKey.Escape)
        {
            _selectionHandler.ClearSelection(canvas);
        }
    }

    /// <summary>
    /// Handle keyboard up events
    /// </summary>
    public void HandleKeyUp(Windows.System.VirtualKey key)
    {
        if (key == Windows.System.VirtualKey.Shift)
        {
            _creationHandler.SetShiftPressed(false);
        }
    }
}
