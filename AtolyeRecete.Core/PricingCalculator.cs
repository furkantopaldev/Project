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
