using System.Text;
using AtolyeRecete.Core;
using AtolyeRecete.Pdf;

namespace AtolyeRecete.Tests;

public class PriceListPdfTests
{
    private static readonly DateOnly Date = new(2026, 10, 2);

    private static PriceList ListOf(params (string Name, decimal Price)[] items) =>
        new(items.Select(i => new PriceListEntry(i.Name, i.Price)).ToList(), 0);

    private static bool IsPdf(byte[] bytes) =>
        bytes.Length > 5 && Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-";

    [Fact]
    public void Create_ProducesPdfBytes()
    {
        var bytes = PriceListPdf.Create(ListOf(("200cc aromaterapi mumu", 450m)), "Atölye Mumluk", Date);

        Assert.True(IsPdf(bytes));
        Assert.True(bytes.Length > 1000);
    }

    [Fact]
    public void Create_TurkishCharactersAndLiraSign_DoNotThrow()
    {
        // QuestPDF, yazı tipinde eksik glifte hata verir: ç ğ ı İ ö ş ü ve ₺ doğrulanır.
        var list = ListOf(
            ("Çam & Ardıç ıslak toprak mumu", 125.5m),
            ("İstanbul Şehir Gökyüzü Öğle Üzüm", 1999.99m),
            ("ğüşiöçı ĞÜŞİÖÇI", 10m));

        var bytes = PriceListPdf.Create(list, "Şükran'ın Mum Atölyesi İstanbul", Date);

        Assert.True(IsPdf(bytes));
    }

    [Fact]
    public void Create_WithoutBusinessName_Works()
    {
        Assert.True(IsPdf(PriceListPdf.Create(ListOf(("A", 1m)), null, Date)));
        Assert.True(IsPdf(PriceListPdf.Create(ListOf(("A", 1m)), "   ", Date)));
    }

    [Fact]
    public void Create_EmptyList_Works() =>
        Assert.True(IsPdf(PriceListPdf.Create(new PriceList(Array.Empty<PriceListEntry>(), 3), "X", Date)));

    [Fact]
    public void Create_ManyEntries_SpansMultiplePages()
    {
        var many = Enumerable.Range(1, 120).Select(i => ($"Ürün {i:000}", (decimal)i)).ToArray();

        var one = PriceListPdf.Create(ListOf(("Ürün 001", 1m)), "X", Date);
        var multi = PriceListPdf.Create(ListOf(many), "X", Date);

        Assert.True(IsPdf(multi));
        Assert.True(multi.Length > one.Length);
    }
}
