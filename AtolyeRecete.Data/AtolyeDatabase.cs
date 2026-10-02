using AtolyeRecete.Data.Rows;
using SQLite;

namespace AtolyeRecete.Data;

/// <summary>SQLite bağlantısı ve şema kurulumu. Depolar bunu paylaşır.</summary>
public class AtolyeDatabase
{
    internal SQLiteAsyncConnection Connection { get; }

    /// <param name="path">Veritabanı dosya yolu.</param>
    public AtolyeDatabase(string path) => Connection = new SQLiteAsyncConnection(path);

    private AtolyeDatabase(SQLiteAsyncConnection connection) => Connection = connection;

    private const SQLiteOpenFlags OpenUri = (SQLiteOpenFlags)0x40; // SQLITE_OPEN_URI

    /// <summary>
    /// Her çağrıda ayrı, boş bir bellek içi veritabanı (testler için). SQLiteAsyncConnection aynı
    /// bağlantı dizesi için ortak bir havuz kullandığından düz ":memory:" tüm çağrılarda aynı
    /// veritabanını verir; bu yüzden her seferinde benzersiz adlı bir URI kullanılır.
    /// </summary>
    public static AtolyeDatabase CreateInMemory() =>
        new(new SQLiteAsyncConnection(
            $"file:mem-{Guid.NewGuid():N}?mode=memory",
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | OpenUri));

    public async Task InitializeAsync()
    {
        await Connection.CreateTableAsync<IngredientRow>();
        await Connection.CreateTableAsync<RecipeRow>();
        await Connection.CreateTableAsync<RecipeItemRow>();
    }
}
