using AtolyeRecete.App.Services;
using AtolyeRecete.Core.Models;
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

public record UnitOption(UnitOfMeasure Unit, string Label);

public partial class IngredientEditViewModel(IIngredientRepository repository)
    : ObservableObject, IQueryAttributable
{
    public IReadOnlyList<UnitOption> Units { get; } =
        Enum.GetValues<UnitOfMeasure>().Select(u => new UnitOption(u, Display.UnitLabel(u))).ToList();

    private int _id;

    public bool IsExisting => _id != 0;
    public string PageTitle => IsExisting ? "Hammadde düzenle" : "Yeni hammadde";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private UnitOption? _selectedUnit;

    [ObservableProperty]
    private string _priceText = "";

    [ObservableProperty]
    private string _quantityText = "";

    [ObservableProperty]
    private string _unitPricePreview = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = "";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnPriceTextChanged(string value) => UpdatePreview();
    partial void OnQuantityTextChanged(string value) => UpdatePreview();
    partial void OnSelectedUnitChanged(UnitOption? value) => UpdatePreview();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _id = query.TryGetValue("id", out var raw) && int.TryParse(raw?.ToString(), out var id) ? id : 0;
        OnPropertyChanged(nameof(IsExisting));
        OnPropertyChanged(nameof(PageTitle));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        ErrorMessage = "";

        if (!IsExisting)
        {
            Name = "";
            PriceText = "";
            QuantityText = "";
            SelectedUnit = Units[0];
            return;
        }

        var ingredient = await repository.GetAsync(_id);
        if (ingredient is null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        Name = ingredient.Name;
        SelectedUnit = Units.First(u => u.Unit == ingredient.Unit);
        PriceText = ingredient.PurchasePrice.ToString("0.##", AppDefaults.Culture);
        QuantityText = ingredient.PurchaseQuantity.ToString("0.##", AppDefaults.Culture);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Hammadde adı boş olamaz.";
            return;
        }
        if (!Display.TryParseDecimal(PriceText, out var price) || price < 0)
        {
            ErrorMessage = "Alış fiyatı geçerli bir sayı olmalı (0 veya daha büyük).";
            return;
        }
        if (!Display.TryParseDecimal(QuantityText, out var quantity) || quantity <= 0)
        {
            ErrorMessage = "Alış miktarı sıfırdan büyük bir sayı olmalı.";
            return;
        }

        await repository.SaveAsync(new Ingredient
        {
            Id = _id,
            Name = Name.Trim(),
            Unit = (SelectedUnit ?? Units[0]).Unit,
            PurchasePrice = price,
            PurchaseQuantity = quantity
        });

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsExisting) return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Hammaddeyi sil", $"'{Name}' silinsin mi?", "Sil", "Vazgeç");
        if (!confirmed) return;

        try
        {
            await repository.DeleteAsync(_id);
            await Shell.Current.GoToAsync("..");
        }
        catch (InvalidOperationException ex)
        {
            // Reçetede kullanılan hammadde silinemez.
            await Shell.Current.DisplayAlert("Silinemedi", ex.Message, "Tamam");
        }
    }

    private void UpdatePreview()
    {
        if (Display.TryParseDecimal(PriceText, out var price)
            && Display.TryParseDecimal(QuantityText, out var quantity)
            && quantity > 0)
        {
            var ingredient = new Ingredient { PurchasePrice = price, PurchaseQuantity = quantity };
            var unit = SelectedUnit?.Label ?? "";
            UnitPricePreview = $"Birim fiyat: {Display.UnitPrice(ingredient.UnitPrice)} / {unit}";
        }
        else
        {
            UnitPricePreview = "";
        }
    }
}
