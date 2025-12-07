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

    private async void StartDrawing_Click(object sender, RoutedEventArgs e)
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
            await dialog.ShowAsync();
            return;
        }

        System.Diagnostics.Debug.WriteLine($"🎯 StartDrawing_Click - Profile: {_selectedProfile.Name} (ID: {_selectedProfile.Id})");

        // ✅ Show confirmation dialog
        var confirmDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Start Drawing",
            PrimaryButtonText = "Start Drawing",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var stackPanel = new StackPanel { Spacing = 16 };

        // Profile info
        stackPanel.Children.Add(new TextBlock
        {
            Text = "You are about to start drawing with:",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black)
        });

        // Profile card
        var profileCard = new Border
        {
            Padding = new Thickness(16),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.AliceBlue),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8)
        };

        var profileInfo = new StackPanel { Spacing = 8 };

        // Profile name
        var namePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        namePanel.Children.Add(new FontIcon
        {
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            Glyph = "\uE748", // Contact icon
            FontSize = 16,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
        });
        namePanel.Children.Add(new TextBlock
        {
            Text = $"Profile: {_selectedProfile.Name}",
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
            VerticalAlignment = VerticalAlignment.Center
        });
        profileInfo.Children.Add(namePanel);

        // Canvas size
        var canvasPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        canvasPanel.Children.Add(new FontIcon
        {
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            Glyph = "\uE8B9", // Picture icon
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray)
        });
        canvasPanel.Children.Add(new TextBlock
        {
            Text = $"Canvas: {_selectedProfile.DefaultCanvasWidth} × {_selectedProfile.DefaultCanvasHeight} px",
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
            VerticalAlignment = VerticalAlignment.Center
        });
        profileInfo.Children.Add(canvasPanel);

        // Theme
        var themePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        themePanel.Children.Add(new FontIcon
        {
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            Glyph = "\uE793", // Theme icon
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray)
        });
        themePanel.Children.Add(new TextBlock
        {
            Text = $"Theme: {_selectedProfile.Theme}",
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
            VerticalAlignment = VerticalAlignment.Center
        });
        profileInfo.Children.Add(themePanel);

        // Stroke thickness
        var brushPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        brushPanel.Children.Add(new FontIcon
        {
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            Glyph = "\uE70F", // Edit icon
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray)
        });
        brushPanel.Children.Add(new TextBlock
        {
            Text = $"Default Stroke: {_selectedProfile.DefaultStrokeThickness}px",
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
            VerticalAlignment = VerticalAlignment.Center
        });
        profileInfo.Children.Add(brushPanel);

        profileCard.Child = profileInfo;
        stackPanel.Children.Add(profileCard);

        // Info message
        stackPanel.Children.Add(new TextBlock
        {
            Text = "All settings from this profile will be applied to your drawing canvas.",
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
            TextWrapping = TextWrapping.Wrap
        });

        confirmDialog.Content = stackPanel;

        var result = await confirmDialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
        {
            // User cancelled
            System.Diagnostics.Debug.WriteLine("❌ User cancelled drawing");
            return;
        }

        // User confirmed, proceed with drawing
        System.Diagnostics.Debug.WriteLine("✅ User confirmed, starting drawing...");

        // Set active profile
        SetActiveProfileAsync(_selectedProfile.Id);

        // Navigate to DrawingCanvas with Profile settings
        if (App.MainWindow is MainWindow mainWindow)
        {
            System.Diagnostics.Debug.WriteLine("✅ Got MainWindow");

            // Get RootFrame from MainWindow (defined in MainWindow.xaml)
            var rootFrame = mainWindow.Content as Grid;
            if (rootFrame != null)
            {
                // Find RootFrame by name
                var frameElement = rootFrame.FindName("RootFrame") as Frame;
                if (frameElement != null && frameElement.Content is ShellPage shellPage)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Got ShellPage via RootFrame");

                    // Pass ProfileId to apply settings
                    var parameters = new AppPaint.Models.DrawingNavigationParameters(
                     _selectedProfile.Id,
                  drawingId: null // null = new blank drawing
                   );

                    System.Diagnostics.Debug.WriteLine($"✅ Created DrawingNavigationParameters: ProfileId={parameters.ProfileId}, DrawingId={parameters.DrawingId}");

                    shellPage.NavigateToDrawingCanvas(parameters);
                    System.Diagnostics.Debug.WriteLine("✅ Navigation called!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Could not get ShellPage from RootFrame");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ MainWindow.Content is not Grid");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ Could not get MainWindow");
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
            System.Diagnostics.Debug.WriteLine($"✏️ Edit Profile clicked: {profile.Name} (ID: {profile.Id})");

            // Navigate to Management > Profiles with profile ID
            if (App.MainWindow is MainWindow mainWindow)
            {
                var rootFrame = mainWindow.Content as Grid;
                if (rootFrame != null)
                {
                    var frameElement = rootFrame.FindName("RootFrame") as Frame;
                    if (frameElement != null)
                    {
                        // Navigate to ManagementPage with profile navigation info
                        // Format: "Profiles:{profileId}"
                        string navigationParam = $"Profiles:{profile.Id}";
                        frameElement.Navigate(typeof(ManagementPage), navigationParam);

                        System.Diagnostics.Debug.WriteLine($"✅ Navigating to Management with: {navigationParam}");
                    }
                }
            }
        }
    }
}
