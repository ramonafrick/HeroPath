namespace HeroPath.Modules.Levels.Domain;

/// <summary>Grid and metadata limits from docs/Projekt_Kontext.md, section 4.</summary>
public static class LevelLimits
{
    public const int MinGridSize = 3;
    public const int MaxGridSize = 20;
    public const int DefaultGridSize = 10;
    public const int MaxIdLength = 40;
    public const int MaxNameLength = 50;

    /// <summary>Most strawberries a level may hold (keeps the solver state space small).</summary>
    public const int MaxStrawberries = 10;

    /// <summary>Most pizzas a level may hold.</summary>
    public const int MaxPizzas = 10;

    /// <summary>Most custom levels a child may keep.</summary>
    public const int MaxCustomLevels = 20;

    /// <summary>Moves the interpreter executes before a run fails; mirrors <c>ProgramLimits.MaxExecutedSteps</c> in the Game module (a test keeps them equal).</summary>
    public const int MaxSteps = 1000;
}
