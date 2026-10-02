using AtolyeRecete.Core.Models;
using SQLite;

namespace AtolyeRecete.Data.Rows;

[Table("Ingredients")]
internal class IngredientRow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Unit { get; set; }
    public string PurchasePrice { get; set; } = "0";
    public string PurchaseQuantity { get; set; } = "1";

    public static IngredientRow From(Ingredient model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Unit = (int)model.Unit,
        PurchasePrice = DecimalText.ToDb(model.PurchasePrice),
        PurchaseQuantity = DecimalText.ToDb(model.PurchaseQuantity)
    };

    public Ingredient ToModel() => new()
    {
        Id = Id,
        Name = Name,
        Unit = (UnitOfMeasure)Unit,
        PurchasePrice = DecimalText.FromDb(PurchasePrice),
        PurchaseQuantity = DecimalText.FromDb(PurchaseQuantity)
    };
}
