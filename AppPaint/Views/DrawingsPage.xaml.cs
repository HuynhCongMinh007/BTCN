using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using AppPaint.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Data.Models;
using System;
using System.Threading.Tasks;

namespace AppPaint.Views;

public sealed partial class DrawingsPage : Page
{
    public TemplateManagerViewModel ViewModel { get; }

    public DrawingsPage()
    {
        ViewModel = App.Services.GetRequiredService<TemplateManagerViewModel>();
   this.DataContext = ViewModel;
        
        // Subscribe to events
        ViewModel.LoadTemplateRequested += OnLoadTemplateRequested;
        
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
        
        ViewModel.OnNavigatedFrom();
    }

    private void OnLoadTemplateRequested(object? sender, int templateId)
    {
     // Navigate using Shell's main frame
        NavigateToDrawingCanvas(templateId);
    }

    private void LoadDrawing_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DrawingTemplate drawing)
        {
       // Navigate using Shell's main frame
            NavigateToDrawingCanvas(drawing.Id);
        }
    }

    private async void DeleteDrawing_Click(object sender, RoutedEventArgs e)
    {
   if (sender is Button button && button.Tag is DrawingTemplate drawing)
        {
            await ShowDeleteConfirmationAndDelete(drawing);
 }
    }

    private async Task ShowDeleteConfirmationAndDelete(DrawingTemplate drawing)
    {
        var dialog = new ContentDialog
  {
  XamlRoot = this.XamlRoot,
        Title = "Delete Drawing?",
          Content = $"Are you sure you want to delete '{drawing.Name}'?\n\n" +
    $"This will delete {drawing.Shapes.Count} shape(s).\n\n" +
   $"This action cannot be undone.",
     PrimaryButtonText = "Delete",
      CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
};

 var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
     // Execute delete command
        await ViewModel.DeleteTemplateCommand.ExecuteAsync(drawing);
     
            // Show success message
var successDialog = new ContentDialog
  {
 XamlRoot = this.XamlRoot,
      Title = "Deleted",
         Content = $"'{drawing.Name}' has been deleted successfully.",
       CloseButtonText = "OK"
   };
            await successDialog.ShowAsync();
   }
    }

    private void NavigateToDrawingCanvas(int? drawingId)
    {
 // Get MainWindow via static property
        if (App.MainWindow is MainWindow mainWindow)
        {
        // Get ShellPage from RootFrame
   if (mainWindow.Content is Frame rootFrame && rootFrame.Content is ShellPage shellPage)
   {
    // Use ShellPage's public method
    shellPage.NavigateToDrawingCanvas(drawingId);
}
     }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
      await ViewModel.LoadTemplatesCommand.ExecuteAsync(null);
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
   if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
  {
       string tag = item.Tag?.ToString() ?? "Date";
            
    // TODO: Implement sorting logic
            System.Diagnostics.Debug.WriteLine($"Sort by: {tag}");
        }
}
}
