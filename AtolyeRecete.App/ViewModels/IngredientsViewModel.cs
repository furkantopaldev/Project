using System.Collections.ObjectModel;
using AtolyeRecete.App.Services;
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

/// <summary>Liste satırı: hazır biçimlenmiş metinlerle.</summary>
public record IngredientItem(int Id, string Name, string PurchaseText, string UnitPriceText);

public partial class IngredientsViewModel(
    StartupService startup,
    IIngredientRepository repository) : ObservableObject
{
    public ObservableCollection<IngredientItem> Items { get; } = new();

    [ObservableProperty]
    private IngredientItem? _selectedItem;

    partial void OnSelectedItemChanged(IngredientItem? value)
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
        foreach (var i in await repository.GetAllAsync())
        {
            var unit = Display.UnitLabel(i.Unit);
            Items.Add(new IngredientItem(
                i.Id,
                i.Name,
                $"{Display.Money(i.PurchasePrice)} / {i.PurchaseQuantity:0.##} {unit}",
                $"{Display.UnitPrice(i.UnitPrice)} / {unit}"));
        }
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync("IngredientEditPage?id=0");

    [RelayCommand]
    private Task EditAsync(int id) => Shell.Current.GoToAsync($"IngredientEditPage?id={id}");
}
