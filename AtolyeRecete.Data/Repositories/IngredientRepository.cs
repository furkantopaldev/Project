using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;
using AtolyeRecete.Data.Rows;

namespace AtolyeRecete.Data.Repositories;

public class IngredientRepository(AtolyeDatabase db) : IIngredientRepository
{
    public async Task<List<Ingredient>> GetAllAsync()
    {
        var rows = await db.Connection.Table<IngredientRow>().ToListAsync();

        // Sıralama bellekte, Türkçe kurala göre (SQL ORDER BY Türkçe harfleri bilmez).
        return rows.Select(r => r.ToModel()).OrderBy(i => i.Name, TextOrder.Turkish).ToList();
    }

    public async Task<Ingredient?> GetAsync(int id)
    {
        var row = await db.Connection.FindAsync<IngredientRow>(id);
        return row?.ToModel();
    }

    public async Task SaveAsync(Ingredient ingredient)
    {
        var row = IngredientRow.From(ingredient);
        if (row.Id == 0)
        {
            await db.Connection.InsertAsync(row);
            ingredient.Id = row.Id;
        }
        else
        {
            await db.Connection.UpdateAsync(row);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var usedBy = await db.Connection.Table<RecipeItemRow>()
            .Where(i => i.IngredientId == id).CountAsync();
        if (usedBy > 0)
            throw new InvalidOperationException("Bu hammadde bir reçetede kullanılıyor, silinemez.");

        await db.Connection.DeleteAsync<IngredientRow>(id);
    }
}
