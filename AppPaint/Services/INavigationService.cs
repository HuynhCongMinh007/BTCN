using Microsoft.UI.Xaml.Controls;
using System;

namespace AppPaint.Services;

public interface INavigationService
{
    Frame? Frame { get; set; }
    
  bool CanGoBack { get; }
    
 void NavigateTo(Type pageType, object? parameter = null);
    
    void GoBack();
  
    void ClearBackStack();
}
