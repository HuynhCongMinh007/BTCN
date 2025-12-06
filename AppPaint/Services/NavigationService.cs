using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace AppPaint.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public Frame? Frame
    {
    get => _frame;
        set => _frame = value;
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void NavigateTo(Type pageType, object? parameter = null)
    {
   if (_frame == null)
        {
     throw new InvalidOperationException("Navigation frame is not set.");
    }

        _frame.Navigate(pageType, parameter, new SlideNavigationTransitionInfo
        {
 Effect = SlideNavigationTransitionEffect.FromRight
  });
   }

    public void GoBack()
  {
      if (CanGoBack)
        {
     _frame?.GoBack();
        }
    }

    public void ClearBackStack()
    {
        if (_frame != null)
 {
            _frame.BackStack.Clear();
     }
    }
}
