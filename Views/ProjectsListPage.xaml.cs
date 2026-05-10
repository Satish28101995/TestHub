using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class ProjectsListPage : ContentPage
{
    private readonly ProjectsListViewModel _vm;

    public ProjectsListPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<ProjectsListViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync(reset: true).ConfigureAwait(true);
    }
}
