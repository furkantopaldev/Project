using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Data.Repositories;

public interface IRecipeRepository
{
    /// <summary>Kalemleriyle birlikte, hammaddeler güncel fiyatlarıyla bağlı gelir.</summary>
    Task<List<Recipe>> GetAllAsync();
    Task<Recipe?> GetAsync(int id);

    /// <summary>Id 0 ise ekler (Id'yi doldurur), değilse günceller. Kalemler baştan yazılır.</summary>
    Task SaveAsync(Recipe recipe);
    Task DeleteAsync(int id);
}
