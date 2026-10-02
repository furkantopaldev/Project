using AtolyeRecete.Core.Models;
using AtolyeRecete.Data.Repositories;
using AtolyeRecete.Data.Rows;

namespace AtolyeRecete.Data;

/// <summary>
/// Mum atölyesi başlangıç paketi. Fiyatlar örnektir; "200cc aromaterapi mumu" reçetesi
/// HESAP_MOTORU.md'deki altın örnekle birebir aynıdır (saatlik 150 TL ile birim maliyet 177,10).
/// </summary>
public class SeedService(
    AtolyeDatabase db,
    IIngredientRepository ingredients,
    IRecipeRepository recipes)
{
    /// <summary>Veritabanı tamamen boşsa örnek verileri ekler. Eklediyse true döner.</summary>
    public async Task<bool> SeedIfEmptyAsync()
    {
        var hasIngredients = await db.Connection.Table<IngredientRow>().CountAsync() > 0;
        var hasRecipes = await db.Connection.Table<RecipeRow>().CountAsync() > 0;
        if (hasIngredients || hasRecipes) return false;

        var wax = await AddIngredient("Soya mumu", UnitOfMeasure.Gram, 300m, 1000m);
        var lavender = await AddIngredient("Lavanta esansı", UnitOfMeasure.Milliliter, 150m, 50m);
        var vanilla = await AddIngredient("Vanilya esansı", UnitOfMeasure.Milliliter, 150m, 50m);
        var wick = await AddIngredient("Pamuk fitil", UnitOfMeasure.Piece, 200m, 100m);
        var jar200 = await AddIngredient("Cam kavanoz 200cc", UnitOfMeasure.Piece, 300m, 12m);
        var jar100 = await AddIngredient("Cam kavanoz 100cc", UnitOfMeasure.Piece, 180m, 12m);
        var lid = await AddIngredient("Ahşap kapak", UnitOfMeasure.Piece, 150m, 10m);
        await AddIngredient("Mum boyası", UnitOfMeasure.Milliliter, 90m, 30m);
        await AddIngredient("Karton kutu", UnitOfMeasure.Piece, 200m, 20m);
        var label = await AddIngredient("Etiket", UnitOfMeasure.Piece, 150m, 100m);

        await recipes.SaveAsync(new Recipe
        {
            Name = "200cc aromaterapi mumu",
            Yield = 1m,
            LaborMinutes = 20m,
            OverheadRate = 0.10m,
            SalePrice = 450m,
            Items =
            {
                new RecipeItem { Ingredient = wax, Quantity = 180m },
                new RecipeItem { Ingredient = lavender, Quantity = 10m },
                new RecipeItem { Ingredient = wick, Quantity = 1m },
                new RecipeItem { Ingredient = jar200, Quantity = 1m },
            }
        });

        await recipes.SaveAsync(new Recipe
        {
            Name = "100cc mini vanilya mumu",
            Yield = 1m,
            LaborMinutes = 15m,
            OverheadRate = 0.10m,
            SalePrice = 250m,
            Items =
            {
                new RecipeItem { Ingredient = wax, Quantity = 90m, WasteRate = 0.05m },
                new RecipeItem { Ingredient = vanilla, Quantity = 5m },
                new RecipeItem { Ingredient = wick, Quantity = 1m },
                new RecipeItem { Ingredient = jar100, Quantity = 1m },
                new RecipeItem { Ingredient = lid, Quantity = 1m },
                new RecipeItem { Ingredient = label, Quantity = 1m },
            }
        });

        return true;
    }

    /// <summary>Tüm hammadde ve reçeteleri siler (örnek veri dahil, kullanıcı verisi dahil).</summary>
    public async Task ClearAllAsync()
    {
        await db.Connection.DeleteAllAsync<RecipeItemRow>();
        await db.Connection.DeleteAllAsync<RecipeRow>();
        await db.Connection.DeleteAllAsync<IngredientRow>();
    }

    private async Task<Ingredient> AddIngredient(
        string name, UnitOfMeasure unit, decimal purchasePrice, decimal purchaseQuantity)
    {
        var ingredient = new Ingredient
        {
            Name = name,
            Unit = unit,
            PurchasePrice = purchasePrice,
            PurchaseQuantity = purchaseQuantity
        };
        await ingredients.SaveAsync(ingredient);
        return ingredient;
    }
}
