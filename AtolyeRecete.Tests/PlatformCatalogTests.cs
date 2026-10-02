using AtolyeRecete.Core;

namespace AtolyeRecete.Tests;

public class PlatformCatalogTests
{
    private static string Json(string platformsArray) =>
        $$"""{ "version": 1, "platforms": {{platformsArray}} }""";

    [Fact]
    public void Parse_ReadsAllFields()
    {
        var json = Json("""
            [ { "name": "Trendyol", "commissionRate": 0.20, "fixedFee": 5.5, "notes": "örnek" } ]
            """);

        var platform = Assert.Single(PlatformCatalog.Parse(json));

        Assert.Equal("Trendyol", platform.Name);
        Assert.Equal(0.20m, platform.CommissionRate);
        Assert.Equal(5.5m, platform.FixedFee);
        Assert.Equal("örnek", platform.Notes);
    }

    [Fact]
    public void Parse_KeepsOrder_AndDecimalPrecision()
    {
        var json = Json("""
            [ { "name": "A", "commissionRate": 0.035, "fixedFee": 0 },
              { "name": "B", "commissionRate": 0.095, "fixedFee": 0 } ]
            """);

        var list = PlatformCatalog.Parse(json);

        Assert.Equal(new[] { "A", "B" }, list.Select(p => p.Name));
        Assert.Equal(0.035m, list[0].CommissionRate);
        Assert.Equal(0.095m, list[1].CommissionRate);
    }

    [Fact]
    public void Parse_NotesAreOptional()
    {
        var json = Json("""[ { "name": "A", "commissionRate": 0.1, "fixedFee": 0 } ]""");
        Assert.Null(Assert.Single(PlatformCatalog.Parse(json)).Notes);
    }

    [Fact]
    public void Parse_ZeroCommissionIsValid()
    {
        var json = Json("""[ { "name": "Elden", "commissionRate": 0, "fixedFee": 0 } ]""");
        Assert.Equal(0m, Assert.Single(PlatformCatalog.Parse(json)).CommissionRate);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{ "platforms": [] }""")]
    [InlineData("""{ "platforms": [ { "commissionRate": 0.1, "fixedFee": 0 } ] }""")]              // ad yok
    [InlineData("""{ "platforms": [ { "name": " ", "commissionRate": 0.1, "fixedFee": 0 } ] }""")] // boş ad
    [InlineData("""{ "platforms": [ { "name": "A", "fixedFee": 0 } ] }""")]                        // oran yok
    [InlineData("""{ "platforms": [ { "name": "A", "commissionRate": 0.1 } ] }""")]                 // ücret yok
    [InlineData("""{ "platforms": [ { "name": "A", "commissionRate": 1, "fixedFee": 0 } ] }""")]    // oran >= 1
    [InlineData("""{ "platforms": [ { "name": "A", "commissionRate": -0.1, "fixedFee": 0 } ] }""")] // oran < 0
    [InlineData("""{ "platforms": [ { "name": "A", "commissionRate": 0.1, "fixedFee": -1 } ] }""")] // ücret < 0
    [InlineData("""{ "platforms": [ { "name": "A", "commissionRate": 0.1, "fixedFee": 0 }, { "name": "a", "commissionRate": 0.2, "fixedFee": 0 } ] }""")] // yinelenen ad
    public void Parse_InvalidInput_ThrowsFormatException(string json) =>
        Assert.Throws<FormatException>(() => PlatformCatalog.Parse(json));

    [Fact]
    public void ShippedPlatformsJson_IsValid()
    {
        var path = FindRepoFile("AtolyeRecete.App", "Resources", "Raw", "platforms.json");

        var list = PlatformCatalog.Parse(File.ReadAllText(path));

        Assert.NotEmpty(list);
        Assert.All(list, p => Assert.InRange(p.CommissionRate, 0m, 0.99m));
    }

    /// <summary>Çözüm kökünü (AtolyeRecete.sln) yukarı doğru arayıp dosyayı bulur.</summary>
    private static string FindRepoFile(params string[] relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AtolyeRecete.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(new[] { dir!.FullName }.Concat(relative).ToArray());
    }
}
