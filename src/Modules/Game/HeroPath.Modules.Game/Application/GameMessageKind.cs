namespace HeroPath.Modules.Game.Application;

/// <summary>How a <see cref="GameMessage"/> is presented: something went wrong, the child won, or a hint explains a blocked action.</summary>
public enum GameMessageKind
{
    Error,
    Success,
    Hint,
}
