using System.Collections.ObjectModel;
using AtolyeRecete.App.Services;
using AtolyeRecete.Core;
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

public record RecipeListItem(int Id, string Name, string DetailText, string UnitCostText);

public partial class RecipesViewModel(
    StartupService startup,
    AppSettings settings,
    IRecipeRepository repository) : ObservableObject
{
    public ObservableCollection<RecipeListItem> Items { get; } = new();

    [ObservableProperty]
    private RecipeListItem? _selectedItem;

    partial void OnSelectedItemChanged(RecipeListItem? value)
    {
        if (value is null) return;
        SelectedItem = null; // aynı satıra tekrar dokunulabilsin
        EditCommand.Execute(value.Id);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await startup.EnsureInitializedAsync();

        Items.Clear();
        foreach (var r in await repository.GetAllAsync())
        {
            // Hesap Core'da; burada sadece biçimlendirme.
            var unitCost = CostCalculator.UnitCost(r, settings.HourlyRate);
            Items.Add(new RecipeListItem(
                r.Id,
                r.Name,
                $"{r.Items.Count} kalem · verim {Display.NumberText(r.Yield)}",
                Display.Money(unitCost)));
        }
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync("RecipeEditPage?id=0");

    [RelayCommand]
    private Task EditAsync(int id) => Shell.Current.GoToAsync($"RecipeEditPage?id={id}");
}
