using System.Globalization;

namespace AtolyeRecete.Core;

/// <summary>
/// Kullanıcıya görünen listelerin sıralama kuralı. Türkçe alfabe (ç, ğ, ı, ö, ş, ü doğru yerde),
/// büyük/küçük harf fark etmez. SQLite'ın varsayılan sıralaması (BINARY) bunu yapmaz:
/// büyük harfi küçüğün önüne koyar, ç/ş/ğ'yi en sona atar; bu yüzden sıralama veritabanında değil burada yapılır.
/// </summary>
public static class TextOrder
{
    public static readonly StringComparer Turkish =
        StringComparer.Create(new CultureInfo("tr-TR"), ignoreCase: true);
}
