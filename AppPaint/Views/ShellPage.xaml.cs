using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using AppPaint.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AppPaint.Views;

public sealed partial class ShellPage : Page
{
    private readonly Dictionary<string, Type> _pages = new()
    {
     { "Home", typeof(MainPage) },
 { "Management", typeof(ManagementPage) } 
        // DrawingCanvas removed - only accessible via MainPage or Management
    };

  private readonly List<(string Tag, string Title)> _breadcrumbs = new();

 public ShellPage()
  {
 this.InitializeComponent();
   
   // Navigate to Home by default
        ContentFrame.Navigate(typeof(MainPage));
  UpdateBreadcrumb("Home", "Home");
  
        // Update back button state
        NavView.IsBackEnabled = false;
  
    // Load theme asynchronously
 _ = LoadThemeAsync();
  }

    private async System.Threading.Tasks.Task LoadThemeAsync()
    {
  try
        {
 // Create a scope for scoped services
     using var scope = App.Services.CreateScope();
 var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();
  
        var savedTheme = await themeService.GetSavedThemeAsync();
   
   // Update toggle switch based on theme
   ThemeToggle.IsOn = savedTheme == ElementTheme.Dark;
   
    // Apply theme
      themeService.ApplyTheme(savedTheme);
 }
        catch (Exception ex)
   {
     System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
   if (args.InvokedItemContainer != null)
   {
      var tag = args.InvokedItemContainer.Tag?.ToString();
  if (tag != null && _pages.ContainsKey(tag))
   {
 ContentFrame.Navigate(_pages[tag]);
  UpdateBreadcrumb(tag, args.InvokedItemContainer.Content?.ToString() ?? tag);
  }
   }
  }

 private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
      if (ContentFrame.CanGoBack)
   {
       ContentFrame.GoBack();
  }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
  NavView.IsBackEnabled = ContentFrame.CanGoBack;
  
    // Update selected item
      var pageType = e.SourcePageType;
var selectedItem = NavView.MenuItems
   .OfType<NavigationViewItem>()
       .FirstOrDefault(item =>
      {
var tag = item.Tag?.ToString();
    return tag != null && _pages.ContainsKey(tag) && _pages[tag] == pageType;
   });

if (selectedItem != null)
 {
      NavView.SelectedItem = selectedItem;
}
    }

    private void UpdateBreadcrumb(string tag, string title)
    {
_breadcrumbs.Clear();
        _breadcrumbs.Add(("Home", "Home"));
   
 if (tag != "Home")
 {
_breadcrumbs.Add((tag, title));
 }

  BreadcrumbNav.ItemsSource = _breadcrumbs.Select(b => b.Title).ToList();
    }

    private async void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
{
   try
        {
      var theme = ThemeToggle.IsOn ? ElementTheme.Dark : ElementTheme.Light;
  
     // Create a scope for scoped services
       using var scope = App.Services.CreateScope();
    var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();
     
 await themeService.SetThemeAsync(theme);
   }
      catch (Exception ex)
   {
      System.Diagnostics.Debug.WriteLine($"Error setting theme: {ex.Message}");
 }
  }

    /// <summary>
    /// Navigate to DrawingCanvasPage from anywhere in the app
    /// </summary>
    public void NavigateToDrawingCanvas(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"🚀 ShellPage.NavigateToDrawingCanvas called with parameter: {parameter?.GetType().Name ?? "null"}");
   
 ContentFrame.Navigate(typeof(DrawingCanvasPage), parameter);
 System.Diagnostics.Debug.WriteLine("✅ ContentFrame.Navigate called");
      
        UpdateBreadcrumb("DrawingCanvas", "Draw Canvas");
        
        // Update NavigationView selection
   var drawingItem = NavView.MenuItems
       .OfType<NavigationViewItem>()
     .FirstOrDefault(item => item.Tag?.ToString() == "DrawingCanvas");
  if (drawingItem != null)
      {
      NavView.SelectedItem = drawingItem;
        }
    }
}
