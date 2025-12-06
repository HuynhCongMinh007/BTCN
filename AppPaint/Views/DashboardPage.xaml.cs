using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AppPaint.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AppPaint.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
    ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
  this.DataContext = ViewModel;
        
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
        ViewModel.OnNavigatedFrom();
    }
}
