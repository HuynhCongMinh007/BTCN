using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppPaint.Services;
using Data.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    private string _selectedProfileTheme = "System";

    [ObservableProperty]
  private bool _hasSelectedProfile;

   [ObservableProperty]
    private bool _canDeleteProfile;

    [ObservableProperty]
    private bool _canSetActiveProfile;

    // Navigation events
 public event EventHandler? NavigateBackRequested;
  public event EventHandler<string>? SaveProfileSuccess; // ✅ Event with profile name

    public ProfileViewModel()
  {
   Title = "Profile Settings";
}

    public override async void OnNavigatedTo(object? parameter = null)
    {
        base.OnNavigatedTo(parameter);
 await LoadProfilesAsync();
    }

    partial void OnSelectedProfileChanged(Profile? value)
  {
        // Update UI states
  HasSelectedProfile = value != null;
     CanDeleteProfile = value != null && !value.IsActive && Profiles.Count > 1;
   CanSetActiveProfile = value != null && !value.IsActive;
   
        // Update theme binding
        if (value != null)
      {
     SelectedProfileTheme = value.Theme;
        }
    }

    partial void OnSelectedProfileThemeChanged(string value)
 {
    if (SelectedProfile != null && SelectedProfile.Theme != value)
        {
 SelectedProfile.Theme = value;
   }
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
       
            Profile? activeProfile = null;
 foreach (var profile in profiles)
      {
      Profiles.Add(profile);
   if (profile.IsActive)
     {
      activeProfile = profile;
     }
    }

    // Auto-select active profile or first profile
  SelectedProfile = activeProfile ?? Profiles.FirstOrDefault();
   
      System.Diagnostics.Debug.WriteLine($"✅ Loaded {Profiles.Count} profiles");
        }
    catch (Exception ex)
        {
   ErrorMessage = $"Error loading profiles: {ex.Message}";
 System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex}");
        }
   finally
  {
  IsBusy = false;
     }
    }

    /// <summary>
    /// Create new profile
    /// </summary>
    public async Task CreateProfileAsync(string name)
    {
try
      {
            IsBusy = true;

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
   IsActive = Profiles.Count == 0, // First profile is active by default
        CreatedAt = DateTime.Now
            };

var createdProfile = await profileService.CreateProfileAsync(newProfile);
       Profiles.Add(createdProfile);
 SelectedProfile = createdProfile;

    System.Diagnostics.Debug.WriteLine($"✅ Created profile: {name}");
        }
     catch (Exception ex)
   {
        ErrorMessage = $"Error creating profile: {ex.Message}";
    System.Diagnostics.Debug.WriteLine($"Error creating profile: {ex}");
        }
        finally
        {
  IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
  {
 if (SelectedProfile == null) return;

try
 {
 IsBusy = true;

  using var scope = App.Services.CreateScope();
   var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

    await profileService.UpdateProfileAsync(SelectedProfile);
  
     System.Diagnostics.Debug.WriteLine($"✅ Saved profile: {SelectedProfile.Name}");
   System.Diagnostics.Debug.WriteLine($"   ⚠️ All drawings using this profile will use updated settings on next load");
  
  // ✅ Trigger success event with profile name
            SaveProfileSuccess?.Invoke(this, SelectedProfile.Name);
    
         // Reload to get fresh data
     await LoadProfilesAsync();
        }
        catch (Exception ex)
   {
     ErrorMessage = $"Error saving profile: {ex.Message}";
    System.Diagnostics.Debug.WriteLine($"Error saving profile: {ex}");
      }
   finally
  {
   IsBusy = false;
    }
  }

[RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null) return;
  
  // Cannot delete active profile
  if (SelectedProfile.IsActive)
{
   ErrorMessage = "Cannot delete the active profile. Please set another profile as active first.";
    return;
        }

        // Cannot delete if only one profile
   if (Profiles.Count <= 1)
        {
        ErrorMessage = "Cannot delete the last profile. At least one profile must exist.";
     return;
        }

   try
    {
            IsBusy = true;

      using var scope = App.Services.CreateScope();
  var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

       var success = await profileService.DeleteProfileAsync(SelectedProfile.Id);
    if (success)
   {
   var deletedProfile = SelectedProfile;
      Profiles.Remove(deletedProfile);
  SelectedProfile = Profiles.FirstOrDefault();
         
          System.Diagnostics.Debug.WriteLine($"✅ Deleted profile: {deletedProfile.Name}");
   }
   }
        catch (Exception ex)
  {
          ErrorMessage = $"Error deleting profile: {ex.Message}";
       System.Diagnostics.Debug.WriteLine($"Error deleting profile: {ex}");
    }
        finally
  {
    IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SetActiveProfileAsync()
    {
        if (SelectedProfile == null) return;

        try
{
         IsBusy = true;

  using var scope = App.Services.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

    await profileService.SetActiveProfileAsync(SelectedProfile.Id);
      
          // Update all profiles' IsActive status
      foreach (var profile in Profiles)
            {
  profile.IsActive = profile.Id == SelectedProfile.Id;
    }

     System.Diagnostics.Debug.WriteLine($"✅ Set active profile: {SelectedProfile.Name}");
     
        // Reload to refresh UI
await LoadProfilesAsync();
        }
   catch (Exception ex)
  {
            ErrorMessage = $"Error setting active profile: {ex.Message}";
        System.Diagnostics.Debug.WriteLine($"Error setting active profile: {ex}");
    }
   finally
  {
   IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateBackRequested?.Invoke(this, EventArgs.Empty);
}
}
