using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AppPaint.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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
}
