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

public partial class TemplateManagerViewModel : BaseViewModel
{
    private readonly ITemplateService _templateService;

  [ObservableProperty]
    private ObservableCollection<DrawingTemplate> _templates = new();

    [ObservableProperty]
    private DrawingTemplate? _selectedTemplate;

    // NEW: Filter mode
    [ObservableProperty]
    private bool _showTemplatesOnly = true; // true = templates, false = drawings

    // Navigation events
    public event EventHandler<int>? LoadTemplateRequested;
   public event EventHandler? NavigateBackRequested;

    public TemplateManagerViewModel(ITemplateService templateService)
    {
        _templateService = templateService;
     Title = "Template Manager";
    }

    public override async void OnNavigatedTo(object? parameter = null)
    {
    base.OnNavigatedTo(parameter);
   
        // Check if parameter specifies filter mode
 if (parameter is bool showTemplatesOnly)
        {
         ShowTemplatesOnly = showTemplatesOnly;
        }
        
      await LoadTemplatesAsync();
    }

    [RelayCommand]
 private async Task LoadTemplatesAsync()
    {
        try
 {
          IsBusy = true;

          using var scope = App.Services.CreateScope();
     var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();

            var templates = await templateService.GetAllTemplatesAsync();

       // Filter based on mode
            Templates.Clear();
      foreach (var template in templates.Where(t => t.IsTemplate == ShowTemplatesOnly))
  {
          Templates.Add(template);
            }

     string mode = ShowTemplatesOnly ? "templates (IsTemplate=true)" : "drawings (IsTemplate=false)";
 System.Diagnostics.Debug.WriteLine($"✅ Loaded {Templates.Count} {mode}");
    }
        catch (Exception ex)
        {
     ErrorMessage = $"Error loading templates: {ex.Message}";
    }
      finally
        {
         IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefreshTemplates))
]
    private async Task RefreshTemplatesAsync()
    {
 await LoadTemplatesAsync();
    }

    private bool CanRefreshTemplates() => !IsBusy;

    [RelayCommand]
  private async Task DeleteTemplateAsync(DrawingTemplate? template)
    {
     if (template == null) return;

     try
        {
            IsBusy = true;

     using var scope = App.Services.CreateScope();
         var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();

          var success = await templateService.DeleteTemplateAsync(template.Id);
      if (success)
     {
      Templates.Remove(template);
  System.Diagnostics.Debug.WriteLine($"✅ Deleted template: {template.Name}");
         }
        }
        catch (Exception ex)
        {
      ErrorMessage = $"Error deleting template: {ex.Message}";
        }
 finally
        {
    IsBusy = false;
        }
    }

    [RelayCommand]
    private void LoadTemplate(DrawingTemplate? template)
    {
 if (template == null) return;
        LoadTemplateRequested?.Invoke(this, template.Id);
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateBackRequested?.Invoke(this, EventArgs.Empty);
    }
}
