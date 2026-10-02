using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;
using AtolyeRecete.Data;
using AtolyeRecete.Data.Repositories;

namespace AtolyeRecete.Tests;

public class DataTests
{
    private const decimal HourlyRate = 150m;

    /// <summary>Her test kendi bellek içi veritabanını kurar.</summary>
    private static async Task<(IIngredientRepository Ingredients, IRecipeRepository Recipes, SeedService Seed)>
        CreateAsync()
    {
        var db = AtolyeDatabase.CreateInMemory();
        await db.InitializeAsync();
        var ingredients = new IngredientRepository(db);
        var recipes = new RecipeRepository(db);
        return (ingredients, recipes, new SeedService(db, ingredients, recipes));
    }

    private static Ingredient NewIngredient(string name = "Test", decimal price = 300m, decimal quantity = 1000m) => new()
    {
        Name = name,
        Unit = UnitOfMeasure.Gram,
        PurchasePrice = price,
        PurchaseQuantity = quantity
    };

    // --- Hammadde ---

    [Fact]
    public async Task Ingredient_SaveAssignsIdAndRoundTrips()
    {
        var (repo, _, _) = await CreateAsync();
        var ingredient = NewIngredient("Soya mumu");

        await repo.SaveAsync(ingredient);
        var loaded = await repo.GetAsync(ingredient.Id);

        Assert.NotEqual(0, ingredient.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Soya mumu", loaded.Name);
        Assert.Equal(UnitOfMeasure.Gram, loaded.Unit);
        Assert.Equal(300m, loaded.PurchasePrice);
        Assert.Equal(0.30m, loaded.UnitPrice);
    }

    [Fact]
    public async Task Ingredient_DecimalValuesAreStoredLosslessly()
    {
        var (repo, _, _) = await CreateAsync();
        // double ile saklansaydı bu değerler bozulurdu.
        var ingredient = NewIngredient(price: 123456789.123456789m, quantity: 0.035m);

        await repo.SaveAsync(ingredient);
        var loaded = await repo.GetAsync(ingredient.Id);

        Assert.Equal(123456789.123456789m, loaded!.PurchasePrice);
        Assert.Equal(0.035m, loaded.PurchaseQuantity);
    }

    [Fact]
    public async Task Ingredient_SaveWithExistingIdUpdates()
    {
        var (repo, _, _) = await CreateAsync();
        var ingredient = NewIngredient();
        await repo.SaveAsync(ingredient);

        ingredient.PurchasePrice = 400m;
        await repo.SaveAsync(ingredient);

        Assert.Single(await repo.GetAllAsync());
        Assert.Equal(400m, (await repo.GetAsync(ingredient.Id))!.PurchasePrice);
    }

    [Fact]
    public async Task Ingredient_GetAllIsOrderedByName()
    {
        var (repo, _, _) = await CreateAsync();
        await repo.SaveAsync(NewIngredient("Fitil"));
        await repo.SaveAsync(NewIngredient("Esans"));
        await repo.SaveAsync(NewIngredient("Kavanoz"));

        var names = (await repo.GetAllAsync()).Select(i => i.Name).ToList();

        Assert.Equal(new[] { "Esans", "Fitil", "Kavanoz" }, names);
    }

    [Fact]
    public async Task Ingredient_DeleteRemovesIt()
    {
        var (repo, _, _) = await CreateAsync();
        var ingredient = NewIngredient();
        await repo.SaveAsync(ingredient);

        await repo.DeleteAsync(ingredient.Id);

        Assert.Null(await repo.GetAsync(ingredient.Id));
    }

    [Fact]
    public async Task Ingredient_UsedInRecipe_CannotBeDeleted()
    {
        var (ingredients, recipes, _) = await CreateAsync();
        var ingredient = NewIngredient();
        await ingredients.SaveAsync(ingredient);
        await recipes.SaveAsync(new Recipe
        {
            Name = "R",
            Items = { new RecipeItem { Ingredient = ingredient, Quantity = 10m } }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => ingredients.DeleteAsync(ingredient.Id));
        Assert.NotNull(await ingredients.GetAsync(ingredient.Id));
    }

    // --- Reçete ---

    [Fact]
    public async Task Recipe_RoundTripsWithItems()
    {
        var (ingredients, recipes, _) = await CreateAsync();
        var wax = NewIngredient("Mum");
        var jar = NewIngredient("Kavanoz", 25m, 1m);
        await ingredients.SaveAsync(wax);
        await ingredients.SaveAsync(jar);

        var recipe = new Recipe
        {
            Name = "Mum 200cc",
            Yield = 2m,
            LaborMinutes = 12.5m,
            OverheadRate = 0.10m,
            Items =
            {
                new RecipeItem { Ingredient = wax, Quantity = 180m, WasteRate = 0.05m },
                new RecipeItem { Ingredient = jar, Quantity = 1m },
            }
        };
        await recipes.SaveAsync(recipe);

        var loaded = await recipes.GetAsync(recipe.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Mum 200cc", loaded.Name);
        Assert.Equal(2m, loaded.Yield);
        Assert.Equal(12.5m, loaded.LaborMinutes);
        Assert.Equal(0.10m, loaded.OverheadRate);
        Assert.Equal(2, loaded.Items.Count);
        Assert.Contains(loaded.Items, i => i.Ingredient.Name == "Mum" && i.Quantity == 180m && i.WasteRate == 0.05m);
        Assert.Contains(loaded.Items, i => i.Ingredient.Name == "Kavanoz" && i.Quantity == 1m);
    }

    [Fact]
    public async Task Recipe_UpdateReplacesItems()
    {
        var (ingredients, recipes, _) = await CreateAsync();
        var a = NewIngredient("A");
        var b = NewIngredient("B");
        await ingredients.SaveAsync(a);
        await ingredients.SaveAsync(b);
        var recipe = new Recipe { Name = "R", Items = { new RecipeItem { Ingredient = a, Quantity = 1m } } };
        await recipes.SaveAsync(recipe);

        recipe.Items = new List<RecipeItem> { new() { Ingredient = b, Quantity = 2m } };
        await recipes.SaveAsync(recipe);

        var loaded = await recipes.GetAsync(recipe.Id);
        Assert.Single(await recipes.GetAllAsync());
        Assert.Single(loaded!.Items);
        Assert.Equal("B", loaded.Items[0].Ingredient.Name);
    }

    [Fact]
    public async Task Recipe_DeleteRemovesRecipeAndItems()
    {
        var (ingredients, recipes, _) = await CreateAsync();
        var a = NewIngredient();
        await ingredients.SaveAsync(a);
        var recipe = new Recipe { Name = "R", Items = { new RecipeItem { Ingredient = a, Quantity = 1m } } };
        await recipes.SaveAsync(recipe);

        await recipes.DeleteAsync(recipe.Id);

        Assert.Null(await recipes.GetAsync(recipe.Id));
        // Kalemler gittiği için hammadde artık silinebilir.
        await ingredients.DeleteAsync(a.Id);
        Assert.Empty(await ingredients.GetAllAsync());
    }

    [Fact]
    public async Task Recipe_GetMissing_ReturnsNull()
    {
        var (_, recipes, _) = await CreateAsync();
        Assert.Null(await recipes.GetAsync(999));
    }

    [Fact]
    public async Task IngredientPriceChange_UpdatesRecipeCost_WithoutTouchingRecipe()
    {
        var (ingredients, recipes, _) = await CreateAsync();
        var wax = NewIngredient("Mum", 300m, 1000m);
        await ingredients.SaveAsync(wax);
        var recipe = new Recipe { Name = "R", Items = { new RecipeItem { Ingredient = wax, Quantity = 100m } } };
        await recipes.SaveAsync(recipe);
        Assert.Equal(30m, CostCalculator.MaterialCost((await recipes.GetAsync(recipe.Id))!));

        wax.PurchasePrice = 400m;
        await ingredients.SaveAsync(wax);

        Assert.Equal(40m, CostCalculator.MaterialCost((await recipes.GetAsync(recipe.Id))!));
    }

    // --- Seed ---

    [Fact]
    public async Task Seed_AddsStarterPack()
    {
        var (ingredients, recipes, seed) = await CreateAsync();

        var added = await seed.SeedIfEmptyAsync();

        Assert.True(added);
        Assert.Equal(10, (await ingredients.GetAllAsync()).Count);
        Assert.Equal(2, (await recipes.GetAllAsync()).Count);
    }

    [Fact]
    public async Task Seed_GoldenRecipeMatchesHesapMotoruDoc()
    {
        var (_, recipes, seed) = await CreateAsync();
        await seed.SeedIfEmptyAsync();

        var golden = (await recipes.GetAllAsync()).Single(r => r.Name == "200cc aromaterapi mumu");

        Assert.Equal(111.00m, CostCalculator.MaterialCost(golden));
        Assert.Equal(177.10m, CostCalculator.UnitCost(golden, HourlyRate));
    }

    [Fact]
    public async Task Seed_EveryRecipeHasPositiveCost()
    {
        var (_, recipes, seed) = await CreateAsync();
        await seed.SeedIfEmptyAsync();

        foreach (var recipe in await recipes.GetAllAsync())
            Assert.True(CostCalculator.UnitCost(recipe, HourlyRate) > 0, recipe.Name);
    }

    [Fact]
    public async Task Seed_SecondCallAddsNothing()
    {
        var (ingredients, recipes, seed) = await CreateAsync();
        await seed.SeedIfEmptyAsync();

        var addedAgain = await seed.SeedIfEmptyAsync();

        Assert.False(addedAgain);
        Assert.Equal(10, (await ingredients.GetAllAsync()).Count);
        Assert.Equal(2, (await recipes.GetAllAsync()).Count);
    }

    [Fact]
    public async Task Seed_DoesNotRunWhenUserDataExists()
    {
        var (ingredients, _, seed) = await CreateAsync();
        await ingredients.SaveAsync(NewIngredient("Kendi hammaddem"));

        var added = await seed.SeedIfEmptyAsync();

        Assert.False(added);
        Assert.Single(await ingredients.GetAllAsync());
    }

    [Fact]
    public async Task ClearAll_EmptiesEverything_AndSeedCanRunAgain()
    {
        var (ingredients, recipes, seed) = await CreateAsync();
        await seed.SeedIfEmptyAsync();

        await seed.ClearAllAsync();

        Assert.Empty(await ingredients.GetAllAsync());
        Assert.Empty(await recipes.GetAllAsync());
        Assert.True(await seed.SeedIfEmptyAsync());
    }
}
