using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;

namespace AppPaint.Views;

public sealed partial class ManagementPage : Page
{
    // Breadcrumb items
  public ObservableCollection<string> BreadcrumbItems { get; } = new()
    {
        "Home",
    "Management"
    };

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
     }
    }

    private void ManagementTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ManagementTabView.SelectedItem is TabViewItem selectedTab)
        {
 string tabHeader = selectedTab.Header?.ToString() ?? "";
  
   // Update breadcrumb
    if (BreadcrumbItems.Count > 2)
 {
      BreadcrumbItems.RemoveAt(2);
         }
   BreadcrumbItems.Add(tabHeader);
 
            System.Diagnostics.Debug.WriteLine($"📍 Management Tab: {tabHeader}");
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
    }
}
