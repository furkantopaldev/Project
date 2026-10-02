using AtolyeRecete.App.Services;
using AtolyeRecete.Data;
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

public partial class SettingsViewModel(
    StartupService startup,
    AppSettings settings,
    SeedService seed,
    IIngredientRepository ingredients,
    IRecipeRepository recipes) : ObservableObject
{
    [ObservableProperty] private string _hourlyRateText = "";
    [ObservableProperty] private string _overheadPercentText = "";
    [ObservableProperty] private string _dataSummary = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _statusMessage = "";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    [RelayCommand]
    private async Task LoadAsync()
    {
        await startup.EnsureInitializedAsync();

        HourlyRateText = Display.NumberText(settings.HourlyRate);
        OverheadPercentText = Display.NumberText(settings.DefaultOverheadPercentValue);
        ErrorMessage = "";
        StatusMessage = "";
        await RefreshSummaryAsync();
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = "";
        StatusMessage = "";

        if (!Display.TryParseDecimal(HourlyRateText, out var hourlyRate) || hourlyRate < 0)
        {
            ErrorMessage = "Saatlik işçilik ücreti geçerli bir sayı olmalı (0 veya daha büyük).";
            return;
        }
        if (!Display.TryParseDecimal(OverheadPercentText, out var overheadPercent) || overheadPercent < 0)
        {
            ErrorMessage = "Genel gider % geçerli bir sayı olmalı (0 veya daha büyük).";
            return;
        }

        settings.HourlyRate = hourlyRate;
        settings.DefaultOverheadPercentValue = overheadPercent;
        StatusMessage = "Kaydedildi. Tüm reçetelerin maliyeti yeni saatlik ücretle yeniden hesaplanır.";
    }

    [RelayCommand]
    private async Task LoadSampleAsync()
    {
        ErrorMessage = "";
        StatusMessage = "";

        if (await seed.SeedIfEmptyAsync())
            StatusMessage = "Örnek mum atölyesi verileri yüklendi.";
        else
            ErrorMessage = "Örnek veriler yalnızca veritabanı boşken yüklenir. Önce 'Tüm verileri sil' ile temizle.";

        await RefreshSummaryAsync();
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        ErrorMessage = "";
        StatusMessage = "";

        var confirmed = await Shell.Current.DisplayAlert(
            "Tüm verileri sil",
            "Tüm hammaddeler ve reçeteler silinecek. Bu işlem geri alınamaz.",
            "Sil", "Vazgeç");
        if (!confirmed) return;

        await seed.ClearAllAsync();
        StatusMessage = "Tüm hammadde ve reçeteler silindi.";
        await RefreshSummaryAsync();
    }

    private async Task RefreshSummaryAsync()
    {
        var ingredientCount = (await ingredients.GetAllAsync()).Count;
        var recipeCount = (await recipes.GetAllAsync()).Count;
        DataSummary = $"Veritabanında {ingredientCount} hammadde, {recipeCount} reçete var.";
    }
}
