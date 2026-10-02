using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;
using Platform = AtolyeRecete.Core.Models.Platform; // MAUI'nin kendi Platform sınıfıyla çakışmayı önler

namespace AtolyeRecete.App.Services;

/// <summary>
/// Komisyon tablosunu (Resources/Raw/platforms.json) yükler. Oranlar koda gömülü değildir;
/// ileride uzaktan çekilebilmesi için okuma tek yerde toplandı.
/// </summary>
public class PlatformService
{
    private IReadOnlyList<Platform>? _cache;

    /// <summary>Dosya okunamaz veya geçersizse <see cref="FormatException"/> fırlatır.</summary>
    public async Task<IReadOnlyList<Platform>> GetAllAsync()
    {
        if (_cache is not null) return _cache;

        await using var stream = await FileSystem.OpenAppPackageFileAsync("platforms.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        return _cache = PlatformCatalog.Parse(json);
    }
}
