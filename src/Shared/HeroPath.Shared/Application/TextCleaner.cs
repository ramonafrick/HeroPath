using System.Globalization;
using System.Text;

namespace HeroPath.Shared.Application;

/// <summary>Cleans free text typed or pasted by the child (hero name, level name).</summary>
public static class TextCleaner
{
    /// <summary>
    /// Removes control characters, invisible format characters (zero-width, bidi marks and overrides) and line and
    /// paragraph separators, trims, cuts to <paramref name="maxTextElements"/> text elements (never inside a surrogate
    /// pair or a combining sequence) and trims again. Returns an empty string when nothing is left.
    /// </summary>
    public static string Clean(string? raw, int maxTextElements)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxTextElements);
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        var cleaned = string.Concat(raw.Where(character => !IsHidden(character))).Trim();
        var builder = new StringBuilder();
        var enumerator = StringInfo.GetTextElementEnumerator(cleaned);
        for (var count = 0; count < maxTextElements && enumerator.MoveNext(); count++)
        {
            builder.Append(enumerator.GetTextElement());
        }

        return builder.ToString().Trim();
    }

    /// <summary>Whether a character is invisible or a line break: control, format (zero-width, bidi marks and overrides), line or paragraph separator.</summary>
    public static bool IsHidden(char character) =>
        char.IsControl(character)
        || CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;
}
