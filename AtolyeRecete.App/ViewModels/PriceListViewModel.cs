using System.Collections.ObjectModel;
using AtolyeRecete.App.Services;
using AtolyeRecete.Core;
using AtolyeRecete.Data.Repositories;
using AtolyeRecete.Pdf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtolyeRecete.App.ViewModels;

public record PriceListRow(string Name, string PriceText);

public partial class PriceListViewModel(
    StartupService startup,
    AppSettings settings,
    IRecipeRepository recipes) : ObservableObject
{
    public ObservableCollection<PriceListRow> Rows { get; } = new();

    [ObservableProperty] private string _summary = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private int _entryCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPdf))]
    private string _pdfPath = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _statusMessage = "";

    public bool CanCreate => EntryCount > 0;
    public bool HasPdf => !string.IsNullOrEmpty(PdfPath);
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    [RelayCommand]
    private async Task LoadAsync()
    {
        await startup.EnsureInitializedAsync();
        await RefreshAsync();
    }

    /// <summary>Güncel reçetelerden listeyi kurar (PDF'e giren satırların önizlemesi).</summary>
    private async Task<PriceList> RefreshAsync()
    {
        var list = PriceListBuilder.Build(await recipes.GetAllAsync());

        Rows.Clear();
        foreach (var e in list.Entries)
            Rows.Add(new PriceListRow(e.Name, Display.Money(e.Price)));
        EntryCount = list.Entries.Count;

        Summary = list.Entries.Count == 0
            ? "Listelenecek ürün yok. Reçetelere satış fiyatı girince burada görünür."
            : $"{list.Entries.Count} ürün listede."
              + (list.SkippedCount > 0
                  ? $" {list.SkippedCount} reçete satış fiyatı girilmediği için listeye girmiyor."
                  : "");
        return list;
    }

    [RelayCommand]
    private async Task CreatePdfAsync()
    {
        ErrorMessage = "";
        StatusMessage = "";

        var list = await RefreshAsync();
        if (list.Entries.Count == 0)
        {
            ErrorMessage = "Listelenecek ürün yok; önce reçetelere satış fiyatı gir.";
            return;
        }

        try
        {
            var bytes = PriceListPdf.Create(list, settings.BusinessName, DateOnly.FromDateTime(DateTime.Today));

            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AtolyeRecete");
            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, $"Fiyat-Listesi-{DateTime.Today:yyyy-MM-dd}.pdf");
            await File.WriteAllBytesAsync(path, bytes);

            PdfPath = path;
            StatusMessage = "PDF oluşturuldu.";
        }
        catch (IOException ex)
        {
            // Aynı günkü dosya PDF okuyucuda açıksa üzerine yazılamaz.
            ErrorMessage = $"PDF kaydedilemedi: {ex.Message} Dosya başka bir programda açıksa kapatıp tekrar dene.";
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = $"PDF kaydedilemedi: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (!HasPdf) return;
        ErrorMessage = "";

        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest("Fiyat listesi", new ReadOnlyFile(PdfPath)));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"PDF açılamadı: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (!HasPdf) return;
        ErrorMessage = "";

        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest("Fiyat listesi", new ShareFile(PdfPath)));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Paylaşılamadı: {ex.Message} Dosyayı \"Aç\" ile açıp oradan gönderebilirsin.";
        }
    }
}
