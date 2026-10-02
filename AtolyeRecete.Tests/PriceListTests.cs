using AtolyeRecete.Core;
using AtolyeRecete.Core.Models;

namespace AtolyeRecete.Tests;

public class PriceListTests
{
    private static Recipe R(string name, decimal? salePrice) => new() { Name = name, SalePrice = salePrice };

    [Fact]
    public void OnlyRecipesWithSalePrice_AreListed()
    {
        var list = PriceListBuilder.Build(new[] { R("A", 100m), R("B", null), R("C", 250.5m) });

        Assert.Equal(new[] { "A", "C" }, list.Entries.Select(e => e.Name));
        Assert.Equal(new[] { 100m, 250.5m }, list.Entries.Select(e => e.Price));
        Assert.Equal(1, list.SkippedCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NonPositivePrice_IsTreatedAsMissing(int price)
    {
        var list = PriceListBuilder.Build(new[] { R("A", price) });

        Assert.Empty(list.Entries);
        Assert.Equal(1, list.SkippedCount);
    }

    [Fact]
    public void Entries_AreSortedByTurkishAlphabet()
    {
        // Türkçe sıra: A, Ç, I, İ, Z gibi; sıralama "ç"yi sona atmamalı.
        var list = PriceListBuilder.Build(new[] { R("Zeytin", 1m), R("çam", 1m), R("Ardıç", 1m), R("Bergamot", 1m) });

        Assert.Equal(new[] { "Ardıç", "Bergamot", "çam", "Zeytin" }, list.Entries.Select(e => e.Name));
    }

    [Fact]
    public void Entries_SortIgnoresCase()
    {
        var list = PriceListBuilder.Build(new[] { R("bambu", 1m), R("Amber", 1m), R("cam", 1m) });

        Assert.Equal(new[] { "Amber", "bambu", "cam" }, list.Entries.Select(e => e.Name));
    }

    [Fact]
    public void NoRecipes_GivesEmptyList()
    {
        var list = PriceListBuilder.Build(Array.Empty<Recipe>());

        Assert.Empty(list.Entries);
        Assert.Equal(0, list.SkippedCount);
    }

    [Fact]
    public void NamesAreTrimmed()
    {
        var list = PriceListBuilder.Build(new[] { R("  Lavanta mumu  ", 100m) });

        Assert.Equal("Lavanta mumu", Assert.Single(list.Entries).Name);
    }
}
