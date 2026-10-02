using System.Collections.ObjectModel;
using AtolyeRecete.App.Services;
using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

public partial class RecipeEditViewModel(
    StartupService startup,
    AppSettings settings,
    IRecipeRepository recipes,
    IIngredientRepository ingredients) : ObservableObject, IQueryAttributable
{
    private int _id;
    private IReadOnlyList<Ingredient> _allIngredients = Array.Empty<Ingredient>();

    /// <summary>Yükleme sırasında her alan değişiminde yeniden hesaplamayı önler.</summary>
    private bool _loading;

    public bool IsExisting => _id != 0;
    public string PageTitle => IsExisting ? "Reçeteyi düzenle" : "Yeni reçete";

    public ObservableCollection<RecipeItemViewModel> Items { get; } = new();

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _yieldText = "1";
    [ObservableProperty] private string _laborMinutesText = "0";
    [ObservableProperty] private string _overheadPercentText = "10";

    /// <summary>Satış fiyatı (₺, KDV hariç). Boş bırakılabilir.</summary>
    [ObservableProperty] private string _salePriceText = "";

    [ObservableProperty] private string _marginText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMarginWarning))]
    private string _marginWarning = "";

    public bool HasMarginWarning => !string.IsNullOrEmpty(MarginWarning);

    // Canlı maliyet özeti
    [ObservableProperty] private string _materialCostText = "";
    [ObservableProperty] private string _laborCostText = "";
    [ObservableProperty] private string _overheadCostText = "";
    [ObservableProperty] private string _batchCostText = "";
    [ObservableProperty] private string _unitCostText = "";
    [ObservableProperty] private string _hourlyRateText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = "";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnYieldTextChanged(string value) => Recalculate();
    partial void OnLaborMinutesTextChanged(string value) => Recalculate();
    partial void OnOverheadPercentTextChanged(string value) => Recalculate();
    partial void OnSalePriceTextChanged(string value) => Recalculate();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _id = query.TryGetValue("id", out var raw) && int.TryParse(raw?.ToString(), out var id) ? id : 0;
        OnPropertyChanged(nameof(IsExisting));
        OnPropertyChanged(nameof(PageTitle));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await startup.EnsureInitializedAsync();

        _loading = true;
        try
        {
            ErrorMessage = "";
            _allIngredients = await ingredients.GetAllAsync();
            HourlyRateText = $"Saatlik işçilik ücreti: {Display.Money(settings.HourlyRate)}";
            Items.Clear();

            if (!IsExisting)
            {
                Name = "";
                YieldText = "1";
                LaborMinutesText = "0";
                OverheadPercentText = Display.NumberText(settings.DefaultOverheadPercentValue);
                SalePriceText = "";
                return;
            }

            var recipe = await recipes.GetAsync(_id);
            if (recipe is null)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            Name = recipe.Name;
            YieldText = Display.NumberText(recipe.Yield);
            LaborMinutesText = Display.NumberText(recipe.LaborMinutes);
            OverheadPercentText = Display.PercentText(recipe.OverheadRate);
            SalePriceText = recipe.SalePrice is { } price ? Display.NumberText(price) : "";

            foreach (var item in recipe.Items)
            {
                var row = NewRow();
                // Picker referans eşitliğiyle seçer: listedeki aynı hammadde örneğini bul.
                row.SelectedIngredient = _allIngredients.FirstOrDefault(i => i.Id == item.Ingredient.Id);
                row.QuantityText = Display.NumberText(item.Quantity);
                row.WasteText = Display.PercentText(item.WasteRate);
                Items.Add(row);
            }
        }
        finally
        {
            _loading = false;
            Recalculate();
        }
    }

    [RelayCommand]
    private void AddItem()
    {
        Items.Add(NewRow());
        Recalculate();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var error = Validate(out var yield, out var labor, out var overheadPercent);
        if (error is not null)
        {
            ErrorMessage = error;
            return;
        }

        var recipe = new Recipe
        {
            Id = _id,
            Name = Name.Trim(),
            Yield = yield,
            LaborMinutes = labor,
            OverheadRate = Display.RateFromPercent(overheadPercent),
            SalePrice = Display.TryParseDecimal(SalePriceText, out var salePrice) ? salePrice : null, // doğrulandı: boş ya da > 0
            Items = Items.Select(i => i.ToPreviewModel()!).ToList()
        };

        await recipes.SaveAsync(recipe);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsExisting) return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Reçeteyi sil", $"'{Name}' silinsin mi?", "Sil", "Vazgeç");
        if (!confirmed) return;

        await recipes.DeleteAsync(_id);
        await Shell.Current.GoToAsync("..");
    }

    private RecipeItemViewModel NewRow() => new(_allIngredients, Recalculate, RemoveRow);

    private void RemoveRow(RecipeItemViewModel row)
    {
        Items.Remove(row);
        Recalculate();
    }

    /// <summary>Kaydetmeden önce sıkı doğrulama.</summary>
    private string? Validate(out decimal yield, out decimal labor, out decimal overheadPercent)
    {
        labor = 0;
        overheadPercent = 0;

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield = 0;
            return "Reçete adı boş olamaz.";
        }
        if (!Display.TryParseDecimal(YieldText, out yield) || yield <= 0)
            return "Verim sıfırdan büyük bir sayı olmalı.";
        if (!Display.TryParseDecimal(LaborMinutesText, out labor) || labor < 0)
            return "İşçilik süresi geçerli bir sayı olmalı (0 veya daha büyük).";
        if (!Display.TryParseDecimal(OverheadPercentText, out overheadPercent) || overheadPercent < 0)
            return "Genel gider % geçerli bir sayı olmalı (0 veya daha büyük).";
        if (!string.IsNullOrWhiteSpace(SalePriceText)
            && (!Display.TryParseDecimal(SalePriceText, out var salePrice) || salePrice <= 0))
            return "Satış fiyatı boş bırakılabilir; girilirse sıfırdan büyük bir sayı olmalı.";
        if (Items.Count == 0)
            return "Reçetede en az bir kalem olmalı.";

        foreach (var item in Items)
        {
            var itemError = item.Validate();
            if (itemError is not null) return itemError;
        }
        return null;
    }

    /// <summary>Satış fiyatı girildiyse bu fiyattaki marjı gösterir; eşiğin altındaysa uyarır.</summary>
    private void UpdateMargin(decimal? unitCost)
    {
        MarginText = "";
        MarginWarning = "";

        if (unitCost is not { } cost
            || !Display.TryParseDecimal(SalePriceText, out var salePrice)
            || salePrice <= 0)
            return;

        var margin = PricingCalculator.MarginAtPrice(cost, salePrice);
        MarginText = $"Bu fiyatla marj: {Display.PercentLabel(margin)} · kâr: {Display.Money(salePrice - cost)} (komisyon öncesi)";

        var threshold = settings.MarginWarningPercentValue;
        if (MarginAlerts.IsBelowThreshold(margin, Display.RateFromPercent(threshold)))
            MarginWarning = margin < 0m
                ? "Satış fiyatı maliyetin altında: zararına satış."
                : $"Marj, uyarı eşiğinin (%{Display.NumberText(threshold)}) altında.";
    }

    /// <summary>Girdilerden canlı bir Recipe kurar (geçersiz sayılar 0) ve Core ile hesaplar.</summary>
    private void Recalculate()
    {
        if (_loading) return;

        ErrorMessage = ""; // kullanıcı bir şeyi değiştirdi, eski uyarı geçersiz

        foreach (var item in Items) item.RefreshCost();

        var recipe = new Recipe
        {
            Name = Name,
            Yield = Display.ParseOrZero(YieldText),
            LaborMinutes = Display.ParseOrZero(LaborMinutesText),
            OverheadRate = Display.RateFromPercent(Display.ParseOrZero(OverheadPercentText)),
            Items = Items.Select(i => i.ToPreviewModel()).OfType<RecipeItem>().ToList()
        };
        var rate = settings.HourlyRate;

        MaterialCostText = Display.Money(CostCalculator.MaterialCost(recipe));
        LaborCostText = Display.Money(CostCalculator.LaborCost(recipe, rate));
        OverheadCostText = Display.Money(CostCalculator.OverheadCost(recipe, rate));
        BatchCostText = Display.Money(CostCalculator.BatchCost(recipe, rate));

        decimal? unitCost = null;
        try
        {
            unitCost = CostCalculator.UnitCost(recipe, rate);
            UnitCostText = Display.Money(unitCost.Value);
        }
        catch (InvalidOperationException)
        {
            // Verim 0 veya geçersiz: birim maliyet hesaplanamaz.
            UnitCostText = "—";
        }

        UpdateMargin(unitCost);
    }
}
