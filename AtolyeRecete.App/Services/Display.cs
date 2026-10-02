using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;

namespace AtolyeRecete.App.Services;

/// <summary>Arayüz metinleri: biçimlendirme ve girdi okuma. Hesap yok, sadece gösterim.</summary>
public static class Display
{
    public static string UnitLabel(UnitOfMeasure unit) => unit switch
    {
        UnitOfMeasure.Gram => "g",
        UnitOfMeasure.Milliliter => "ml",
        UnitOfMeasure.Piece => "adet",
        _ => ""
    };

    /// <summary>2 basamaklı para metni (yuvarlama sadece burada, gösterim için).</summary>
    public static string Money(decimal value) =>
        Core.Money.Round(value).ToString("C2", AppDefaults.Culture);

    /// <summary>Birim fiyat 0,30 gibi küçük olabilir; en fazla 4 basamak gösterilir.</summary>
    public static string UnitPrice(decimal value) =>
        "₺" + decimal.Round(value, 4, MidpointRounding.AwayFromZero).ToString("0.00##", AppDefaults.Culture);

    /// <summary>Ekranda yüzde (10), modelde kesir (0.10) tutulur.</summary>
    public static decimal RateFromPercent(decimal percent) => percent / 100m;

    public static string PercentText(decimal rate) =>
        (rate * 100m).ToString("0.##", AppDefaults.Culture);

    /// <summary>"%60,64" veya negatifse "-%77,1" (eksi işareti yüzde işaretinden önce).</summary>
    public static string PercentLabel(decimal rate) =>
        (rate < 0m ? "-" : "") + "%" + PercentText(Math.Abs(rate));

    public static string NumberText(decimal value) => value.ToString("0.##", AppDefaults.Culture);

    /// <summary>Canlı önizleme için: geçersiz veya boş girdi 0 sayılır.</summary>
    public static decimal ParseOrZero(string? text) => TryParseDecimal(text, out var v) ? v : 0m;

    /// <summary>
    /// Kullanıcı girdisini sayıya çevirir. "0,30" (Türkçe) ve "0.30" kabul edilir.
    /// Binlik ayracı desteklenmez.
    /// </summary>
    public static bool TryParseDecimal(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        const System.Globalization.NumberStyles style = System.Globalization.NumberStyles.AllowDecimalPoint;
        return decimal.TryParse(text.Trim(), style, AppDefaults.Culture, out value)
            || decimal.TryParse(text.Trim(), style, System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}
