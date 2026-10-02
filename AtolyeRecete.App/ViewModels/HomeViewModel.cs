using System.Collections.ObjectModel;
using AtolyeRecete.App.Services;
using AtolyeRecete.Core;
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

/// <summary>Satır gösterimi: ad ve biçimlenmiş birim maliyet.</summary>
public record RecipeSummary(string Name, string UnitCostText);

public partial class HomeViewModel(
    StartupService startup,
    IIngredientRepository ingredients,
    IRecipeRepository recipes) : ObservableObject
{
    [ObservableProperty]
    private int _ingredientCount;

    [ObservableProperty]
    private int _recipeCount;

    public ObservableCollection<RecipeSummary> Recipes { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        await startup.EnsureInitializedAsync();

        IngredientCount = (await ingredients.GetAllAsync()).Count;

        var all = await recipes.GetAllAsync();
        RecipeCount = all.Count;

        Recipes.Clear();
        foreach (var recipe in all)
        {
            // Hesap Core'da; burada sadece biçimlendirme.
            var unitCost = Money.Round(CostCalculator.UnitCost(recipe, AppDefaults.HourlyRate));
            Recipes.Add(new RecipeSummary(recipe.Name, unitCost.ToString("C2", AppDefaults.Culture)));
        }
    }
}
