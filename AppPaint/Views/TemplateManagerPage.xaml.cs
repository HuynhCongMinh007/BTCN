using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AppPaint.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Data.Models;

namespace AppPaint.Views;

public sealed partial class TemplateManagerPage : Page
{
    public TemplateManagerViewModel ViewModel { get; }

    public TemplateManagerPage()
    {
        ViewModel = App.Services.GetRequiredService<TemplateManagerViewModel>();
        this.DataContext = ViewModel;
 
    // Subscribe to events
        ViewModel.LoadTemplateRequested += OnLoadTemplateRequested;
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
   ViewModel.LoadTemplateRequested -= OnLoadTemplateRequested;
        ViewModel.NavigateBackRequested -= OnNavigateBackRequested;
        
        ViewModel.OnNavigatedFrom();
    }

    private void OnLoadTemplateRequested(object? sender, int templateId)
    {
      // Navigate to DrawingCanvas with template ID
        Frame.Navigate(typeof(DrawingCanvasPage), templateId);
    }

    private void OnNavigateBackRequested(object? sender, System.EventArgs e)
    {
if (Frame.CanGoBack)
        {
    Frame.GoBack();
      }
    }

    private async void InsertTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DrawingTemplate template)
   {
 // TODO: Implement insert template to current canvas
   // This requires passing template to DrawingCanvasPage
    
      var dialog = new ContentDialog
  {
             XamlRoot = this.XamlRoot,
    Title = "Insert Template",
      Content = $"This will insert '{template.Name}' ({template.Shapes.Count} shapes) to the active canvas.\n\n" +
         "Note: You must have an active drawing canvas open.",
            PrimaryButtonText = "OK",
       CloseButtonText = "Cancel"
   };

            var result = await dialog.ShowAsync();
      
            if (result == ContentDialogResult.Primary)
      {
      // Navigate to DrawingCanvas with template to insert
  // TODO: Implement this functionality
 System.Diagnostics.Debug.WriteLine($"Insert template: {template.Name} with {template.Shapes.Count} shapes");
  }
   }
  }

    private async void DeleteTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DrawingTemplate template)
 {
      await ShowDeleteConfirmationAndDelete(template);
   }
  }

  private async Task ShowDeleteConfirmationAndDelete(DrawingTemplate template)
    {
        var dialog = new ContentDialog
  {
   XamlRoot = this.XamlRoot,
   Title = "Delete Template?",
        Content = $"Are you sure you want to delete template '{template.Name}'?\n\n" +
   $"This will delete {template.Shapes.Count} shape(s).\n\n" +
       $"This action cannot be undone.",
  PrimaryButtonText = "Delete",
     CloseButtonText = "Cancel",
   DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
 {
 // Execute delete command
      await ViewModel.DeleteTemplateCommand.ExecuteAsync(template);

 // Show success message
    var successDialog = new ContentDialog
      {
    XamlRoot = this.XamlRoot,
         Title = "Deleted",
       Content = $"Template '{template.Name}' has been deleted successfully.",
     CloseButtonText = "OK"
       };
        await successDialog.ShowAsync();
        }
  }
}
