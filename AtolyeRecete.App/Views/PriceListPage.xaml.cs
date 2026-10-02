using AtolyeRecete.App.ViewModels;

namespace AtolyeRecete.App.Views;

public partial class PriceListPage : ContentPage
{
    private readonly PriceListViewModel _viewModel;

    public PriceListPage(PriceListViewModel viewModel)
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
