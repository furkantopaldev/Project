using System.Globalization;

namespace AtolyeRecete.App.Services;

/// <summary>
/// Ayarlar ekranı (Faz 4) gelene kadar sabit varsayılanlar.
/// </summary>
public static class AppDefaults
{
    /// <summary>Saatlik işçilik ücreti (TL).</summary>
    public const decimal HourlyRate = 150m;

    public static readonly CultureInfo Culture = new("tr-TR");
}
