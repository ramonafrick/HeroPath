namespace HeroPath.Shared.Application.Contracts;

/// <summary>Keys used with <see cref="IAppStorage"/>. The hero's name is never stored, so it has no key.</summary>
public static class StorageKeys
{
    /// <summary>Selected UI culture name (<c>de-CH</c> or <c>en</c>).</summary>
    public const string Language = "heropath.language";
}
