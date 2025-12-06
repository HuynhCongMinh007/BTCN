using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppPaint.Services;
using Data.Models;
using System;
using System.Collections.ObjectModel;
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
   await LoadTemplatesAsync();
    }

    [RelayCommand]
    private async Task LoadTemplatesAsync()
    {
   try
   {
      IsBusy = true;
   
   // Use scoped service
  using var scope = App.Services.CreateScope();
        var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
        
   var templates = await templateService.GetAllTemplatesAsync();
   Templates.Clear();
       foreach (var template in templates)
{
Templates.Add(template);
   }
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
