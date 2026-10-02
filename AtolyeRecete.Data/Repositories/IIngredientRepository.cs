using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Data.Repositories;

public interface IIngredientRepository
{
    Task<List<Ingredient>> GetAllAsync();
    Task<Ingredient?> GetAsync(int id);

    /// <summary>Id 0 ise ekler (Id'yi doldurur), değilse günceller.</summary>
    Task SaveAsync(Ingredient ingredient);

    /// <summary>Bir reçetede kullanılan hammadde silinemez (<see cref="InvalidOperationException"/>).</summary>
    Task DeleteAsync(int id);
}
