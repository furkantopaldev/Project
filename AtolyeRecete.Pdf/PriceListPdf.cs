using System.Globalization;
using AtolyeRecete.Core;
using SkiaSharp;

namespace AtolyeRecete.Pdf;

/// <summary>
/// Müşteriye giden fiyat listesi PDF'i. Yalnızca ürün adı ve satış fiyatı içerir;
/// maliyet ve marj müşteriyle paylaşılmaz.
/// SkiaSharp ile çizilir: Windows ve Android'de aynı kod çalışır (QuestPDF mobilde çalışmıyor).
/// </summary>
public static class PriceListPdf
{
    private static readonly CultureInfo Turkish = new("tr-TR");

    // A4, nokta (1/72 inç) cinsinden.
    private const float PageWidth = 595f;
    private const float PageHeight = 842f;
    private const float Margin = 40f;
    private const float PriceColumnWidth = 120f;
    private const float RowPadding = 7f;

    private static readonly SKColor Dark = new(0x21, 0x21, 0x21);
    private static readonly SKColor Grey = new(0x6E, 0x6E, 0x6E);
    private static readonly SKColor LightGrey = new(0xD0, 0xD0, 0xD0);

    /// <param name="list">Listelenecek ürünler (<see cref="PriceListBuilder"/> çıktısı).</param>
    /// <param name="businessName">İşletme adı; boşsa başlık yalnızca "Fiyat Listesi" olur.</param>
    /// <param name="date">Listenin tarihi.</param>
    public static byte[] Create(PriceList list, string? businessName, DateOnly date)
    {
        var hasName = !string.IsNullOrWhiteSpace(businessName);
        var title = hasName ? businessName!.Trim() : "Fiyat Listesi";
        var dateText = date.ToString("d MMMM yyyy", Turkish);

        using var fonts = new Fonts();

        // 1) Satırları sar ve yüksekliklerini hesapla
        var nameWidth = PageWidth - 2 * Margin - PriceColumnWidth;
        var rows = list.Entries
            .Select(e =>
            {
                var lines = PriceListLayout.Wrap(e.Name, nameWidth, fonts.Body.Measure);
                return new LayoutRow(lines, FormatPrice(e.Price), lines.Count * fonts.Body.LineHeight + 2 * RowPadding);
            })
            .ToList();

        // 2) Sayfalara dağıt (üstbilgi ve altbilgi için yer ayır)
        var headerHeight = HeaderHeight(fonts, hasName);
        var tableHeaderHeight = fonts.BodyBold.LineHeight + 2 * RowPadding;
        var footerHeight = fonts.Small.LineHeight + 10f;
        var available = PageHeight - 2 * Margin - headerHeight - tableHeaderHeight - footerHeight;
        var pages = PriceListLayout.Paginate(rows, available);

        // 3) Çiz
        using var stream = new SKDynamicMemoryWStream();
        using (var document = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata
        {
            Title = hasName ? $"{title} - Fiyat Listesi" : title,
            Creator = "AtölyeReçete"
        }))
        {
            for (var i = 0; i < pages.Count; i++)
            {
                var canvas = document.BeginPage(PageWidth, PageHeight);
                var y = DrawHeader(canvas, fonts, title, hasName, dateText);

                if (list.Entries.Count == 0)
                    fonts.Body.Draw(canvas, "Listelenecek ürün yok.", Margin, y + fonts.Body.Ascent, Grey);
                else
                    DrawTable(canvas, fonts, pages[i], y);

                DrawFooter(canvas, fonts, i + 1, pages.Count);
                document.EndPage();
            }
            document.Close();
        }

        using var data = stream.DetachAsData();
        return data.ToArray();
    }

    private static string FormatPrice(decimal price) => Money.Round(price).ToString("C2", Turkish);

    private static float HeaderHeight(Fonts fonts, bool hasName) =>
        fonts.Title.LineHeight
        + (hasName ? fonts.Subtitle.LineHeight : 0f)
        + fonts.Small.LineHeight + 20f;

    /// <summary>Başlığı çizer; tablonun başlayacağı y konumunu döner.</summary>
    private static float DrawHeader(SKCanvas canvas, Fonts fonts, string title, bool hasName, string dateText)
    {
        var y = Margin;

        y += fonts.Title.Ascent;
        fonts.Title.Draw(canvas, title, Margin, y, Dark);
        y += fonts.Title.Descent;

        if (hasName)
        {
            y += fonts.Subtitle.Ascent;
            fonts.Subtitle.Draw(canvas, "Fiyat Listesi", Margin, y, Grey);
            y += fonts.Subtitle.Descent;
        }

        y += fonts.Small.Ascent;
        fonts.Small.Draw(canvas, dateText, Margin, y, Grey);
        y += fonts.Small.Descent;

        return y + 20f;
    }

    private static void DrawTable(SKCanvas canvas, Fonts fonts, List<LayoutRow> rows, float top)
    {
        var right = PageWidth - Margin;
        using var heavy = new SKPaint { Color = Dark, StrokeWidth = 1.5f, IsStroke = true };
        using var light = new SKPaint { Color = LightGrey, StrokeWidth = 1f, IsStroke = true };

        // Tablo başlığı
        var y = top + RowPadding;
        fonts.BodyBold.Draw(canvas, "Ürün", Margin, y + fonts.BodyBold.Ascent, Dark);
        fonts.BodyBold.DrawRight(canvas, "Fiyat", right, y + fonts.BodyBold.Ascent, Dark);
        y += fonts.BodyBold.LineHeight + RowPadding;
        canvas.DrawLine(Margin, y, right, y, heavy);

        // Satırlar
        foreach (var row in rows)
        {
            var baseline = y + RowPadding + fonts.Body.Ascent;
            for (var i = 0; i < row.NameLines.Count; i++)
                fonts.Body.Draw(canvas, row.NameLines[i], Margin, baseline + i * fonts.Body.LineHeight, Dark);

            fonts.Body.DrawRight(canvas, row.PriceText, right, baseline, Dark);

            y += row.Height;
            canvas.DrawLine(Margin, y, right, y, light);
        }
    }

    private static void DrawFooter(SKCanvas canvas, Fonts fonts, int pageNumber, int pageCount)
    {
        var baseline = PageHeight - Margin;
        fonts.Small.Draw(canvas, "Fiyatlar KDV hariçtir.", Margin, baseline, Grey);
        fonts.Small.DrawRight(canvas, $"Sayfa {pageNumber} / {pageCount}", PageWidth - Margin, baseline, Grey);
    }

    /// <summary>Boyut ve kalınlığa göre yazı tipleri (belge boyunca bir kez oluşturulur).</summary>
    private sealed class Fonts : IDisposable
    {
        public TextStyle Title { get; } = new(26f, bold: true);
        public TextStyle Subtitle { get; } = new(14f, bold: false);
        public TextStyle Body { get; } = new(12f, bold: false);
        public TextStyle BodyBold { get; } = new(12f, bold: true);
        public TextStyle Small { get; } = new(10f, bold: false);

        public void Dispose()
        {
            Title.Dispose();
            Subtitle.Dispose();
            Body.Dispose();
            BodyBold.Dispose();
            Small.Dispose();
        }
    }
}
