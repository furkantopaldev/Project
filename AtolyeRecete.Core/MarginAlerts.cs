using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Core;

/// <summary>Bir reçetenin marjının bir fiyat değişikliği öncesi ve sonrası değeri.</summary>
public record MarginDrop(
    int RecipeId,
    string RecipeName,
    decimal SalePrice,
    decimal UnitCostBefore,
    decimal UnitCostAfter,
    decimal MarginBefore,
    decimal MarginAfter);

public static class MarginAlerts
{
    /// <summary>
    /// Değişiklik öncesi ve sonrası reçete listelerini (Id ile eşleşir) karşılaştırır.
    /// Bir reçete listeye girer, ancak ve ancak: satış fiyatı varsa, marjı kötüleştiyse
    /// ve yeni marj eşiğin kesinlikle altındaysa. Eşit marj uyarı vermez.
    /// </summary>
    /// <param name="thresholdRate">Eşik, kesir olarak (0.30 = %30).</param>
    public static IReadOnlyList<MarginDrop> FindDrops(
        IEnumerable<Recipe> before, IEnumerable<Recipe> after, decimal hourlyRate, decimal thresholdRate)
    {
        var beforeById = before.ToDictionary(r => r.Id);
        var drops = new List<MarginDrop>();

        foreach (var recipe in after)
        {
            if (!beforeById.TryGetValue(recipe.Id, out var old)) continue;
            if (old.SalePrice is not { } oldPrice || recipe.SalePrice is not { } newPrice) continue;
            if (oldPrice <= 0m || newPrice <= 0m) continue;

            decimal costBefore, costAfter;
            try
            {
                costBefore = CostCalculator.UnitCost(old, hourlyRate);
                costAfter = CostCalculator.UnitCost(recipe, hourlyRate);
            }
            catch (InvalidOperationException)
            {
                continue; // maliyeti hesaplanamayan reçete için marj uyarısı üretilmez
            }

            var marginBefore = PricingCalculator.MarginAtPrice(costBefore, oldPrice);
            var marginAfter = PricingCalculator.MarginAtPrice(costAfter, newPrice);

            if (marginAfter < marginBefore && marginAfter < thresholdRate)
                drops.Add(new MarginDrop(
                    recipe.Id, recipe.Name, newPrice, costBefore, costAfter, marginBefore, marginAfter));
        }

        return drops;
    }
}
