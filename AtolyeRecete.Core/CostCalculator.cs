using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Core;

/// <summary>Reçete maliyet hesapları. Ara sonuçlar yuvarlanmaz.</summary>
public static class CostCalculator
{
    public static decimal ItemCost(RecipeItem item) =>
        throw new NotImplementedException();

    public static decimal MaterialCost(Recipe recipe) =>
        throw new NotImplementedException();

    public static decimal LaborCost(Recipe recipe, decimal hourlyRate) =>
        throw new NotImplementedException();

    public static decimal OverheadCost(Recipe recipe, decimal hourlyRate) =>
        throw new NotImplementedException();

    public static decimal BatchCost(Recipe recipe, decimal hourlyRate) =>
        throw new NotImplementedException();

    public static decimal UnitCost(Recipe recipe, decimal hourlyRate) =>
        throw new NotImplementedException();
}
