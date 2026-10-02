using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Core;

public static class PricingCalculator
{
    /// <summary>
    /// Verilen oranlarla fiyat hesaplanabilir mi? (UI uyarısı için)
    /// Marj: k + m &lt; 1. Markup: k &lt; 1.
    /// </summary>
    public static bool IsFeasible(decimal commissionRate, decimal profitRate, ProfitMode mode) =>
        mode switch
        {
            ProfitMode.Margin => commissionRate + profitRate < 1m,
            ProfitMode.Markup => commissionRate < 1m,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

    /// <summary>
    /// Verilen satış fiyatında marj = (fiyat - birim maliyet) / fiyat (komisyon öncesi, kesir olarak).
    /// Fiyat sıfır veya negatifse <see cref="InvalidOperationException"/> fırlatır.
    /// Maliyetin altında satışta negatif döner.
    /// </summary>
    public static decimal MarginAtPrice(decimal unitCost, decimal salePrice) =>
        salePrice > 0m
            ? (salePrice - unitCost) / salePrice
            : throw new InvalidOperationException("Satış fiyatı sıfırdan büyük olmalı.");

    /// <summary>
    /// Önerilen fiyat ve döküm (komisyon, net kâr, gerçekleşen marj/markup).
    /// Hesaplanamıyorsa <see cref="InvalidOperationException"/> fırlatır.
    /// </summary>
    public static PriceBreakdown Analyze(
        decimal unitCost, Platform platform, decimal profitRate, ProfitMode mode = ProfitMode.Margin)
    {
        var price = SuggestPrice(unitCost, platform, profitRate, mode);

        // Döküm, yuvarlanmış (satıcının gerçekten alacağı) fiyat üzerinden hesaplanır.
        var commission = price * platform.CommissionRate;
        var netProfit = price - commission - platform.FixedFee - unitCost;

        return new PriceBreakdown(
            Price: price,
            UnitCost: unitCost,
            Commission: commission,
            FixedFee: platform.FixedFee,
            NetProfit: netProfit,
            MarginRate: price > 0m ? netProfit / price : 0m,
            MarkupRate: unitCost > 0m ? netProfit / unitCost : 0m);
    }

    /// <summary>
    /// Önerilen satış fiyatı, 2 basamağa yuvarlanmış.
    /// Marj:   P = (C + F) / (1 - k - m)
    /// Markup: P = (C x (1 + m) + F) / (1 - k)
    /// Hesaplanamıyorsa <see cref="InvalidOperationException"/> fırlatır.
    /// </summary>
    public static decimal SuggestPrice(
        decimal unitCost, Platform platform, decimal profitRate, ProfitMode mode = ProfitMode.Margin)
    {
        var k = platform.CommissionRate;
        var f = platform.FixedFee;

        if (!IsFeasible(k, profitRate, mode))
            throw new InvalidOperationException(mode == ProfitMode.Margin
                ? "Komisyon ve hedef marj toplamı %100'ün altında olmalı."
                : "Komisyon oranı %100'ün altında olmalı.");

        // Tek bölme, en sonda (ara bölmeler ondalık hata biriktirir).
        var price = mode switch
        {
            ProfitMode.Margin => (unitCost + f) / (1m - k - profitRate),
            ProfitMode.Markup => (unitCost * (1m + profitRate) + f) / (1m - k),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        return Money.Round(price);
    }
}
