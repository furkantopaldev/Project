using AtolyeRecete.Data.Rows;
using SQLite;

namespace AtolyeRecete.Data;

/// <summary>SQLite bağlantısı ve şema kurulumu. Depolar bunu paylaşır.</summary>
public class AtolyeDatabase
{
    internal SQLiteAsyncConnection Connection { get; }

    /// <param name="path">Dosya yolu veya ":memory:" (testler için).</param>
    public AtolyeDatabase(string path) => Connection = new SQLiteAsyncConnection(path);

    public async Task InitializeAsync()
    {
        await Connection.CreateTableAsync<IngredientRow>();
        await Connection.CreateTableAsync<RecipeRow>();
        await Connection.CreateTableAsync<RecipeItemRow>();
    }
}
