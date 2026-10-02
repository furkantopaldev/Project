using AtolyeRecete.App.ViewModels;

namespace AtolyeRecete.App.Views;

public partial class RecipeEditPage : ContentPage
{
    private readonly RecipeEditViewModel _viewModel;

    public RecipeEditPage(RecipeEditViewModel viewModel)
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
