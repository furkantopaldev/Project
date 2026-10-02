using System.Collections.ObjectModel;
using AtolyeRecete.App.Services;
using AtolyeRecete.Core;
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

/// <summary>Ana sayfa tablosu satırı. Satış fiyatı girilmemişse fiyat ve marj "—" gösterir.</summary>
public record RecipeSummary(string Name, string UnitCostText, string SalePriceText, string MarginText, bool IsLow);

public record AttentionItem(string Text);

public partial class HomeViewModel(
    StartupService startup,
    AppSettings settings,
    IIngredientRepository ingredients,
    IRecipeRepository recipes) : ObservableObject
{
    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private int _ingredientCount;
    [ObservableProperty] private int _recipeCount;
    [ObservableProperty] private string _priceListText = "";
    [ObservableProperty] private string _hourlyRateText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecipes))]
    [NotifyPropertyChangedFor(nameof(ShowAllGood))]
    private bool _hasRecipesFlag;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAttention))]
    [NotifyPropertyChangedFor(nameof(ShowAllGood))]
    private int _attentionCount;

    public bool HasRecipes => HasRecipesFlag;
    public bool HasAttention => AttentionCount > 0;
    public bool ShowAllGood => HasRecipesFlag && AttentionCount == 0;
    public string ThresholdText => $"Marj uyarı eşiği: %{Display.NumberText(settings.MarginWarningPercentValue)}";

    public ObservableCollection<RecipeSummary> Recipes { get; } = new();
    public ObservableCollection<AttentionItem> Attention { get; } = new();

    [RelayCommand]
    private Task OpenSettingsAsync() => Shell.Current.GoToAsync("SettingsPage");

    [RelayCommand]
    private async Task LoadAsync()
    {
        await startup.EnsureInitializedAsync();

        var businessName = settings.BusinessName;
        Subtitle = string.IsNullOrWhiteSpace(businessName)
            ? "Maliyet, fire ve pazaryeri fiyat motoru"
            : businessName;

        IngredientCount = (await ingredients.GetAllAsync()).Count;
        var all = await recipes.GetAllAsync();
        RecipeCount = all.Count;
        HasRecipesFlag = all.Count > 0;
        HourlyRateText = $"{Display.Money(settings.HourlyRate)} / saat";
        OnPropertyChanged(nameof(ThresholdText));

        var thresholdRate = Display.RateFromPercent(settings.MarginWarningPercentValue);
        var pricedCount = 0;

        Recipes.Clear();
        Attention.Clear();
        foreach (var recipe in all)
        {
            // Hesap ve "düşük marj" kuralı Core'da; burada sadece biçimlendirme.
            var unitCost = CostCalculator.UnitCost(recipe, settings.HourlyRate);

            if (recipe.SalePrice is { } salePrice)
            {
                pricedCount++;
                var margin = PricingCalculator.MarginAtPrice(unitCost, salePrice);
                var low = MarginAlerts.IsBelowThreshold(margin, thresholdRate);

                Recipes.Add(new RecipeSummary(
                    recipe.Name, Display.Money(unitCost), Display.Money(salePrice), Display.PercentLabel(margin), low));

                if (low)
                    Attention.Add(new AttentionItem(
                        $"⚠ {recipe.Name}: marj {Display.PercentLabel(margin)} (eşik %{Display.NumberText(settings.MarginWarningPercentValue)})"));
            }
            else
            {
                Recipes.Add(new RecipeSummary(recipe.Name, Display.Money(unitCost), "—", "—", false));
                Attention.Add(new AttentionItem(
                    $"{recipe.Name}: satış fiyatı girilmemiş (fiyat listesine girmez)"));
            }
        }

        AttentionCount = Attention.Count;
        PriceListText = $"{pricedCount} / {all.Count}";
    }
}
