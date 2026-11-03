using System.Globalization;
using System.Text;
using HeroPath.Modules.Levels.Infrastructure;

namespace HeroPath.Modules.Game.Application;

/// <summary>
/// The hero and level the child picked on the start page. Memory only: the name is never stored, logged or part of any
/// text produced by this type (no <c>ToString</c> override that includes it).
/// </summary>
public sealed class HeroChoice
{
    /// <summary>Maximum length of the hero name in characters (text elements).</summary>
    public const int MaxNameLength = 20;

    private HeroAvatar _avatar = HeroAvatar.Max;
    private string? _name;
    private string? _levelId;

    /// <summary>Raised after <see cref="Choose"/> changed something.</summary>
    public event Action? Changed;

    /// <summary>The chosen hero. <see cref="HeroAvatar.Max"/> until something is chosen.</summary>
    public HeroAvatar Avatar => _avatar;

    /// <summary>The normalized hero name; the default name of the avatar until something is chosen.</summary>
    public string Name => _name ?? DefaultName(_avatar);

    /// <summary>Whether the child confirmed a choice on the start page.</summary>
    public bool IsChosen { get; private set; }

    /// <summary>Id of the chosen level; the first standard level until something is chosen.</summary>
    public string LevelId => _levelId ?? StandardLevels.All[0].Id;

    /// <summary>Confirms the choice. The raw name is normalized (see <see cref="NormalizeName"/>).</summary>
    public void Choose(HeroAvatar avatar, string? rawName, string levelId)
    {
        ArgumentNullException.ThrowIfNull(levelId);
        var name = NormalizeName(rawName, avatar);
        var changed = !IsChosen || avatar != _avatar || name != _name || levelId != _levelId;

        _avatar = avatar;
        _name = name;
        _levelId = levelId;
        IsChosen = true;

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>Switches the level while keeping hero and name. Does nothing before a choice was confirmed or for the same level.</summary>
    public void SelectLevel(string levelId)
    {
        ArgumentNullException.ThrowIfNull(levelId);
        if (!IsChosen || levelId == _levelId)
        {
            return;
        }

        _levelId = levelId;
        Changed?.Invoke();
    }

    /// <summary>The name a hero has when the child types nothing.</summary>
    public static string DefaultName(HeroAvatar avatar) => avatar switch
    {
        HeroAvatar.Max => "Max",
        HeroAvatar.Lisa => "Lisa",
        _ => throw new ArgumentOutOfRangeException(nameof(avatar)),
    };

    /// <summary>
    /// Removes control characters, trims, cuts to <see cref="MaxNameLength"/> text elements (never inside a surrogate pair or a
    /// combining sequence) and falls back to the default name of <paramref name="avatar"/> when nothing is left.
    /// </summary>
    public static string NormalizeName(string? raw, HeroAvatar avatar)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return DefaultName(avatar);
        }

        var builder = new StringBuilder(raw.Length);
        foreach (var character in raw)
        {
            if (!char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        var cleaned = builder.ToString().Trim();
        var cut = new StringBuilder();
        var enumerator = StringInfo.GetTextElementEnumerator(cleaned);
        for (var count = 0; count < MaxNameLength && enumerator.MoveNext(); count++)
        {
            cut.Append(enumerator.GetTextElement());
        }

        var result = cut.ToString().Trim();
        return result.Length == 0 ? DefaultName(avatar) : result;
    }

    /// <summary>
    /// The name field after the child switched hero: an empty field or the previous hero's default name becomes the new
    /// default name, anything else the child typed is kept.
    /// </summary>
    public static string NameAfterSwitch(string? currentInput, HeroAvatar previous, HeroAvatar next)
    {
        if (string.IsNullOrWhiteSpace(currentInput) || currentInput.Trim() == DefaultName(previous))
        {
            return DefaultName(next);
        }

        return currentInput;
    }
}
