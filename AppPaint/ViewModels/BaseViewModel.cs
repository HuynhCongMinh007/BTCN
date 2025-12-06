using CommunityToolkit.Mvvm.ComponentModel;

namespace AppPaint.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
 private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public virtual void OnNavigatedTo(object? parameter = null)
    {
        // Override in derived classes
    }

    public virtual void OnNavigatedFrom()
  {
        // Override in derived classes
    }
}
