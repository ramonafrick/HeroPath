namespace HeroPath.Modules.Game.UI;

internal static class EnumExtensions
{
    /// <summary>Lowercase name of an enum value for CSS classes and <c>data-*</c> attributes (<c>Up</c> becomes <c>up</c>).</summary>
    public static string ToSlug<TEnum>(this TEnum value)
        where TEnum : struct, Enum => value.ToString().ToLowerInvariant();
}
