using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Data.Models;

namespace AppPaint.Handlers;

/// <summary>
/// Handles template panel UI events (hover, drag, visibility)
/// </summary>
public class TemplateUIHandler
{
    /// <summary>
  /// Toggle template panel visibility
    /// </summary>
    public void TogglePanel(Border panel, Button showButton)
    {
     if (panel.Visibility == Visibility.Visible)
        {
            panel.Visibility = Visibility.Collapsed;
            showButton.Visibility = Visibility.Visible;
        }
     else
        {
      panel.Visibility = Visibility.Visible;
   showButton.Visibility = Visibility.Collapsed;
    }
    }

    /// <summary>
    /// Handle template card pointer entered (hover effect)
    /// </summary>
    public void HandleCardPointerEntered(Border border)
    {
        if (border.RenderTransform is ScaleTransform scaleTransform)
      {
AnimateScale(scaleTransform, 1.02, 200);
            border.Translation = new System.Numerics.Vector3(0, -2, 8);
        }
    }

    /// <summary>
    /// Handle template card pointer exited (remove hover effect)
 /// </summary>
    public void HandleCardPointerExited(Border border)
    {
        if (border.RenderTransform is ScaleTransform scaleTransform)
    {
     AnimateScale(scaleTransform, 1.0, 200);
          border.Translation = new System.Numerics.Vector3(0, 0, 0);
        }
    }

    /// <summary>
    /// Handle template drag starting
    /// </summary>
    public void HandleDragStarting(DrawingTemplate template, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {
        args.Data.Properties.Add("TemplateId", template.Id);
  args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
      System.Diagnostics.Debug.WriteLine($"🎯 Drag started: Template '{template.Name}' (ID: {template.Id})");
    }

    /// <summary>
    /// Animate scale transform
    /// </summary>
 private void AnimateScale(ScaleTransform scaleTransform, double targetScale, int durationMs)
    {
        var scaleXAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
{
    To = targetScale,
      Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase 
            { 
  EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut 
  }
        };

     var scaleYAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
To = targetScale,
     Duration = TimeSpan.FromMilliseconds(durationMs),
  EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase 
            { 
        EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut 
  }
        };

  Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleXAnim, scaleTransform);
 Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

    Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleYAnim, scaleTransform);
     Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

  var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        storyboard.Children.Add(scaleXAnim);
        storyboard.Children.Add(scaleYAnim);
        storyboard.Begin();
    }
}
