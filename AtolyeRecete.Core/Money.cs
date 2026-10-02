namespace AtolyeRecete.Core;

public static class Money
{
    /// <summary>Gösterim/sonuç için 2 basamak, yarım değerler yukarı.</summary>
    public static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
