using System.Globalization;
using AtolyeRecete.Core;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AtolyeRecete.Pdf;

/// <summary>
/// Müşteriye giden fiyat listesi PDF'i. Yalnızca ürün adı ve satış fiyatı içerir;
/// maliyet ve marj müşteriyle paylaşılmaz.
/// </summary>
public static class PriceListPdf
{
    private static readonly CultureInfo Turkish = new("tr-TR");

    static PriceListPdf()
    {
        // QuestPDF Community lisansı: yıllık geliri 1 milyon USD'nin altındaki işletmeler,
        // öğrenciler, kâr amacı gütmeyenler için ücretsizdir. Ticari büyümede lisans yeniden değerlendirilir.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <param name="list">Listelenecek ürünler (<see cref="PriceListBuilder"/> çıktısı).</param>
    /// <param name="businessName">İşletme adı; boşsa başlık yalnızca "Fiyat Listesi" olur.</param>
    /// <param name="date">Listenin tarihi.</param>
    public static byte[] Create(PriceList list, string? businessName, DateOnly date)
    {
        var hasName = !string.IsNullOrWhiteSpace(businessName);
        var title = hasName ? businessName!.Trim() : "Fiyat Listesi";
        var dateText = date.ToString("d MMMM yyyy", Turkish);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().PaddingBottom(16).Column(column =>
                {
                    column.Item().Text(title).FontSize(26).Bold();
                    if (hasName)
                        column.Item().Text("Fiyat Listesi").FontSize(14).FontColor(Colors.Grey.Darken1);
                    column.Item().Text(dateText).FontSize(11).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Element(content => Content(content, list));

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("Fiyatlar KDV hariçtir.").FontSize(10).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken1));
                        text.Span("Sayfa ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void Content(IContainer container, PriceList list)
    {
        if (list.Entries.Count == 0)
        {
            container.Text("Listelenecek ürün yok.").FontColor(Colors.Grey.Darken1);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(110);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Ürün").Bold();
                header.Cell().Element(HeaderCell).AlignRight().Text("Fiyat").Bold();
            });

            foreach (var entry in list.Entries)
            {
                table.Cell().Element(BodyCell).Text(entry.Name);
                table.Cell().Element(BodyCell).AlignRight().Text(FormatPrice(entry.Price));
            }
        });
    }

    private static string FormatPrice(decimal price) => Money.Round(price).ToString("C2", Turkish);

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1.5f).BorderColor(Colors.Grey.Darken2).PaddingVertical(6);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(7);
}
