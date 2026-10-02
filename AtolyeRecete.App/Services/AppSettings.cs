using System.Globalization;

namespace AtolyeRecete.App.Services;

/// <summary>
/// Kullanıcı ayarları (MAUI Preferences). Hesaplayan her ekran ücreti buradan okur;
/// böylece ayar değişince tüm reçeteler bir sonraki hesapta yeni değeri kullanır.
/// </summary>
public class AppSettings
{
    public const decimal DefaultHourlyRate = 150m;
    public const decimal DefaultOverheadPercent = 10m;
    public const decimal DefaultMarginWarningPercent = 30m;

    private const string HourlyRateKey = "hourlyRate";
    private const string OverheadKey = "defaultOverheadPercent";
    private const string MarginWarningKey = "marginWarningPercent";
    private const string BusinessNameKey = "businessName";
    private const string SeedAppliedKey = "seedApplied";

    /// <summary>Saatlik işçilik ücreti (₺). İşçilik maliyeti bununla hesaplanır.</summary>
    public decimal HourlyRate
    {
        get => ReadDecimal(HourlyRateKey, DefaultHourlyRate);
        set => WriteDecimal(HourlyRateKey, value);
    }

    /// <summary>Yeni reçetede önerilen genel gider yüzdesi. Mevcut reçeteleri etkilemez.</summary>
    public decimal DefaultOverheadPercentValue
    {
        get => ReadDecimal(OverheadKey, DefaultOverheadPercent);
        set => WriteDecimal(OverheadKey, value);
    }

    /// <summary>Satış fiyatındaki marj bu yüzdenin altına düşerse uyarı gösterilir.</summary>
    public decimal MarginWarningPercentValue
    {
        get => ReadDecimal(MarginWarningKey, DefaultMarginWarningPercent);
        set => WriteDecimal(MarginWarningKey, value);
    }

    /// <summary>İşletme adı; müşteriye giden fiyat listesi PDF'inin başlığında görünür. Boş olabilir.</summary>
    public string BusinessName
    {
        get => Preferences.Default.Get(BusinessNameKey, "");
        set => Preferences.Default.Set(BusinessNameKey, value ?? "");
    }

    /// <summary>
    /// Örnek veri paketi bir kez denendi mi? Silinen örnek verinin bir sonraki açılışta
    /// geri gelmemesi için tutulur.
    /// </summary>
    public bool SeedApplied
    {
        get => Preferences.Default.Get(SeedAppliedKey, false);
        set => Preferences.Default.Set(SeedAppliedKey, value);
    }

    // Preferences decimal desteklemez; double'a çevirmemek için metin olarak saklanır.
    private static decimal ReadDecimal(string key, decimal fallback)
    {
        var text = Preferences.Default.Get(key, "");
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static void WriteDecimal(string key, decimal value) =>
        Preferences.Default.Set(key, value.ToString(CultureInfo.InvariantCulture));
}
