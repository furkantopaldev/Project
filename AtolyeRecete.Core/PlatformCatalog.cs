using System.Text.Json;
using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Core;

/// <summary>platforms.json içeriğini okur ve doğrular.</summary>
public static class PlatformCatalog
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    // Alanlar nullable: eksik alanın sessizce 0 sayılması yerine hata verebilmek için.
    private record PlatformDto(string? Name, decimal? CommissionRate, decimal? FixedFee, string? Notes);

    private record CatalogDto(List<PlatformDto>? Platforms);

    /// <summary>
    /// JSON metnini platform listesine çevirir. Geçersizse <see cref="FormatException"/> fırlatır.
    /// </summary>
    public static IReadOnlyList<Platform> Parse(string json)
    {
        CatalogDto? catalog;
        try
        {
            catalog = JsonSerializer.Deserialize<CatalogDto>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"platforms.json okunamadı: {ex.Message}", ex);
        }

        if (catalog?.Platforms is null || catalog.Platforms.Count == 0)
            throw new FormatException("platforms.json içinde en az bir platform olmalı.");

        var result = new List<Platform>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < catalog.Platforms.Count; i++)
        {
            var dto = catalog.Platforms[i];
            var where = $"platforms[{i}]";

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new FormatException($"{where}: 'name' boş olamaz.");
            var name = dto.Name.Trim();

            if (dto.CommissionRate is not { } rate)
                throw new FormatException($"{where} ('{name}'): 'commissionRate' eksik.");
            if (rate < 0m || rate >= 1m)
                throw new FormatException($"{where} ('{name}'): 'commissionRate' 0 ile 1 arasında olmalı (0.20 = %20).");

            if (dto.FixedFee is not { } fee)
                throw new FormatException($"{where} ('{name}'): 'fixedFee' eksik.");
            if (fee < 0m)
                throw new FormatException($"{where} ('{name}'): 'fixedFee' negatif olamaz.");

            if (!names.Add(name))
                throw new FormatException($"{where}: '{name}' adı birden fazla kez tanımlı.");

            result.Add(new Platform { Name = name, CommissionRate = rate, FixedFee = fee, Notes = dto.Notes });
        }

        return result;
    }
}
