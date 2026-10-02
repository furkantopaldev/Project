namespace AtolyeRecete.Core.Models;

public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public UnitOfMeasure Unit { get; set; }

    /// <summary>Toplam alış fiyatı (örn. 1000 g için 300).</summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>Alış miktarı, <see cref="Unit"/> cinsinden (örn. 1000).</summary>
    public decimal PurchaseQuantity { get; set; }

    /// <summary>Birim fiyat = alış fiyatı / alış miktarı. Yuvarlanmaz.</summary>
    public decimal UnitPrice =>
        PurchaseQuantity > 0
            ? PurchasePrice / PurchaseQuantity
            : throw new InvalidOperationException(
                $"'{Name}' için alış miktarı sıfırdan büyük olmalı.");
}
