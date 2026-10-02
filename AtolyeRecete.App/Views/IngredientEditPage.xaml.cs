using AtolyeRecete.App.ViewModels;

namespace AtolyeRecete.App.Views;

public partial class IngredientEditPage : ContentPage
{
    private readonly IngredientEditViewModel _viewModel;

    public IngredientEditPage(IngredientEditViewModel viewModel)
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
