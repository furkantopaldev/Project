using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Core;

/// <summary>Reçete maliyet hesapları. Ara sonuçlar yuvarlanmaz.</summary>
public static class CostCalculator
{
    /// <summary>Kalem maliyeti = birim fiyat x miktar x (1 + fire oranı).</summary>
    public static decimal ItemCost(RecipeItem item) =>
        item.Ingredient.UnitPrice * item.Quantity * (1 + item.WasteRate);

    public static decimal MaterialCost(Recipe recipe) =>
        recipe.Items.Sum(ItemCost);

    /// <summary>
    /// İşçilik = süre (dk) / 60 x saatlik ücret. Önce çarpılır, sonra bölünür:
    /// 20 / 60m = 0.333... olur ve 150 ile çarpınca 49.999... çıkar.
    /// </summary>
    public static decimal LaborCost(Recipe recipe, decimal hourlyRate) =>
        recipe.LaborMinutes * hourlyRate / 60m;

    /// <summary>Genel gider payı = (malzeme + işçilik) x genel gider oranı.</summary>
    public static decimal OverheadCost(Recipe recipe, decimal hourlyRate) =>
        (MaterialCost(recipe) + LaborCost(recipe, hourlyRate)) * recipe.OverheadRate;

    /// <summary>Parti maliyeti = malzeme + işçilik + genel gider payı.</summary>
    public static decimal BatchCost(Recipe recipe, decimal hourlyRate) =>
        MaterialCost(recipe)
        + LaborCost(recipe, hourlyRate)
        + OverheadCost(recipe, hourlyRate);

    /// <summary>Birim maliyet = parti maliyeti / verim.</summary>
    public static decimal UnitCost(Recipe recipe, decimal hourlyRate) =>
        recipe.Yield > 0
            ? BatchCost(recipe, hourlyRate) / recipe.Yield
            : throw new InvalidOperationException(
                $"'{recipe.Name}' için verim sıfırdan büyük olmalı.");
}
