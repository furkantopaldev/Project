namespace AtolyeRecete.Core.Models;

public class RecipeItem
{
    public required Ingredient Ingredient { get; set; }

    /// <summary>Kullanılan miktar, hammaddenin biriminde.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Kalem fire oranı, kesir olarak (0.05 = %5).</summary>
    public decimal WasteRate { get; set; }
}
