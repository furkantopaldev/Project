using AtolyeRecete.App.ViewModels;

namespace AtolyeRecete.App.Views;

public partial class IngredientsPage : ContentPage
{
    private readonly IngredientsViewModel _viewModel;

    public IngredientsPage(IngredientsViewModel viewModel)
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
