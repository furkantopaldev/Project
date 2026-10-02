using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Tests;

public class CostCalculatorTests
{
    private const decimal HourlyRate = 150m;

    private static Ingredient Wax() => new()
        { Name = "Soya mumu", Unit = UnitOfMeasure.Gram, PurchasePrice = 300m, PurchaseQuantity = 1000m };
    private static Ingredient Fragrance() => new()
        { Name = "Esans", Unit = UnitOfMeasure.Milliliter, PurchasePrice = 150m, PurchaseQuantity = 50m };
    private static Ingredient Wick() => new()
        { Name = "Fitil", Unit = UnitOfMeasure.Piece, PurchasePrice = 2m, PurchaseQuantity = 1m };
    private static Ingredient Jar() => new()
        { Name = "Cam kavanoz", Unit = UnitOfMeasure.Piece, PurchasePrice = 25m, PurchaseQuantity = 1m };

    /// <summary>HESAP_MOTORU.md Test senaryosu 1: 200cc aromaterapi mumu.</summary>
    private static Recipe GoldenRecipe(decimal yield = 1m) => new()
    {
        Name = "200cc aromaterapi mumu",
        Yield = yield,
        LaborMinutes = 20m,
        OverheadRate = 0.10m,
        Items =
        {
            new RecipeItem { Ingredient = Wax(), Quantity = 180m },
            new RecipeItem { Ingredient = Fragrance(), Quantity = 10m },
            new RecipeItem { Ingredient = Wick(), Quantity = 1m },
            new RecipeItem { Ingredient = Jar(), Quantity = 1m },
        }
    };

    [Fact]
    public void ItemCost_UsesUnitPriceTimesQuantity()
    {
        var item = new RecipeItem { Ingredient = Wax(), Quantity = 180m };
        Assert.Equal(54.00m, CostCalculator.ItemCost(item));
    }

    [Fact]
    public void ItemCost_AppliesWasteRate()
    {
        var item = new RecipeItem { Ingredient = Wax(), Quantity = 100m, WasteRate = 0.10m };
        Assert.Equal(33.00m, CostCalculator.ItemCost(item)); // 0.30 x 100 x 1.10
    }

    [Fact]
    public void ItemCost_ZeroQuantity_IsZero()
    {
        var item = new RecipeItem { Ingredient = Wax(), Quantity = 0m, WasteRate = 0.10m };
        Assert.Equal(0m, CostCalculator.ItemCost(item));
    }

    [Fact]
    public void Golden_MaterialCost() =>
        Assert.Equal(111.00m, CostCalculator.MaterialCost(GoldenRecipe()));

    [Fact]
    public void Golden_LaborCost() =>
        Assert.Equal(50.00m, CostCalculator.LaborCost(GoldenRecipe(), HourlyRate));

    [Fact]
    public void Golden_OverheadCost() =>
        Assert.Equal(16.10m, CostCalculator.OverheadCost(GoldenRecipe(), HourlyRate));

    [Fact]
    public void Golden_BatchAndUnitCost()
    {
        var recipe = GoldenRecipe();
        Assert.Equal(177.10m, CostCalculator.BatchCost(recipe, HourlyRate));
        Assert.Equal(177.10m, CostCalculator.UnitCost(recipe, HourlyRate));
    }

    [Fact]
    public void UnitCost_DividesBatchCostByYield()
    {
        var recipe = GoldenRecipe(yield: 10m);
        Assert.Equal(177.10m, CostCalculator.BatchCost(recipe, HourlyRate));
        Assert.Equal(17.71m, CostCalculator.UnitCost(recipe, HourlyRate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UnitCost_NonPositiveYield_Throws(int yield)
    {
        var recipe = GoldenRecipe(yield);
        Assert.Throws<InvalidOperationException>(() => CostCalculator.UnitCost(recipe, HourlyRate));
    }

    [Fact]
    public void IngredientPriceChange_IsReflectedWithoutStoredValues()
    {
        var recipe = GoldenRecipe();
        var before = CostCalculator.UnitCost(recipe, HourlyRate);

        recipe.Items[0].Ingredient.PurchasePrice = 400m; // mum 0.40 TL/g
        var after = CostCalculator.UnitCost(recipe, HourlyRate);

        // mum kalemi 54 -> 72, malzeme +18, genel gider dahil +19.80
        Assert.Equal(before + 19.80m, after);
    }

    [Fact]
    public void MoneyRound_UsesAwayFromZero()
    {
        Assert.Equal(309.93m, Money.Round(309.925m));
        Assert.Equal(-309.93m, Money.Round(-309.925m));
    }
}
