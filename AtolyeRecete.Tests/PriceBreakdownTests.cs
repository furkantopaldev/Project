using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Tests;

public class PriceBreakdownTests
{
    private const decimal GoldenUnitCost = 177.10m;

    private static Platform Pf(decimal commission, decimal fixedFee = 0m) =>
        new() { Name = "Test", CommissionRate = commission, FixedFee = fixedFee };

    [Fact]
    public void Golden_Margin40_Commission20()
    {
        var b = PricingCalculator.Analyze(GoldenUnitCost, Pf(0.20m), 0.40m, ProfitMode.Margin);

        Assert.Equal(442.75m, b.Price);
        Assert.Equal(GoldenUnitCost, b.UnitCost);
        Assert.Equal(88.55m, b.Commission);     // 442.75 x 0.20
        Assert.Equal(0m, b.FixedFee);
        Assert.Equal(177.10m, b.NetProfit);     // 442.75 - 88.55 - 177.10
        Assert.Equal(0.40m, b.MarginRate);      // 177.10 / 442.75
        Assert.Equal(1.00m, b.MarkupRate);      // 177.10 / 177.10: aynı kâr, maliyet üzerinden %100
    }

    [Fact]
    public void Golden_Markup40_Commission20()
    {
        var b = PricingCalculator.Analyze(GoldenUnitCost, Pf(0.20m), 0.40m, ProfitMode.Markup);

        Assert.Equal(309.93m, b.Price);
        Assert.Equal(61.986m, b.Commission);    // 309.93 x 0.20, yuvarlanmamış
        Assert.Equal(70.844m, b.NetProfit);     // 309.93 - 61.986 - 177.10
        // Fiyat kuruşa yuvarlandığı için hedeften çok az sapar.
        Assert.InRange(b.MarkupRate, 0.3999m, 0.4001m);
        Assert.InRange(b.MarginRate, 0.2285m, 0.2287m);
    }

    [Fact]
    public void FixedFee_IsIncludedInBreakdown()
    {
        var b = PricingCalculator.Analyze(GoldenUnitCost, Pf(0.20m, 20m), 0.40m, ProfitMode.Margin);

        Assert.Equal(492.75m, b.Price);
        Assert.Equal(98.55m, b.Commission);
        Assert.Equal(20m, b.FixedFee);
        Assert.Equal(197.10m, b.NetProfit);     // 492.75 - 98.55 - 20 - 177.10
        Assert.Equal(0.40m, b.MarginRate);
    }

    [Fact]
    public void Price_MatchesSuggestPrice()
    {
        var p = Pf(0.035m, 7.5m);
        foreach (var mode in new[] { ProfitMode.Margin, ProfitMode.Markup })
            Assert.Equal(
                PricingCalculator.SuggestPrice(GoldenUnitCost, p, 0.35m, mode),
                PricingCalculator.Analyze(GoldenUnitCost, p, 0.35m, mode).Price);
    }

    [Fact]
    public void NoCommission_MarginMode_MatchesTarget()
    {
        var b = PricingCalculator.Analyze(100m, Pf(0m), 0.50m, ProfitMode.Margin);

        Assert.Equal(200m, b.Price);
        Assert.Equal(0m, b.Commission);
        Assert.Equal(100m, b.NetProfit);
        Assert.Equal(0.50m, b.MarginRate);
        Assert.Equal(1.00m, b.MarkupRate);
    }

    [Fact]
    public void ZeroCostAndFee_DoesNotDivideByZero()
    {
        var b = PricingCalculator.Analyze(0m, Pf(0.20m), 0.40m, ProfitMode.Margin);

        Assert.Equal(0m, b.Price);
        Assert.Equal(0m, b.NetProfit);
        Assert.Equal(0m, b.MarginRate);
        Assert.Equal(0m, b.MarkupRate);
    }

    [Fact]
    public void Infeasible_Throws() =>
        Assert.Throws<InvalidOperationException>(() =>
            PricingCalculator.Analyze(GoldenUnitCost, Pf(0.70m), 0.40m, ProfitMode.Margin));
}
