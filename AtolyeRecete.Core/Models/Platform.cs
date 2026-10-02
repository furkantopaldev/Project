namespace AtolyeRecete.Core.Models;

public class Platform
{
    public string Name { get; set; } = "";

    /// <summary>Komisyon oranı, kesir olarak (0.20 = %20).</summary>
    public decimal CommissionRate { get; set; }

    /// <summary>Satış başına sabit ücretler toplamı (kargo + işlem ücreti).</summary>
    public decimal FixedFee { get; set; }

    public string? Notes { get; set; }
}
