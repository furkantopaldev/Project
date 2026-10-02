using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Core;

/// <summary>Müşteriye giden fiyat listesindeki bir satır. Maliyet ve marj içermez.</summary>
public record PriceListEntry(string Name, decimal Price);

/// <summary>Fiyat listesi: girenler ve fiyatı olmadığı için girmeyen reçete sayısı.</summary>
public record PriceList(IReadOnlyList<PriceListEntry> Entries, int SkippedCount);

public static class PriceListBuilder
{
    /// <summary>
    /// Yalnızca satış fiyatı girilmiş (&gt; 0) reçeteleri alır, Türkçe alfabeye göre ada göre sıralar.
    /// Fiyatı olmayanlar listeye girmez, sayısı <see cref="PriceList.SkippedCount"/> olarak döner.
    /// </summary>
    public static PriceList Build(IEnumerable<Recipe> recipes)
    {
        var entries = new List<PriceListEntry>();
        var skipped = 0;

        foreach (var recipe in recipes)
        {
            if (recipe.SalePrice is { } price and > 0m)
                entries.Add(new PriceListEntry(recipe.Name.Trim(), price));
            else
                skipped++;
        }

        // Türkçe alfabe: ç, ğ, ı, ö, ş, ü doğru yerlerine oturur; büyük/küçük harf fark etmez.
        entries.Sort((a, b) => TextOrder.Turkish.Compare(a.Name, b.Name));

        return new PriceList(entries, skipped);
    }
}
