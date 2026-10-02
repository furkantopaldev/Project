namespace AtolyeRecete.Pdf;

/// <summary>Tabloda tek bir ürün satırı: uzun adlar için birden çok satıra bölünmüş ad ve fiyat.</summary>
internal record LayoutRow(IReadOnlyList<string> NameLines, string PriceText, float Height);

/// <summary>
/// Sayfalama ve satır sarma. Çizimden bağımsızdır: yazı ölçümü dışarıdan verilir,
/// böylece gerçek yazı tipi olmadan test edilebilir.
/// </summary>
internal static class PriceListLayout
{
    /// <summary>
    /// Metni verilen genişliğe sığacak satırlara böler (kelime sınırında). Tek bir kelime sığmazsa
    /// harf sınırında keser. Boş metin için tek boş satır döner.
    /// </summary>
    /// <param name="measure">Bir metnin genişliğini ölçer.</param>
    public static IReadOnlyList<string> Wrap(string text, float maxWidth, Func<string, float> measure)
    {
        var lines = new List<string>();
        var current = "";

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (measure(candidate) <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
            {
                lines.Add(current);
                current = "";
            }

            // Tek başına sığmayan uzun kelime: harf harf kes.
            var rest = word;
            while (measure(rest) > maxWidth && rest.Length > 1)
            {
                var cut = rest.Length - 1;
                while (cut > 1 && measure(rest[..cut]) > maxWidth) cut--;
                lines.Add(rest[..cut]);
                rest = rest[cut..];
            }
            current = rest;
        }

        if (current.Length > 0 || lines.Count == 0)
            lines.Add(current);
        return lines;
    }

    /// <summary>
    /// Satırları, her sayfada <paramref name="availableHeight"/> aşılmayacak şekilde sayfalara dağıtır.
    /// Bir satır tek başına sığmayacak kadar yüksekse yine de kendi sayfasına konur.
    /// Satır yoksa bile tek (boş) sayfa döner.
    /// </summary>
    public static List<List<LayoutRow>> Paginate(IReadOnlyList<LayoutRow> rows, float availableHeight)
    {
        var pages = new List<List<LayoutRow>> { new() };
        var used = 0f;

        foreach (var row in rows)
        {
            var page = pages[^1];
            if (page.Count > 0 && used + row.Height > availableHeight)
            {
                page = new List<LayoutRow>();
                pages.Add(page);
                used = 0f;
            }
            page.Add(row);
            used += row.Height;
        }

        return pages;
    }
}
