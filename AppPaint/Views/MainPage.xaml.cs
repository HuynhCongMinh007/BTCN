using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using AppPaint.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AppPaint.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        this.DataContext = ViewModel;

        // Subscribe to navigation events
        ViewModel.NavigateToDrawingRequested += OnNavigateToDrawing;
        ViewModel.NavigateToTemplatesRequested += OnNavigateToTemplates;
        ViewModel.NavigateToProfileRequested += OnNavigateToProfile;

        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.OnNavigatedTo(e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        // Unsubscribe to prevent memory leaks
        ViewModel.NavigateToDrawingRequested -= OnNavigateToDrawing;
        ViewModel.NavigateToTemplatesRequested -= OnNavigateToTemplates;
        ViewModel.NavigateToProfileRequested -= OnNavigateToProfile;

        ViewModel.OnNavigatedFrom();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Start entrance animation
        PageEntranceAnimation.Begin();
    }

    private void OnNavigateToDrawing(object? sender, System.EventArgs e)
    {
        // Navigate using parent Frame
        Frame.Navigate(typeof(DrawingCanvasPage));
    }

    private void OnNavigateToTemplates(object? sender, System.EventArgs e)
    {
        Frame.Navigate(typeof(TemplateManagerPage));
    }

    private void OnNavigateToProfile(object? sender, System.EventArgs e)
    {
        Frame.Navigate(typeof(ProfilePage));
    }
}
