using System.Security.Cryptography;

namespace HeroPath.Modules.Levels.Application;

/// <summary>Creates ids for custom levels. It lives in Application, not Domain, because it is random.</summary>
public static class LevelIdGenerator
{
    private const string Prefix = "custom-";

    /// <summary><c>custom-</c> plus 12 lowercase hex characters; draws again while <paramref name="exists"/> says the id is taken.</summary>
    public static string NewId(Func<string, bool> exists)
    {
        ArgumentNullException.ThrowIfNull(exists);
        string id;
        do
        {
            id = Prefix + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        }
        while (exists(id));

        return id;
    }
}
