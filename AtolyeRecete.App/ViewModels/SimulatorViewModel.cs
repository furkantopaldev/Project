using System.Collections.ObjectModel;
using AtolyeRecete.App.Services;
using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;
using Platform = AtolyeRecete.Core.Models.Platform; // MAUI'nin kendi Platform sınıfıyla çakışmayı önler
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

public record ModeOption(ProfitMode Mode, string Label);

/// <summary>Karşılaştırma tablosu satırı: aynı reçete, bir platformda.</summary>
public record PlatformPriceRow(string Name, string CommissionText, string PriceText, string NetProfitText);

public partial class SimulatorViewModel(
    StartupService startup,
    IRecipeRepository recipeRepository,
    PlatformService platformService) : ObservableObject
{
    /// <summary>Yükleme sırasında her seçimde yeniden hesaplamayı önler.</summary>
    private bool _loading;

    /// <summary>platforms.json okunamadıysa nedeni; hesaplamalar bu mesajı gösterir.</summary>
    private string _platformLoadError = "";

    public IReadOnlyList<ModeOption> Modes { get; } = new[]
    {
        new ModeOption(ProfitMode.Margin, "Marj (kâr, satış fiyatı üzerinden)"),
        new ModeOption(ProfitMode.Markup, "Markup (kâr, maliyet üzerinden)")
    };

    [ObservableProperty] private IReadOnlyList<Recipe> _recipes = Array.Empty<Recipe>();
    [ObservableProperty] private IReadOnlyList<Platform> _platforms = Array.Empty<Platform>();

    [ObservableProperty] private Recipe? _selectedRecipe;
    [ObservableProperty] private Platform? _selectedPlatform;
    [ObservableProperty] private ModeOption? _selectedMode;

    /// <summary>Hedef kâr, yüzde olarak girilir (seçili moda göre marj veya markup).</summary>
    [ObservableProperty] private string _profitPercentText = "40";

    /// <summary>Platformun sabit ücretine eklenecek kargo vb. (₺).</summary>
    [ObservableProperty] private string _extraFeeText = "0";

    // Sonuçlar
    [ObservableProperty] private string _unitCostText = "";
    [ObservableProperty] private string _priceText = "";
    [ObservableProperty] private string _commissionText = "";
    [ObservableProperty] private string _fixedFeeText = "";
    [ObservableProperty] private string _netProfitText = "";
    [ObservableProperty] private string _marginText = "";
    [ObservableProperty] private string _markupText = "";
    [ObservableProperty] private string _platformNote = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string _message = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    private bool _resultVisible;

    public bool HasMessage => !string.IsNullOrEmpty(Message);
    public bool HasResult => ResultVisible;

    public ObservableCollection<PlatformPriceRow> Comparison { get; } = new();

    partial void OnSelectedRecipeChanged(Recipe? value) => Recalculate();
    partial void OnSelectedPlatformChanged(Platform? value) => Recalculate();
    partial void OnSelectedModeChanged(ModeOption? value) => Recalculate();
    partial void OnProfitPercentTextChanged(string value) => Recalculate();
    partial void OnExtraFeeTextChanged(string value) => Recalculate();

    [RelayCommand]
    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            await startup.EnsureInitializedAsync();

            _platformLoadError = "";
            var previousRecipeId = SelectedRecipe?.Id;
            var previousPlatform = SelectedPlatform?.Name;

            Recipes = await recipeRepository.GetAllAsync();
            SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == previousRecipeId) ?? Recipes.FirstOrDefault();
            SelectedMode ??= Modes[0];

            try
            {
                Platforms = await platformService.GetAllAsync();
                SelectedPlatform = Platforms.FirstOrDefault(p => p.Name == previousPlatform) ?? Platforms.FirstOrDefault();
            }
            catch (Exception ex) when (ex is FormatException or IOException or FileNotFoundException)
            {
                Platforms = Array.Empty<Platform>();
                SelectedPlatform = null;
                _platformLoadError = $"Komisyon tablosu yüklenemedi: {ex.Message}";
            }
        }
        finally
        {
            _loading = false;
            Recalculate();
        }
    }

    private void Recalculate()
    {
        if (_loading) return;

        ResultVisible = false;
        Comparison.Clear();
        Message = "";
        PlatformNote = SelectedPlatform?.Notes ?? "";

        if (_platformLoadError.Length > 0)
        {
            Message = _platformLoadError;
            return;
        }

        if (SelectedRecipe is null)
        {
            Message = "Önce Reçeteler sekmesinden bir reçete oluştur.";
            return;
        }
        if (SelectedPlatform is null || SelectedMode is null)
        {
            if (Platforms.Count > 0) Message = "Bir platform seç.";
            return;
        }
        if (!Display.TryParseDecimal(ProfitPercentText, out var profitPercent) || profitPercent < 0)
        {
            Message = "Hedef kâr % geçerli bir sayı olmalı (0 veya daha büyük).";
            return;
        }
        if (!Display.TryParseDecimal(ExtraFeeText, out var extraFee) || extraFee < 0)
        {
            Message = "Ek sabit ücret geçerli bir sayı olmalı (0 veya daha büyük).";
            return;
        }

        decimal unitCost;
        try
        {
            unitCost = CostCalculator.UnitCost(SelectedRecipe, AppDefaults.HourlyRate);
        }
        catch (InvalidOperationException ex)
        {
            Message = ex.Message;
            return;
        }

        var profitRate = Display.RateFromPercent(profitPercent);
        var mode = SelectedMode.Mode;
        UnitCostText = Display.Money(unitCost);

        // Seçili platform: ayrıntılı döküm
        var platform = WithExtraFee(SelectedPlatform, extraFee);
        if (!PricingCalculator.IsFeasible(platform.CommissionRate, profitRate, mode))
        {
            Message = mode == ProfitMode.Margin
                ? "Bu oranlarla fiyat hesaplanamaz: komisyon ve hedef marj toplamı %100'ün altında olmalı."
                : "Bu oranlarla fiyat hesaplanamaz: komisyon %100'ün altında olmalı.";
        }
        else
        {
            var b = PricingCalculator.Analyze(unitCost, platform, profitRate, mode);
            PriceText = Display.Money(b.Price);
            CommissionText = Display.Money(b.Commission);
            FixedFeeText = Display.Money(b.FixedFee);
            NetProfitText = Display.Money(b.NetProfit);
            MarginText = $"Marj (satış fiyatı üzerinden): %{Display.PercentText(b.MarginRate)}";
            MarkupText = $"Markup (maliyet üzerinden): %{Display.PercentText(b.MarkupRate)}";
            ResultVisible = true;
        }

        // Aynı reçete, tüm platformlarda
        foreach (var p in Platforms)
        {
            var candidate = WithExtraFee(p, extraFee);
            var commission = $"%{Display.PercentText(p.CommissionRate)}";
            if (PricingCalculator.IsFeasible(candidate.CommissionRate, profitRate, mode))
            {
                var row = PricingCalculator.Analyze(unitCost, candidate, profitRate, mode);
                Comparison.Add(new PlatformPriceRow(p.Name, commission, Display.Money(row.Price), Display.Money(row.NetProfit)));
            }
            else
            {
                Comparison.Add(new PlatformPriceRow(p.Name, commission, "Hesaplanamaz", "—"));
            }
        }
    }

    private static Platform WithExtraFee(Platform platform, decimal extraFee) => new()
    {
        Name = platform.Name,
        CommissionRate = platform.CommissionRate,
        FixedFee = platform.FixedFee + extraFee,
        Notes = platform.Notes
    };
}
