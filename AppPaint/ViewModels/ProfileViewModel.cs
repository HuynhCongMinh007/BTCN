using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppPaint.Services;
using Data.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AppPaint.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    [ObservableProperty]
 private ObservableCollection<Profile> _profiles = new();

  [ObservableProperty]
    private Profile? _selectedProfile;

 [ObservableProperty]
    private Profile? _activeProfile;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
 private string _profileName = string.Empty;

    [ObservableProperty]
 private string _theme = "System";

    [ObservableProperty]
    private double _canvasWidth = 800;

    [ObservableProperty]
 private double _canvasHeight = 600;

    // Navigation events
    public event EventHandler? NavigateBackRequested;

    public ProfileViewModel()
    {
    Title = "Profile Manager";
    }

 public override async void OnNavigatedTo(object? parameter = null)
    {
   base.OnNavigatedTo(parameter);
   await LoadProfilesAsync();
    }

    [RelayCommand]
    private async Task LoadProfilesAsync()
  {
      try
   {
  IsBusy = true;
      
        using var scope = App.Services.CreateScope();
   var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        
   var profiles = await profileService.GetAllProfilesAsync();
   Profiles.Clear();
       foreach (var profile in profiles)
{
   Profiles.Add(profile);
   if (profile.IsActive)
     {
   ActiveProfile = profile;
 }
   }
  }
   catch (Exception ex)
   {
   ErrorMessage = $"Error loading profiles: {ex.Message}";
  }
 finally
        {
IsBusy = false;
     }
  }

 [RelayCommand]
    private async Task CreateProfileAsync()
    {
   try
   {
IsBusy = true;

   using var scope = App.Services.CreateScope();
     var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        
var newProfile = new Profile
 {
       Name = ProfileName,
Theme = Theme,
     DefaultCanvasWidth = CanvasWidth,
       DefaultCanvasHeight = CanvasHeight
  };

   await profileService.CreateProfileAsync(newProfile);
  await LoadProfilesAsync();
   ClearForm();
   }
 catch (Exception ex)
      {
       ErrorMessage = $"Error creating profile: {ex.Message}";
      }
   finally
  {
      IsBusy = false;
   }
    }

    [RelayCommand]
    private async Task UpdateProfileAsync()
    {
   if (SelectedProfile == null) return;

   try
   {
 IsBusy = true;

        using var scope = App.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        
   SelectedProfile.Name = ProfileName;
   SelectedProfile.Theme = Theme;
    SelectedProfile.DefaultCanvasWidth = CanvasWidth;
       SelectedProfile.DefaultCanvasHeight = CanvasHeight;

   await profileService.UpdateProfileAsync(SelectedProfile);
     await LoadProfilesAsync();
   ClearForm();
      }
        catch (Exception ex)
 {
   ErrorMessage = $"Error updating profile: {ex.Message}";
 }
   finally
   {
 IsBusy = false;
   }
    }

    [RelayCommand]
 private async Task DeleteProfileAsync(Profile? profile)
    {
      if (profile == null) return;

    try
   {
     IsBusy = true;

        using var scope = App.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        
   var success = await profileService.DeleteProfileAsync(profile.Id);
   if (success)
   {
    Profiles.Remove(profile);
   }
   else
   {
      ErrorMessage = "Cannot delete active profile";
  }
        }
   catch (Exception ex)
   {
       ErrorMessage = $"Error deleting profile: {ex.Message}";
   }
   finally
 {
   IsBusy = false;
      }
    }

    [RelayCommand]
 private async Task SetActiveProfileAsync(Profile? profile)
    {
   if (profile == null) return;

   try
   {
      IsBusy = true;

        using var scope = App.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
     
     var success = await profileService.SetActiveProfileAsync(profile.Id);
   if (success)
   {
   await LoadProfilesAsync();
}
   }
      catch (Exception ex)
        {
 ErrorMessage = $"Error setting active profile: {ex.Message}";
        }
   finally
 {
     IsBusy = false;
   }
 }

    [RelayCommand]
    private void EditProfile(Profile? profile)
    {
   if (profile == null) return;

   SelectedProfile = profile;
    ProfileName = profile.Name;
   Theme = profile.Theme;
        CanvasWidth = profile.DefaultCanvasWidth;
   CanvasHeight = profile.DefaultCanvasHeight;
   IsEditMode = true;
    }

 private void ClearForm()
    {
ProfileName = string.Empty;
   Theme = "System";
      CanvasWidth = 800;
        CanvasHeight = 600;
   IsEditMode = false;
   SelectedProfile = null;
    }

    [RelayCommand]
    private void GoBack()
 {
        NavigateBackRequested?.Invoke(this, EventArgs.Empty);
    }
}
