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
        ViewModel.SaveProfileSuccess += OnSaveProfileSuccess;
    ViewModel.SetActiveProfileSuccess += OnSetActiveProfileSuccess;
        ViewModel.DeleteProfileSuccess += OnDeleteProfileSuccess;
        
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
        ViewModel.SaveProfileSuccess -= OnSaveProfileSuccess;
      ViewModel.SetActiveProfileSuccess -= OnSetActiveProfileSuccess;
        ViewModel.DeleteProfileSuccess -= OnDeleteProfileSuccess;
  
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

    private async void OnSaveProfileSuccess(object? sender, string profileName)
    {
        var dialog = new ContentDialog
        {
   XamlRoot = this.XamlRoot,
      Title = "Profile Saved Successfully",
       CloseButtonText = "OK",
       DefaultButton = ContentDialogButton.Close
      };

        var stackPanel = new StackPanel { Spacing = 16 };

        // Success message
    stackPanel.Children.Add(new TextBlock
   {
         Text = $"Profile '{profileName}' has been saved successfully!",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green),
TextWrapping = TextWrapping.Wrap
        });

        // Info about drawings
 var infoPanel = new StackPanel { Spacing = 8 };
 
     infoPanel.Children.Add(new TextBlock
   {
            Text = "What happens next:",
     FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
          Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray)
        });

        // Info items
        var info1 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        info1.Children.Add(new FontIcon 
        { 
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
      Glyph = "\uE73E",
            FontSize = 16,
     Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
        });
        info1.Children.Add(new TextBlock
        {
 Text = "New drawings will use these updated settings",
            FontSize = 12,
   TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center
        });
        infoPanel.Children.Add(info1);

        var info2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
    info2.Children.Add(new FontIcon 
      { 
  FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            Glyph = "\uE8B7",
       FontSize = 16,
 Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
    });
        info2.Children.Add(new TextBlock
        {
            Text = "Existing drawings linked to this profile will reload with updated settings",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
          VerticalAlignment = VerticalAlignment.Center
    });
        infoPanel.Children.Add(info2);

        var info3 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
      info3.Children.Add(new FontIcon 
        { 
     FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            Glyph = "\uE946",
         FontSize = 16,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
        });
        info3.Children.Add(new TextBlock
{
            Text = "Already drawn shapes will keep their colors",
            FontSize = 12,
     TextWrapping = TextWrapping.Wrap,
      VerticalAlignment = VerticalAlignment.Center
        });
        infoPanel.Children.Add(info3);

  stackPanel.Children.Add(infoPanel);

 dialog.Content = stackPanel;
        await dialog.ShowAsync();
    }
}
