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
 
     // Load initial tabs
 ProfilesFrame.Navigate(typeof(ProfilePage));
        DrawingsFrame.Navigate(typeof(DrawingsPage));
   TemplatesFrame.Navigate(typeof(TemplateManagerPage));
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
 
        // Check if specific tab requested
        if (e.Parameter is string tabName)
        {
 SelectTab(tabName);
   }
   else
   {
    // Default to Profiles tab
     ManagementTabView.SelectedIndex = 0;
      
   // Update breadcrumb for default tab (Profiles)
            UpdateShellBreadcrumb("Profiles");
 }
    }

    private void ManagementTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    if (ManagementTabView.SelectedItem is TabViewItem selectedTab)
        {
          string tabHeader = selectedTab.Header?.ToString() ?? "";

   // Update ShellPage breadcrumb
 UpdateShellBreadcrumb(tabHeader);

          // Show/hide toolbar based on tab
   int selectedIndex = ManagementTabView.SelectedIndex;
        ToolbarBorder.Visibility = (selectedIndex == 1 || selectedIndex == 2) 
     ? Visibility.Visible 
          : Visibility.Collapsed;

        System.Diagnostics.Debug.WriteLine($"📍 Management Tab: {tabHeader}");
        }
    }

    /// <summary>
    /// Update breadcrumb in ShellPage
 /// </summary>
    private void UpdateShellBreadcrumb(string subPageTitle)
    {
        try
 {
    // Navigate up to find ShellPage
   var frame = this.Frame;
   if (frame?.Parent is Grid grid && grid.Parent is NavigationView navView && navView.Parent is Grid shellGrid && shellGrid.Parent is ShellPage shellPage)
            {
     shellPage.UpdateBreadcrumbWithSubPage("Management", "Management", subPageTitle);
            }
    else
{
  System.Diagnostics.Debug.WriteLine("⚠️ Could not find ShellPage to update breadcrumb");
       }
        }
   catch (Exception ex)
  {
        System.Diagnostics.Debug.WriteLine($"❌ Error updating breadcrumb: {ex.Message}");
     }
    }

    private void SelectTab(string tabName)
    {
        int index = tabName?.ToLower() switch
        {
  "profiles" => 0,
            "drawings" => 1,
       "templates" => 2,
            _ => 0
        };

        ManagementTabView.SelectedIndex = index;

        // Manually update breadcrumb (since SelectionChanged might not fire)
        string tabHeader = tabName?.ToLower() switch
        {
            "profiles" => "Profiles",
            "drawings" => "Drawings",
   "templates" => "Templates",
  _ => "Profiles"
        };
        UpdateShellBreadcrumb(tabHeader);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
      // Refresh current tab content
        int selectedIndex = ManagementTabView.SelectedIndex;
   
        switch (selectedIndex)
        {
    case 1: // Drawings
          if (DrawingsFrame.Content is DrawingsPage drawingsPage)
    {
   var viewModel = drawingsPage.ViewModel;
              viewModel.LoadTemplatesCommand.Execute(null);
     }
    break;
    
      case 2: // Templates
       if (TemplatesFrame.Content is TemplateManagerPage templatesPage)
   {
          var viewModel = templatesPage.ViewModel;
viewModel.RefreshTemplatesCommand.Execute(null);
    }
    break;
        }
    }
}
