using AtolyeRecete.Data;

namespace AtolyeRecete.App.Services;

/// <summary>
/// Veritabanını kurar ve boşsa örnek veriyi yükler. Birden çok ekran çağırabilir; iş bir kez yapılır.
/// </summary>
public class StartupService(AtolyeDatabase db, SeedService seed)
{
    private Task? _initialization;

    public Task EnsureInitializedAsync() => _initialization ??= InitializeAsync();

    private async Task InitializeAsync()
    {
        await db.InitializeAsync();
        await seed.SeedIfEmptyAsync();
    }
}
