using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Tests;

public class MarginAlertsTests
{
    private const decimal HourlyRate = 150m;
    private const decimal Threshold = 0.30m;

    /// <summary>
    /// 100 g mum kullanan basit reçete: işçilik ve genel gider yok, verim 1.
    /// Mum fiyatı 1000 g için <paramref name="waxPrice"/> TL; maliyet = waxPrice / 10.
    /// </summary>
    private static Recipe Make(decimal waxPrice, decimal? salePrice = 100m, int id = 1, string name = "A") => new()
    {
        Id = id,
        Name = name,
        Yield = 1m,
        SalePrice = salePrice,
        Items =
        {
            new RecipeItem
            {
                Ingredient = new Ingredient { Id = 1, Name = "Mum", PurchasePrice = waxPrice, PurchaseQuantity = 1000m },
                Quantity = 100m
            }
        }
    };

    private static IReadOnlyList<MarginDrop> Find(Recipe[] before, Recipe[] after) =>
        MarginAlerts.FindDrops(before, after, HourlyRate, Threshold);

    [Fact]
    public void PriceRise_PushesMarginBelowThreshold_IsReported()
    {
        // Maliyet 30 -> 90, satış 100: marj %70 -> %10.
        var drop = Assert.Single(Find(new[] { Make(300m) }, new[] { Make(900m) }));

        Assert.Equal(1, drop.RecipeId);
        Assert.Equal("A", drop.RecipeName);
        Assert.Equal(100m, drop.SalePrice);
        Assert.Equal(30m, drop.UnitCostBefore);
        Assert.Equal(90m, drop.UnitCostAfter);
        Assert.Equal(0.70m, drop.MarginBefore);
        Assert.Equal(0.10m, drop.MarginAfter);
    }

    [Fact]
    public void PriceRise_MarginStaysAboveThreshold_IsNotReported() =>
        // Maliyet 30 -> 60: marj %70 -> %40, eşik %30.
        Assert.Empty(Find(new[] { Make(300m) }, new[] { Make(600m) }));

    [Fact]
    public void PriceDrop_IsNotReported() =>
        Assert.Empty(Find(new[] { Make(900m) }, new[] { Make(300m) }));

    [Fact]
    public void NoSalePrice_IsNotReported() =>
        Assert.Empty(Find(new[] { Make(300m, salePrice: null) }, new[] { Make(900m, salePrice: null) }));

    [Fact]
    public void AlreadyBelowThreshold_AndGotWorse_IsReported()
    {
        // Maliyet 80 -> 90: marj %20 -> %10.
        var drop = Assert.Single(Find(new[] { Make(800m) }, new[] { Make(900m) }));
        Assert.Equal(0.20m, drop.MarginBefore);
        Assert.Equal(0.10m, drop.MarginAfter);
    }

    [Fact]
    public void AlreadyBelowThreshold_Unchanged_IsNotReported() =>
        Assert.Empty(Find(new[] { Make(900m) }, new[] { Make(900m) }));

    [Fact]
    public void AlreadyBelowThreshold_ButImproved_IsNotReported() =>
        Assert.Empty(Find(new[] { Make(900m) }, new[] { Make(800m) }));

    [Fact]
    public void MarginExactlyAtThreshold_IsNotReported() =>
        // Maliyet 70 -> marj %30 = eşik: "altında" değil.
        Assert.Empty(Find(new[] { Make(300m) }, new[] { Make(700m) }));

    [Fact]
    public void OnlyAffectedRecipes_AreReported()
    {
        var before = new[] { Make(300m, id: 1, name: "Etkilenen"), Make(300m, id: 2, name: "Diğer") };
        var after = new[] { Make(900m, id: 1, name: "Etkilenen"), Make(300m, id: 2, name: "Diğer") };

        var drop = Assert.Single(Find(before, after));

        Assert.Equal("Etkilenen", drop.RecipeName);
    }

    [Fact]
    public void RecipeMissingAfterChange_IsIgnored() =>
        Assert.Empty(Find(new[] { Make(300m, id: 1), Make(300m, id: 2) }, new[] { Make(300m, id: 2) }));

    [Fact]
    public void SalePriceChangedBetweenSnapshots_UsesEachSnapshotsOwnPrice()
    {
        // Önce 200'e satılıyordu (maliyet 30 -> marj %85), sonra 100 (maliyet 90 -> %10).
        var drop = Assert.Single(Find(new[] { Make(300m, salePrice: 200m) }, new[] { Make(900m, salePrice: 100m) }));

        Assert.Equal(0.85m, drop.MarginBefore);
        Assert.Equal(0.10m, drop.MarginAfter);
        Assert.Equal(100m, drop.SalePrice);
    }
}
