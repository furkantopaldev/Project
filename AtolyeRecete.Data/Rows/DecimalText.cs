using System.Globalization;

namespace AtolyeRecete.Data.Rows;

/// <summary>
/// sqlite-net decimal'i double olarak saklar; para/oran alanlarını kayıpsız tutmak için
/// tablolarda metin kullanılır.
/// </summary>
internal static class DecimalText
{
    public static string ToDb(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static decimal FromDb(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);
}
