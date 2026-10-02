using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Tests;

public class PricingCalculatorTests
{
    private const decimal GoldenUnitCost = 177.10m;

    private static Platform Pf(decimal commission, decimal fixedFee = 0m) =>
        new() { Name = "Test", CommissionRate = commission, FixedFee = fixedFee };

    // --- HESAP_MOTORU.md Test senaryosu 1 ---

    [Fact]
    public void Golden_Margin40_Commission20() =>
        Assert.Equal(442.75m,
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0.20m), 0.40m, ProfitMode.Margin));

    [Fact]
    public void Golden_Margin40_Commission3_5() =>
        Assert.Equal(313.45m,
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0.035m), 0.40m, ProfitMode.Margin));

    [Fact]
    public void Golden_Markup40_Commission20_RoundsAwayFromZero() =>
        Assert.Equal(309.93m, // 309.925
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0.20m), 0.40m, ProfitMode.Markup));

    [Fact]
    public void DefaultMode_IsMargin() =>
        Assert.Equal(442.75m,
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0.20m), 0.40m));

    // --- Sabit ücretler (kargo + işlem) ---

    [Fact]
    public void Margin_WithFixedFee() =>
        Assert.Equal(492.75m, // (177.10 + 20) / 0.40
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0.20m, 20m), 0.40m, ProfitMode.Margin));

    [Fact]
    public void Markup_WithFixedFee() =>
        Assert.Equal(334.93m, // (177.10 x 1.40 + 20) / 0.80 = 334.925
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0.20m, 20m), 0.40m, ProfitMode.Markup));

    // --- Aynı %40, iki modda farklı fiyat ---

    [Fact]
    public void SameRate_GivesDifferentPrices_InDifferentModes()
    {
        var margin = PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0m), 0.40m, ProfitMode.Margin);
        var markup = PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0m), 0.40m, ProfitMode.Markup);
        Assert.Equal(295.17m, margin);  // 177.10 / 0.6 = 295.1666..
        Assert.Equal(247.94m, markup);  // 177.10 x 1.4
    }

    // --- Marjın gerçekten satış fiyatı üzerinden olduğunu doğrula ---

    [Fact]
    public void Margin_ProfitIsTargetShareOfPrice()
    {
        var p = Pf(0.20m, 20m);
        var price = PricingCalculator.SuggestPrice(GoldenUnitCost, p, 0.40m, ProfitMode.Margin);
        var profit = price - price * p.CommissionRate - p.FixedFee - GoldenUnitCost;
        Assert.InRange(profit / price, 0.399m, 0.401m);
    }

    // --- Verilen satış fiyatında marj ---

    [Theory]
    [InlineData(354.20, 0.50)]   // 177.10 / 354.20
    [InlineData(295.1666666667, 0.40)]
    [InlineData(177.10, 0.00)]   // maliyetine satış
    public void MarginAtPrice_IsShareOfPrice(double price, double expected) =>
        Assert.Equal((decimal)expected, Math.Round(PricingCalculator.MarginAtPrice(GoldenUnitCost, (decimal)price), 4));

    [Fact]
    public void MarginAtPrice_BelowCost_IsNegative() =>
        Assert.True(PricingCalculator.MarginAtPrice(GoldenUnitCost, 100m) < 0);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void MarginAtPrice_NonPositivePrice_Throws(int price) =>
        Assert.Throws<InvalidOperationException>(() => PricingCalculator.MarginAtPrice(GoldenUnitCost, price));

    // --- Kenar durumlar ---

    [Theory]
    [InlineData(0.60, 0.40)]  // k + m = 1
    [InlineData(0.70, 0.40)]  // k + m > 1
    public void Margin_CommissionPlusMarginAtLeastOne_Throws(double k, double m)
    {
        var ex = Record.Exception(() =>
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf((decimal)k), (decimal)m, ProfitMode.Margin));
        Assert.IsType<InvalidOperationException>(ex);
        Assert.False(PricingCalculator.IsFeasible((decimal)k, (decimal)m, ProfitMode.Margin));
    }

    [Fact]
    public void Markup_CommissionAtLeastOne_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(1m), 0.40m, ProfitMode.Markup));
        Assert.False(PricingCalculator.IsFeasible(1m, 0.40m, ProfitMode.Markup));
    }

    [Fact]
    public void Markup_AllowsHighMarkup_WhenCommissionBelowOne()
    {
        // Markup %200 geçerli (k + m >= 1 kuralı sadece marj modunda).
        Assert.True(PricingCalculator.IsFeasible(0.20m, 2m, ProfitMode.Markup));
        Assert.Equal(664.13m, // 177.10 x 3 / 0.8 = 664.125
            PricingCalculator.SuggestPrice(GoldenUnitCost, Pf(0.20m), 2m, ProfitMode.Markup));
    }

    [Fact]
    public void IsFeasible_TrueForNormalRates()
    {
        Assert.True(PricingCalculator.IsFeasible(0.20m, 0.40m, ProfitMode.Margin));
        Assert.True(PricingCalculator.IsFeasible(0.20m, 0.40m, ProfitMode.Markup));
    }
}
