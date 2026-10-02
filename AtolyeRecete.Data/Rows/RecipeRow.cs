using AtolyeRecete.Core.Models;
using SQLite;

namespace AtolyeRecete.Data.Rows;

[Table("Recipes")]
internal class RecipeRow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Yield { get; set; } = "1";
    public string LaborMinutes { get; set; } = "0";
    public string OverheadRate { get; set; } = "0";

    /// <summary>Satış fiyatı; girilmemişse null. (Eski veritabanlarında sütun otomatik eklenir.)</summary>
    public string? SalePrice { get; set; }

    public static RecipeRow From(Recipe model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Yield = DecimalText.ToDb(model.Yield),
        LaborMinutes = DecimalText.ToDb(model.LaborMinutes),
        OverheadRate = DecimalText.ToDb(model.OverheadRate),
        SalePrice = model.SalePrice is { } price ? DecimalText.ToDb(price) : null
    };

    /// <summary>Kalemler ayrı tabloda; depo doldurur.</summary>
    public Recipe ToModel() => new()
    {
        Id = Id,
        Name = Name,
        Yield = DecimalText.FromDb(Yield),
        LaborMinutes = DecimalText.FromDb(LaborMinutes),
        OverheadRate = DecimalText.FromDb(OverheadRate),
        SalePrice = SalePrice is null ? null : DecimalText.FromDb(SalePrice)
    };
}
