using AtolyeRecete.App.Services;
using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

/// <summary>Reçete düzenleme ekranındaki bir kalem satırı.</summary>
public partial class RecipeItemViewModel : ObservableObject
{
    private readonly Action _changed;
    private readonly Action<RecipeItemViewModel> _remove;

    public RecipeItemViewModel(
        IReadOnlyList<Ingredient> ingredients, Action changed, Action<RecipeItemViewModel> remove)
    {
        Ingredients = ingredients;
        _changed = changed;
        _remove = remove;
    }

    /// <summary>Picker'ın seçenekleri (tüm satırlar aynı listeyi paylaşır).</summary>
    public IReadOnlyList<Ingredient> Ingredients { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnitLabel))]
    private Ingredient? _selectedIngredient;

    [ObservableProperty]
    private string _quantityText = "";

    /// <summary>Kalem fire oranı, yüzde olarak girilir.</summary>
    [ObservableProperty]
    private string _wasteText = "0";

    [ObservableProperty]
    private string _costText = "";

    public string UnitLabel => SelectedIngredient is null ? "" : Display.UnitLabel(SelectedIngredient.Unit);

    partial void OnSelectedIngredientChanged(Ingredient? value) => _changed();
    partial void OnQuantityTextChanged(string value) => _changed();
    partial void OnWasteTextChanged(string value) => _changed();

    [RelayCommand]
    private void Remove() => _remove(this);

    /// <summary>Canlı önizleme için model; hammadde seçilmediyse null, geçersiz sayılar 0.</summary>
    public RecipeItem? ToPreviewModel() =>
        SelectedIngredient is null
            ? null
            : new RecipeItem
            {
                Ingredient = SelectedIngredient,
                Quantity = Display.ParseOrZero(QuantityText),
                WasteRate = Display.RateFromPercent(Display.ParseOrZero(WasteText))
            };

    public void RefreshCost()
    {
        var model = ToPreviewModel();
        CostText = model is null ? "" : Display.Money(CostCalculator.ItemCost(model));
    }

    /// <summary>Kaydetmeden önce sıkı doğrulama; hata varsa mesaj döner.</summary>
    public string? Validate()
    {
        if (SelectedIngredient is null)
            return "Her kalem için bir hammadde seçilmeli.";
        if (!Display.TryParseDecimal(QuantityText, out var quantity) || quantity <= 0)
            return $"'{SelectedIngredient.Name}' için miktar sıfırdan büyük bir sayı olmalı.";
        if (!Display.TryParseDecimal(WasteText, out var waste) || waste < 0)
            return $"'{SelectedIngredient.Name}' için fire % geçerli bir sayı olmalı (0 veya daha büyük).";
        return null;
    }
}
