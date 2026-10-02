namespace AtolyeRecete.Core;

/// <summary>
/// Önerilen fiyatın dökümü. <see cref="Price"/> 2 basamağa yuvarlanmış önerilen satış fiyatıdır;
/// diğer alanlar bu fiyat üzerinden, yuvarlanmadan hesaplanır (gösterimde yuvarlanır).
/// </summary>
/// <param name="Price">Önerilen satış fiyatı.</param>
/// <param name="UnitCost">Birim maliyet.</param>
/// <param name="Commission">Fiyat x komisyon oranı.</param>
/// <param name="FixedFee">Sabit ücretler (kargo + işlem).</param>
/// <param name="NetProfit">Fiyat - komisyon - sabit ücretler - maliyet.</param>
/// <param name="MarginRate">Net kâr / fiyat (satış fiyatı üzerinden).</param>
/// <param name="MarkupRate">Net kâr / maliyet (maliyet üzerinden).</param>
public record PriceBreakdown(
    decimal Price,
    decimal UnitCost,
    decimal Commission,
    decimal FixedFee,
    decimal NetProfit,
    decimal MarginRate,
    decimal MarkupRate);
