using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppPaint.Services;
using AppPaint.Views;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AppPaint.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;

    [ObservableProperty]
 private string _welcomeMessage = "Welcome to AppPaint";

    [ObservableProperty]
 private int _totalTemplates;

    [ObservableProperty]
    private int _totalShapes;

    // Remove navigation service dependency - will navigate via Page events instead
    public MainViewModel(IProfileService profileService)
  {
   _profileService = profileService;
   Title = "AppPaint - Home";
    }

    public override async void OnNavigatedTo(object? parameter = null)
  {
   base.OnNavigatedTo(parameter);
 await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
 {
  try
  {
    IsBusy = true;
   
        // Use scoped service
        using var scope = App.Services.CreateScope();
      var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
      
        var profile = await profileService.GetActiveProfileAsync();
  if (profile != null)
   {
      WelcomeMessage = $"Welcome, {profile.Name}!";
   }
    }
     catch (Exception ex)
   {
ErrorMessage = $"Error loading dashboard: {ex.Message}";
  }
  finally
  {
      IsBusy = false;
        }
 }

    // Navigation will be handled by MainPage code-behind
    public event EventHandler? NavigateToDrawingRequested;
    public event EventHandler? NavigateToTemplatesRequested;
    public event EventHandler? NavigateToProfileRequested;

    [RelayCommand]
    private void NavigateToDrawing()
    {
        NavigateToDrawingRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
 private void NavigateToTemplates()
    {
        NavigateToTemplatesRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void NavigateToProfile()
    {
        NavigateToProfileRequested?.Invoke(this, EventArgs.Empty);
    }
}
