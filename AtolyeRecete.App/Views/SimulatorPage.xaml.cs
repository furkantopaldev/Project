using AtolyeRecete.App.ViewModels;

namespace AtolyeRecete.App.Views;

public partial class SimulatorPage : ContentPage
{
    private readonly SimulatorViewModel _viewModel;

    public SimulatorPage(SimulatorViewModel viewModel)
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
