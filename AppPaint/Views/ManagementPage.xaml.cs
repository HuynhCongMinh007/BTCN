using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;

namespace AppPaint.Views;

public sealed partial class ManagementPage : Page
{
    public ManagementPage()
    {
  this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
 base.OnNavigatedTo(e);

        // ✅ Parse parameter: can be "PageName" or "PageName:ProfileId"
        if (e.Parameter is string param && !string.IsNullOrEmpty(param))
  {
var parts = param.Split(':');
     string pageName = parts[0];
   int? profileId = null;

 if (parts.Length > 1 && int.TryParse(parts[1], out int parsedId))
            {
       profileId = parsedId;
   System.Diagnostics.Debug.WriteLine($"📍 ManagementPage received: {pageName} with ProfileId: {profileId}");
 }

      NavigateToPage(pageName, profileId);
     }
    else
 {
  // Default to Dashboard
  NavigateToPage("Dashboard");
 }
    }

    private void ManagementNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
     if (args.SelectedItem is NavigationViewItem item)
    {
          string tag = item.Tag?.ToString() ?? "";
   NavigateToPage(tag);
    }
    }

    private void NavigateToPage(string tag, object? parameter = null)
    {
        Type? pageType = tag switch
        {
     "Dashboard" => typeof(DashboardPage),
            "Profiles" => typeof(ProfilePage),
      "Drawings" => typeof(DrawingsPage),
       "Templates" => typeof(TemplateManagerPage),
       _ => typeof(DashboardPage) // Default to Dashboard
  };

        if (pageType != null && (ContentFrame.CurrentSourcePageType != pageType || parameter != null))
    {
  ContentFrame.Navigate(pageType, parameter);

            // Update breadcrumb
      UpdateShellBreadcrumb(tag);

       // Update NavigationView selection
       var navItem = ManagementNav.MenuItems
   .OfType<NavigationViewItem>()
   .FirstOrDefault(item => item.Tag?.ToString() == tag);
     if (navItem != null)
          {
        ManagementNav.SelectedItem = navItem;
      }
        }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
   // Optional: Update UI based on navigation
  System.Diagnostics.Debug.WriteLine($"📍 ContentFrame navigated to: {e.SourcePageType.Name}");
    }

    /// <summary>
    /// Update breadcrumb in ShellPage
    /// </summary>
    private void UpdateShellBreadcrumb(string subPageTitle)
    {
 try
  {
 // METHOD 1: Navigate up via visual tree
         var shellPage = FindShellPageInVisualTree();
       
 if (shellPage != null)
     {
          shellPage.UpdateBreadcrumbWithSubPage("Management", "Management", subPageTitle);
   System.Diagnostics.Debug.WriteLine($"✅ Breadcrumb updated: Home > Management > {subPageTitle}");
 }
   else
   {
     System.Diagnostics.Debug.WriteLine("⚠️ Could not find ShellPage in visual tree");
             
     // METHOD 2: Try via MainWindow
     TryUpdateViaMainWindow(subPageTitle);
   }
        }
 catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"❌ Error updating breadcrumb: {ex.Message}");
 }
    }

    /// <summary>
    /// Find ShellPage by walking up the visual tree
/// </summary>
    private ShellPage? FindShellPageInVisualTree()
    {
        DependencyObject? current = this;
  
        // Walk up max 10 levels
   for (int i = 0; i < 10 && current != null; i++)
        {
   System.Diagnostics.Debug.WriteLine($"  Level {i}: {current.GetType().Name}");
     
   if (current is ShellPage shellPage)
       {
    System.Diagnostics.Debug.WriteLine($"  ✅ Found ShellPage at level {i}");
       return shellPage;
         }
    
      current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        
  return null;
    }

    /// <summary>
    /// Try to update breadcrumb via MainWindow
    /// </summary>
    private void TryUpdateViaMainWindow(string subPageTitle)
    {
        try
     {
      if (App.MainWindow?.Content is Grid mainGrid)
   {
          var rootFrame = mainGrid.FindName("RootFrame") as Frame;
         if (rootFrame?.Content is ShellPage shellPage)
{
       shellPage.UpdateBreadcrumbWithSubPage("Management", "Management", subPageTitle);
     System.Diagnostics.Debug.WriteLine($"✅ Breadcrumb updated via MainWindow: {subPageTitle}");
  }
      }
 }
   catch (Exception ex)
   {
   System.Diagnostics.Debug.WriteLine($"❌ Error updating via MainWindow: {ex.Message}");
   }
  }
}
