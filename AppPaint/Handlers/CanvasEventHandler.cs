using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using System;
using System.Threading.Tasks;
using Data.Models;
using AppPaint.Services;
using AppPaint.ViewModels;
using UIShape = Microsoft.UI.Xaml.Shapes.Shape;

namespace AppPaint.Handlers;

/// <summary>
/// Handles canvas-related events (drawing, selection, drag/drop)
/// </summary>
public class CanvasEventHandler
{
    private readonly ShapeCreationHandler _creationHandler;
    private readonly ShapeSelectionHandler _selectionHandler;
    private readonly ShapeEditHandler _editHandler;
    private readonly TemplateInsertionHandler _insertionHandler;
 private readonly DrawingCanvasViewModel _viewModel;

    private bool _isSelectMode = false;

    public bool IsSelectMode
    {
  get => _isSelectMode;
    set => _isSelectMode = value;
    }

    public CanvasEventHandler(
        ShapeCreationHandler creationHandler,
  ShapeSelectionHandler selectionHandler,
      ShapeEditHandler editHandler,
    TemplateInsertionHandler insertionHandler,
 DrawingCanvasViewModel viewModel)
    {
     _creationHandler = creationHandler;
 _selectionHandler = selectionHandler;
   _editHandler = editHandler;
  _insertionHandler = insertionHandler;
        _viewModel = viewModel;
    }

    /// <summary>
    /// Handle pointer pressed on canvas
 /// </summary>
    public void HandlePointerPressed(Canvas canvas, PointerRoutedEventArgs e, Action showFinishPolygonButton)
    {
        var point = e.GetCurrentPoint(canvas).Position;

        // Selection mode
        if (_isSelectMode)
        {
         bool clickedOnSelectedShape = _selectionHandler.SelectedShape != null &&
     _selectionHandler.SelectedShape is UIShape selectedShape &&
          IsPointInSelectedShape(selectedShape, point);

   if (clickedOnSelectedShape && !_editHandler.IsDragging && !_editHandler.IsResizing)
            {
     _editHandler.StartDragging(_selectionHandler.SelectedShape!, point, canvas, e);
   e.Handled = true;
}
      else
     {
         _selectionHandler.SelectShapeAtPoint(point, canvas);
     }
     return;
        }

        // Drawing mode
        _creationHandler.StartDrawing(point, _viewModel.SelectedShapeType, canvas);

    // Show finish button for polygon
        if (_viewModel.SelectedShapeType == ShapeType.Polygon && _creationHandler.PolygonPoints.Count > 0)
        {
        showFinishPolygonButton();
        }
    }

    /// <summary>
    /// Handle pointer moved on canvas
    /// </summary>
    public void HandlePointerMoved(Canvas canvas, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(canvas).Position;

    // Handle resizing
        if (_editHandler.IsResizing && _selectionHandler.SelectedShape != null)
        {
            _editHandler.ResizeShape(_selectionHandler.SelectedShape, currentPoint);
         _selectionHandler.UpdateSelectionBorder(canvas);
            e.Handled = true;
      return;
   }

  // Handle dragging
 if (_editHandler.IsDragging && _selectionHandler.SelectedShape != null)
   {
     _editHandler.DragShape(_selectionHandler.SelectedShape, currentPoint, canvas);
   _selectionHandler.UpdateSelectionBorder(canvas);
          e.Handled = true;
         return;
        }

        // Handle preview during drawing
 if (_creationHandler.IsDrawing)
        {
 _creationHandler.UpdatePreview(currentPoint, _viewModel.SelectedShapeType,
        _viewModel.SelectedColor, _viewModel.StrokeThickness, _viewModel.IsFilled,
    _viewModel.FillColor, _viewModel.StrokeStyle, canvas);
        }
  }

    /// <summary>
    /// Handle pointer released on canvas
    /// </summary>
    public (bool shouldSave, Point start, Point end, ShapeType shapeType) HandlePointerReleased(
    Canvas canvas, 
        PointerRoutedEventArgs e)
    {
     // Handle end of resizing
   if (_editHandler.IsResizing)
        {
   _editHandler.EndResizing(canvas, e);
   e.Handled = true;
            return (false, default, default, default);
    }

     // Handle end of dragging
        if (_editHandler.IsDragging)
  {
            _editHandler.EndDragging(canvas, e);
     e.Handled = true;
            return (false, default, default, default);
        }

// Handle end of drawing
        if (_creationHandler.IsDrawing && _viewModel.SelectedShapeType != ShapeType.Polygon)
        {
            var endPoint = e.GetCurrentPoint(canvas).Position;
            var result = _creationHandler.FinishDrawing(endPoint, _viewModel.SelectedShapeType, canvas);

 if (result.HasValue)
            {
   return (true, result.Value.start, result.Value.end, _viewModel.SelectedShapeType);
            }
    }

      return (false, default, default, default);
    }

    /// <summary>
    /// Handle drag over canvas
    /// </summary>
    public void HandleDragOver(DragEventArgs e)
 {
 e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop to insert template";
  e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsGlyphVisible = true;
    }

    /// <summary>
    /// Handle drop on canvas
 /// </summary>
    public async Task<(bool success, string error)> HandleDropAsync(Canvas canvas, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("TemplateId", out var templateIdObj) && templateIdObj is int templateId)
        {
 var dropPoint = e.GetPosition(canvas);
    System.Diagnostics.Debug.WriteLine($"📍 Dropped at: {dropPoint.X}, {dropPoint.Y}");

   var result = await _insertionHandler.InsertTemplateAtPosition(
              templateId, 
         dropPoint, 
        _viewModel.CurrentTemplateId,
        async (shape) => await _viewModel.AddShapeCommand.ExecuteAsync(shape));

         return (result.success, result.error);
 }

     return (false, "Invalid template data");
    }

    /// <summary>
    /// Insert template at center of canvas
    /// </summary>
    public async Task<(bool success, string error)> InsertTemplateAtCenterAsync(int templateId)
    {
        var centerPoint = new Point(_viewModel.CanvasWidth / 2, _viewModel.CanvasHeight / 2);
        
     var result = await _insertionHandler.InsertTemplateAtPosition(
        templateId,
     centerPoint,
      _viewModel.CurrentTemplateId,
            async (shape) => await _viewModel.AddShapeCommand.ExecuteAsync(shape));

        return (result.success, result.error);
    }

    #region Helper Methods

  private bool IsPointInSelectedShape(UIShape shape, Point point)
    {
        double left = Canvas.GetLeft(shape);
  double top = Canvas.GetTop(shape);

        if (shape is Microsoft.UI.Xaml.Shapes.Line line)
        {
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

    #endregion
}
