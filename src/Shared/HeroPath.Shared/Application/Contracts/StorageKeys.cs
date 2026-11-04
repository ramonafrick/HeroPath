namespace HeroPath.Shared.Application.Contracts;

/// <summary>Keys used with <see cref="IAppStorage"/>. The hero's name is never stored, so it has no key.</summary>
public static class StorageKeys
{
    /// <summary>Selected UI culture name (<c>de-CH</c> or <c>en</c>).</summary>
    public const string Language = "heropath.language";

    /// <summary>JSON array of the child's custom levels.</summary>
    public const string CustomLevels = "heropath.levels";

    /// <summary>Sound on or off (<c>on</c> or <c>off</c>). Nothing else about the child is stored.</summary>
    public const string Sound = "heropath.sound";
}
