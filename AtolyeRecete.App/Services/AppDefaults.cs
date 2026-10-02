using System.Globalization;

namespace AtolyeRecete.App.Services;

/// <summary>Uygulama genelinde sabit değerler (ayarlanabilir olanlar <see cref="AppSettings"/> içinde).</summary>
public static class AppDefaults
{
    public static readonly CultureInfo Culture = new("tr-TR");
}
