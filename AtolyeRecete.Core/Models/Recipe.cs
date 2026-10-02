namespace AtolyeRecete.Core.Models;

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<RecipeItem> Items { get; set; } = new();

    /// <summary>Bu reçeteden kaç adet ürün çıktığı.</summary>
    public decimal Yield { get; set; } = 1;

    /// <summary>Tüm parti için işçilik süresi (dakika).</summary>
    public decimal LaborMinutes { get; set; }

    /// <summary>Genel gider oranı, kesir olarak (0.10 = %10), (malzeme + işçilik) üzerinden.</summary>
    public decimal OverheadRate { get; set; }

    /// <summary>Ürünün şu anki satış fiyatı (KDV hariç). Girilmemişse null.</summary>
    public decimal? SalePrice { get; set; }
}
