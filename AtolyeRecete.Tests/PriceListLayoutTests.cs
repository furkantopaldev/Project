using AtolyeRecete.Pdf;

namespace AtolyeRecete.Tests;

public class PriceListLayoutTests
{
    /// <summary>Gerçek yazı tipi gerekmez: her harf 10 birim genişliğinde.</summary>
    private static float Measure(string text) => text.Length * 10f;

    private static LayoutRow Row(float height, string name = "x") => new(new[] { name }, "₺1,00", height);

    // --- Satır sarma ---

    [Fact]
    public void Wrap_ShortText_IsOneLine() =>
        Assert.Equal(new[] { "Lavanta mumu" }, PriceListLayout.Wrap("Lavanta mumu", 500f, Measure));

    [Fact]
    public void Wrap_BreaksAtWordBoundaries()
    {
        // 100 birim = 10 harf. "aaaa bbbb" 9 harf sığar; "cccc" yeni satıra geçer.
        var lines = PriceListLayout.Wrap("aaaa bbbb cccc", 100f, Measure);

        Assert.Equal(new[] { "aaaa bbbb", "cccc" }, lines);
    }

    [Fact]
    public void Wrap_NoLineExceedsMaxWidth()
    {
        var lines = PriceListLayout.Wrap("Çam ve ardıç ıslak toprak kokulu büyük boy aromaterapi mumu", 120f, Measure);

        Assert.True(lines.Count > 1);
        Assert.All(lines, l => Assert.True(Measure(l) <= 120f, $"'{l}' çok geniş"));
    }

    [Fact]
    public void Wrap_VeryLongWord_IsCutByLetters()
    {
        var lines = PriceListLayout.Wrap("abcdefghijklmnopqrstuvwxyz", 100f, Measure);

        Assert.All(lines, l => Assert.True(Measure(l) <= 100f));
        Assert.Equal("abcdefghijklmnopqrstuvwxyz", string.Concat(lines)); // hiçbir harf kaybolmaz
    }

    [Fact]
    public void Wrap_EmptyText_GivesSingleEmptyLine() =>
        Assert.Equal(new[] { "" }, PriceListLayout.Wrap("", 100f, Measure));

    [Fact]
    public void Wrap_CollapsesExtraSpaces() =>
        Assert.Equal(new[] { "a b" }, PriceListLayout.Wrap("  a   b  ", 100f, Measure));

    // --- Sayfalama ---

    [Fact]
    public void Paginate_NoRows_GivesOneEmptyPage()
    {
        var pages = PriceListLayout.Paginate(Array.Empty<LayoutRow>(), 100f);

        Assert.Single(pages);
        Assert.Empty(pages[0]);
    }

    [Fact]
    public void Paginate_RowsThatFit_StayOnOnePage()
    {
        var pages = PriceListLayout.Paginate(new[] { Row(30), Row(30), Row(40) }, 100f);

        Assert.Single(pages);
        Assert.Equal(3, pages[0].Count);
    }

    [Fact]
    public void Paginate_Overflow_StartsNewPage_AndKeepsOrder()
    {
        var rows = Enumerable.Range(1, 5).Select(i => Row(40, $"r{i}")).ToArray();

        var pages = PriceListLayout.Paginate(rows, 100f); // sayfa başına 2 satır (80 <= 100 < 120)

        Assert.Equal(3, pages.Count);
        Assert.Equal(new[] { 2, 2, 1 }, pages.Select(p => p.Count));
        Assert.Equal(
            new[] { "r1", "r2", "r3", "r4", "r5" },
            pages.SelectMany(p => p).Select(r => r.NameLines[0]));
    }

    [Fact]
    public void Paginate_ExactFit_DoesNotCreateExtraPage()
    {
        var pages = PriceListLayout.Paginate(new[] { Row(50), Row(50) }, 100f);

        Assert.Single(pages);
    }

    [Fact]
    public void Paginate_NoPageExceedsAvailableHeight()
    {
        var rows = Enumerable.Range(1, 60).Select(i => Row(20 + i % 7)).ToArray();

        var pages = PriceListLayout.Paginate(rows, 200f);

        Assert.True(pages.Count > 1);
        Assert.All(pages, p => Assert.True(p.Sum(r => r.Height) <= 200f));
        Assert.Equal(60, pages.Sum(p => p.Count));
    }

    [Fact]
    public void Paginate_OversizedRow_GetsItsOwnPageWithoutLoopingForever()
    {
        var pages = PriceListLayout.Paginate(new[] { Row(10), Row(500), Row(10) }, 100f);

        Assert.Equal(3, pages.Count);
        Assert.Equal(500f, pages[1][0].Height);
    }

    // --- Yazı tipi kapsamı ---

    [Fact]
    public void Font_CanRenderTurkishLettersAndLiraSign()
    {
        // Eksik harf PDF'te kutu (□) olarak çıkar. Bu makinedeki yazı tipleri ç ğ ı İ ö ş ü ve ₺'yi çizebilmeli.
        using var body = new TextStyle(12f, bold: false);
        using var bold = new TextStyle(12f, bold: true);
        const string text = "çğıİöşü ÇĞIÖŞÜ ₺1.234,56 %60,64 • ⚠";

        Assert.True(body.CanRender(text.Replace("•", "").Replace("⚠", "")));
        Assert.True(bold.CanRender("Şükran'ın Mum Atölyesi ₺"));
    }

    [Fact]
    public void Font_BoldStyle_IsReallyBold_AndRegularIsNot()
    {
        using var regular = new TextStyle(12f, bold: false);
        using var bold = new TextStyle(12f, bold: true);

        Assert.True(bold.IsBold);
        Assert.False(regular.IsBold);
    }

    [Fact]
    public void Font_MeasureGrowsWithText()
    {
        using var style = new TextStyle(12f, bold: false);

        Assert.True(style.Measure("aaaa") > style.Measure("aa"));
        Assert.True(style.Measure("") == 0f);
    }
}
