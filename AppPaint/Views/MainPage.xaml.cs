using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using AppPaint.ViewModels;
using AppPaint.Services;
using Microsoft.Extensions.DependencyInjection;
using Data.Models;
using System;
using System.Linq;

namespace AppPaint.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    private Profile? _selectedProfile;

    public MainPage()
    {
      ViewModel = App.Services.GetRequiredService<MainViewModel>();
        this.DataContext = ViewModel;

   InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
  ViewModel.OnNavigatedTo(e.Parameter);
        
        // Load profiles
    await LoadProfilesAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.OnNavigatedFrom();
    }

    private async System.Threading.Tasks.Task LoadProfilesAsync()
    {
  try
  {
            using var scope = App.Services.CreateScope();
     var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
            
            var profiles = await profileService.GetAllProfilesAsync();
   ViewModel.Profiles.Clear();
            foreach (var profile in profiles)
      {
     ViewModel.Profiles.Add(profile);
   
                // Set selected profile if it's active
       if (profile.IsActive)
      {
             _selectedProfile = profile;
    UpdateSelectedProfileUI();
              }
     }
      }
        catch (Exception ex)
     {
      System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex.Message}");
   }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
 {
        // No animation needed for now
    }

    private void ProfileRadio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio && radio.Tag is Profile profile)
        {
            _selectedProfile = profile;
            UpdateSelectedProfileUI();
       
            System.Diagnostics.Debug.WriteLine($"Profile selected: {profile.Name}");
        }
    }

    private void UpdateSelectedProfileUI()
    {
    if (_selectedProfile != null)
        {
            // Enable Start Drawing button
     StartDrawingButton.IsEnabled = true;
 
   // Update text
         SelectedProfileText.Text = $"Profile: {_selectedProfile.Name} • Ready to draw!";
        }
        else
        {
   // Disable Start Drawing button
      StartDrawingButton.IsEnabled = false;
     
            // Update text
            SelectedProfileText.Text = "⚠️ Please select a profile to start drawing";
      }
    }

    private void StartDrawing_Click(object sender, RoutedEventArgs e)
  {
        if (_selectedProfile == null)
        {
        // Show warning dialog
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
       Title = "No Profile Selected",
        Content = "Please select a profile before starting to draw.",
 CloseButtonText = "OK"
 };
            _ = dialog.ShowAsync();
  return;
        }

        // Set active profile
        SetActiveProfileAsync(_selectedProfile.Id);
        
        // Navigate to DrawingCanvas
        if (App.MainWindow is MainWindow mainWindow)
        {
            if (mainWindow.Content is Frame rootFrame && rootFrame.Content is ShellPage shellPage)
   {
        shellPage.NavigateToDrawingCanvas(null); // null = new blank drawing
            }
        }
    }

    private async void SetActiveProfileAsync(int profileId)
    {
        try
        {
            using var scope = App.Services.CreateScope();
  var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
            await profileService.SetActiveProfileAsync(profileId);
     }
     catch (Exception ex)
        {
     System.Diagnostics.Debug.WriteLine($"Error setting active profile: {ex.Message}");
        }
    }

    private async void CreateProfile_Click(object sender, RoutedEventArgs e)
    {
        // Show create profile dialog
     var dialog = new ContentDialog
     {
    XamlRoot = this.XamlRoot,
     Title = "Create New Profile",
            PrimaryButtonText = "Create",
      CloseButtonText = "Cancel",
     DefaultButton = ContentDialogButton.Primary
        };

  var stackPanel = new StackPanel { Spacing = 12 };
  
stackPanel.Children.Add(new TextBlock 
        { 
          Text = "Profile Name:", 
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold 
 });

     var nameTextBox = new TextBox
        {
            PlaceholderText = "My Workspace",
            MaxLength = 200
        };
        stackPanel.Children.Add(nameTextBox);

        dialog.Content = stackPanel;

 var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
 var name = nameTextBox.Text.Trim();
            
if (string.IsNullOrEmpty(name))
       {
  var errorDialog = new ContentDialog
      {
         XamlRoot = this.XamlRoot,
      Title = "Error",
              Content = "Please enter a profile name.",
          CloseButtonText = "OK"
                };
 await errorDialog.ShowAsync();
       return;
        }

          // Create profile
    await CreateNewProfileAsync(name);
        }
    }

    private async System.Threading.Tasks.Task CreateNewProfileAsync(string name)
    {
   try
        {
         using var scope = App.Services.CreateScope();
       var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

          var newProfile = new Profile
            {
             Name = name,
     Theme = "System",
          DefaultCanvasWidth = 800,
                DefaultCanvasHeight = 600,
        DefaultStrokeColor = "#000000",
         DefaultFillColor = "#FFFF00",
     DefaultBackgroundColor = "#FFFFFF",
    DefaultStrokeThickness = 2.0,
         CreatedAt = DateTime.Now
            };

            var createdProfile = await profileService.CreateProfileAsync(newProfile);
          
        // Reload profiles
    await LoadProfilesAsync();

         // Show success message
          var successDialog = new ContentDialog
      {
          XamlRoot = this.XamlRoot,
          Title = "Success",
  Content = $"Profile '{name}' created successfully!",
             CloseButtonText = "OK"
            };
      await successDialog.ShowAsync();
        }
        catch (Exception ex)
      {
            var errorDialog = new ContentDialog
      {
      XamlRoot = this.XamlRoot,
                Title = "Error",
          Content = $"Failed to create profile: {ex.Message}",
          CloseButtonText = "OK"
            };
            await errorDialog.ShowAsync();
  }
    }

    private void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Profile profile)
        {
            // Navigate to Management > Profiles tab
       if (App.MainWindow is MainWindow mainWindow)
            {
  if (mainWindow.Content is Frame rootFrame && rootFrame.Content is ShellPage shellPage)
            {
         // Navigate to ManagementPage
          rootFrame.Navigate(typeof(ManagementPage));
           }
 }
        }
    }
}
