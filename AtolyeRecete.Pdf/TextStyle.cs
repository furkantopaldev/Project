using SkiaSharp;

namespace AtolyeRecete.Pdf;

/// <summary>
/// Bir yazı boyutu ve kalınlığı için çizim ve ölçüm. Sistemin varsayılan yazı tipini kullanır
/// (Windows'ta Segoe/Arial, Android'de Roboto); bir harf (ş, ğ, ı, İ, ₺ ...) o yazı tipinde yoksa
/// sistemdeki uygun bir yazı tipine düşülür.
/// </summary>
internal sealed class TextStyle : IDisposable
{
    private readonly SKTypeface _primary;
    private readonly SKFont _primaryFont;
    private readonly float _size;
    private readonly bool _bold;
    private readonly List<SKTypeface> _fallbacks = new();

    public TextStyle(float size, bool bold)
    {
        _size = size;
        _bold = bold;
        // Aile adı null verilirse stil yok sayılabiliyor; kalın istemi için varsayılan ailenin adı verilir.
        _primary = SKTypeface.FromFamilyName(SKTypeface.Default.FamilyName, bold ? SKFontStyle.Bold : SKFontStyle.Normal)
                   ?? SKTypeface.Default;
        _primaryFont = new SKFont(_primary, size) { Subpixel = true };
    }

    /// <summary>Seçilen yazı tipi gerçekten kalın mı (ağırlık >= 600)?</summary>
    public bool IsBold => _primary.FontWeight >= 600;

    /// <summary>Yazı tipinin baseline'dan yukarı uzaklığı (pozitif).</summary>
    public float Ascent => -_primaryFont.Metrics.Ascent;

    /// <summary>Baseline'ın altındaki uzaklık (pozitif).</summary>
    public float Descent => _primaryFont.Metrics.Descent;

    /// <summary>Bir satırın toplam yüksekliği.</summary>
    public float LineHeight => _primaryFont.Spacing;

    /// <summary>Metindeki her harf için, sistemde onu çizebilen bir yazı tipi bulunuyor mu?</summary>
    public bool CanRender(string text)
    {
        for (var i = 0; i < text.Length;)
        {
            var codepoint = char.ConvertToUtf32(text, i);
            if (!HasGlyph(TypefaceFor(codepoint), codepoint)) return false;
            i += char.IsSurrogatePair(text, i) ? 2 : 1;
        }
        return true;
    }

    public float Measure(string text)
    {
        var width = 0f;
        foreach (var (run, typeface) in Split(text))
        {
            using var font = new SKFont(typeface, _size);
            width += font.MeasureText(run);
        }
        return width;
    }

    public void Draw(SKCanvas canvas, string text, float x, float baseline, SKColor color)
    {
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        foreach (var (run, typeface) in Split(text))
        {
            using var font = new SKFont(typeface, _size) { Subpixel = true };
            canvas.DrawText(run, x, baseline, SKTextAlign.Left, font, paint);
            x += font.MeasureText(run);
        }
    }

    public void DrawRight(SKCanvas canvas, string text, float right, float baseline, SKColor color) =>
        Draw(canvas, text, right - Measure(text), baseline, color);

    /// <summary>Metni, aynı yazı tipiyle çizilebilen ardışık parçalara böler.</summary>
    private IEnumerable<(string Run, SKTypeface Typeface)> Split(string text)
    {
        if (text.Length == 0) yield break;

        var start = 0;
        var current = TypefaceFor(char.ConvertToUtf32(text, 0));

        for (var i = 0; i < text.Length;)
        {
            var codepoint = char.ConvertToUtf32(text, i);
            var width = char.IsSurrogatePair(text, i) ? 2 : 1;
            var typeface = TypefaceFor(codepoint);

            if (!ReferenceEquals(typeface, current))
            {
                yield return (text[start..i], current);
                start = i;
                current = typeface;
            }
            i += width;
        }

        yield return (text[start..], current);
    }

    private static bool HasGlyph(SKTypeface typeface, int codepoint)
    {
        using var font = new SKFont(typeface);
        return font.ContainsGlyph(codepoint);
    }

    private SKTypeface TypefaceFor(int codepoint)
    {
        if (HasGlyph(_primary, codepoint)) return _primary;

        foreach (var fallback in _fallbacks)
            if (HasGlyph(fallback, codepoint)) return fallback;

        var match = SKFontManager.Default.MatchCharacter(
            null, _bold ? SKFontStyle.Bold : SKFontStyle.Normal, null, codepoint);
        if (match is null) return _primary;

        _fallbacks.Add(match);
        return match;
    }

    public void Dispose()
    {
        _primaryFont.Dispose();
        foreach (var fallback in _fallbacks) fallback.Dispose();
        _primary.Dispose();
    }
}
