using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using AppPaint.Services;

namespace AppPaint.Services;

public interface IThemeService
{
    ElementTheme CurrentTheme { get; }
  Task<ElementTheme> GetSavedThemeAsync();
    Task SetThemeAsync(ElementTheme theme);
    void ApplyTheme(ElementTheme theme);
}

public class ThemeService : IThemeService
{
    private readonly IProfileService _profileService;
    private ElementTheme _currentTheme = ElementTheme.Default;

    public ElementTheme CurrentTheme => _currentTheme;

    public ThemeService(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<ElementTheme> GetSavedThemeAsync()
    {
        try
        {
       var activeProfile = await _profileService.GetActiveProfileAsync();
          if (activeProfile != null)
            {
       return activeProfile.Theme switch
      {
          "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
   _ => ElementTheme.Default
  };
  }
   }
        catch
        {
   // Fallback to default
  }

        return ElementTheme.Default;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
_currentTheme = theme;
        ApplyTheme(theme);

 // Save to active profile
        try
        {
     var activeProfile = await _profileService.GetActiveProfileAsync();
            if (activeProfile != null)
      {
        activeProfile.Theme = theme switch
             {
 ElementTheme.Light => "Light",
        ElementTheme.Dark => "Dark",
             _ => "System"
    };
        await _profileService.UpdateProfileAsync(activeProfile);
            }
 }
        catch
   {
        // Silent fail
  }
    }

    public void ApplyTheme(ElementTheme theme)
    {
     if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
     }
 }
}
