using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AppPaint.ViewModels;
using AppPaint.Services;
using Microsoft.Extensions.DependencyInjection;
using Data.Models;
using System;

namespace AppPaint.Views;

public sealed partial class ProfilePage : Page
{
    public ProfileViewModel ViewModel { get; }

    public ProfilePage()
    {
        ViewModel = App.Services.GetRequiredService<ProfileViewModel>();
        this.DataContext = ViewModel;
        
        // Subscribe to events
        ViewModel.NavigateBackRequested += OnNavigateBackRequested;
        
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
 ViewModel.OnNavigatedTo(e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        
        // Unsubscribe
        ViewModel.NavigateBackRequested -= OnNavigateBackRequested;
    
        ViewModel.OnNavigatedFrom();
    }

    private void OnNavigateBackRequested(object? sender, System.EventArgs e)
  {
      if (Frame.CanGoBack)
        {
        Frame.GoBack();
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

   // Create profile using ViewModel command
            await ViewModel.CreateProfileAsync(name);
        }
    }

private void StrokeColor_Changed(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (ViewModel.SelectedProfile != null)
    {
            var color = args.NewColor;
            ViewModel.SelectedProfile.DefaultStrokeColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    private void FillColor_Changed(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (ViewModel.SelectedProfile != null)
      {
   var color = args.NewColor;
     ViewModel.SelectedProfile.DefaultFillColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
  }

    private void BackgroundColor_Changed(ColorPicker sender, ColorChangedEventArgs args)
    {
    if (ViewModel.SelectedProfile != null)
        {
   var color = args.NewColor;
          ViewModel.SelectedProfile.DefaultBackgroundColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
