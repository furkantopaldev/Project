using AtolyeRecete.Core.Models;
using SQLite;

namespace AtolyeRecete.Data.Rows;

[Table("RecipeItems")]
internal class RecipeItemRow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int RecipeId { get; set; }
    [Indexed]
    public int IngredientId { get; set; }
    public string Quantity { get; set; } = "0";
    public string WasteRate { get; set; } = "0";

    public static RecipeItemRow From(int recipeId, RecipeItem model) => new()
    {
        RecipeId = recipeId,
        IngredientId = model.Ingredient.Id,
        Quantity = DecimalText.ToDb(model.Quantity),
        WasteRate = DecimalText.ToDb(model.WasteRate)
    };

    public RecipeItem ToModel(Ingredient ingredient) => new()
    {
        Ingredient = ingredient,
        Quantity = DecimalText.FromDb(Quantity),
        WasteRate = DecimalText.FromDb(WasteRate)
    };
}
