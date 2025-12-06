using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AppPaint.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AppPaint.Views;

public sealed partial class DrawingCanvasPage : Page
{
  public DrawingCanvasViewModel ViewModel { get; }

    public DrawingCanvasPage()
    {
        ViewModel = App.Services.GetRequiredService<DrawingCanvasViewModel>();
   this.DataContext = ViewModel;
   
     // Subscribe to events
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
   ViewModel.NavigateBackRequested -= OnNavigateBackRequested;
        
 ViewModel.OnNavigatedFrom();
    }

    private void OnNavigateBackRequested(object? sender, System.EventArgs e)
    {
 if (Frame.CanGoBack)
   {
  Frame.GoBack();
        }
 }
}
