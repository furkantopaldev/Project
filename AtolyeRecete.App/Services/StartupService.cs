using AtolyeRecete.Data;

namespace AtolyeRecete.App.Services;

/// <summary>
/// Veritabanını kurar ve ilk çalıştırmada örnek veriyi yükler. Birden çok ekran çağırabilir; iş bir kez yapılır.
/// </summary>
public class StartupService(AtolyeDatabase db, SeedService seed, AppSettings settings)
{
    private Task? _initialization;

    public Task EnsureInitializedAsync() => _initialization ??= InitializeAsync();

    private async Task InitializeAsync()
    {
        await db.InitializeAsync();

        // Örnek veri yalnızca ilk çalıştırmada: kullanıcı silerse bir sonraki açılışta geri gelmez.
        if (!settings.SeedApplied)
        {
            await seed.SeedIfEmptyAsync();
            settings.SeedApplied = true;
        }
    }
}
