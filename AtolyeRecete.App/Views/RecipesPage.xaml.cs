using AtolyeRecete.App.ViewModels;

namespace AtolyeRecete.App.Views;

public partial class RecipesPage : ContentPage
{
    private readonly RecipesViewModel _viewModel;

    public RecipesPage(RecipesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
