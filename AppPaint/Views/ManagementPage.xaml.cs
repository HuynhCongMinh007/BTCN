using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace AppPaint.Views;

public sealed partial class ManagementPage : Page
{
    public ManagementPage()
    {
        this.InitializeComponent();
        
        // Set default selection to Profiles
        if (ManagementNav.MenuItems.Count > 0)
     {
            ManagementNav.SelectedItem = ManagementNav.MenuItems[0];
        }
  }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
 
        // Navigate to default page (Profiles)
        if (ContentFrame.Content == null)
        {
        ContentFrame.Navigate(typeof(ProfilePage));
        }
    }

    private void ManagementNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
     if (args.SelectedItem is NavigationViewItem item)
        {
 string tag = item.Tag?.ToString() ?? "";
            
  Type? pageType = tag switch
            {
          "Profiles" => typeof(ProfilePage),
    "Drawings" => typeof(DrawingsPage), // ✅ Use DrawingsPage for saved drawings
     "Templates" => typeof(TemplateManagerPage), // Will create TemplatesPage later for shape presets
      "Dashboard" => typeof(DashboardPage), // ✅ Add Dashboard
                _ => null
       };

    if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
      {
      ContentFrame.Navigate(pageType);
     }
        }
    }

    private void ManagementNav_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
    if (ContentFrame.CanGoBack)
        {
         ContentFrame.GoBack();
        }
     else if (Frame.CanGoBack)
        {
            // Go back to previous page (likely MainPage)
            Frame.GoBack();
      }
    }
}
