using AtolyeRecete.Core.Models;
using AtolyeRecete.Data.Rows;

namespace AtolyeRecete.Data.Repositories;

public class RecipeRepository(AtolyeDatabase db) : IRecipeRepository
{
    public async Task<List<Recipe>> GetAllAsync()
    {
        var ingredients = await LoadIngredientsAsync();
        var recipeRows = await db.Connection.Table<RecipeRow>().OrderBy(r => r.Name).ToListAsync();
        var itemRows = await db.Connection.Table<RecipeItemRow>().ToListAsync();

        return recipeRows.Select(r => Build(r, itemRows.Where(i => i.RecipeId == r.Id), ingredients)).ToList();
    }

    public async Task<Recipe?> GetAsync(int id)
    {
        var row = await db.Connection.FindAsync<RecipeRow>(id);
        if (row is null) return null;

        var ingredients = await LoadIngredientsAsync();
        var itemRows = await db.Connection.Table<RecipeItemRow>().Where(i => i.RecipeId == id).ToListAsync();
        return Build(row, itemRows, ingredients);
    }

    public async Task SaveAsync(Recipe recipe)
    {
        await db.Connection.RunInTransactionAsync(conn =>
        {
            var row = RecipeRow.From(recipe);
            if (row.Id == 0)
            {
                conn.Insert(row);
                recipe.Id = row.Id;
            }
            else
            {
                conn.Update(row);
                conn.Execute("DELETE FROM RecipeItems WHERE RecipeId = ?", recipe.Id);
            }

            foreach (var item in recipe.Items)
                conn.Insert(RecipeItemRow.From(recipe.Id, item));
        });
    }

    public async Task DeleteAsync(int id)
    {
        await db.Connection.RunInTransactionAsync(conn =>
        {
            conn.Execute("DELETE FROM RecipeItems WHERE RecipeId = ?", id);
            conn.Execute("DELETE FROM Recipes WHERE Id = ?", id);
        });
    }

    private async Task<Dictionary<int, Ingredient>> LoadIngredientsAsync()
    {
        var rows = await db.Connection.Table<IngredientRow>().ToListAsync();
        return rows.Select(r => r.ToModel()).ToDictionary(i => i.Id);
    }

    private static Recipe Build(
        RecipeRow row, IEnumerable<RecipeItemRow> itemRows, Dictionary<int, Ingredient> ingredients)
    {
        var recipe = row.ToModel();
        recipe.Items = itemRows.Select(i => i.ToModel(ingredients[i.IngredientId])).ToList();
        return recipe;
    }
}
