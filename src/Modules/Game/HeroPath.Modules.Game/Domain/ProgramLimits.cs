namespace HeroPath.Modules.Game.Domain;

/// <summary>Program and execution limits from docs/Projekt_Kontext.md, section 4.</summary>
public static class ProgramLimits
{
    /// <summary>Maximum number of moves executed per run.</summary>
    public const int MaxExecutedSteps = 1000;

    /// <summary>Smallest allowed repeat count.</summary>
    public const int MinRepeatCount = 1;

    /// <summary>Largest allowed repeat count.</summary>
    public const int MaxRepeatCount = 20;

    /// <summary>Maximum nesting depth of repeat blocks (a repeat inside a repeat is depth 2).</summary>
    public const int MaxRepeatNestingDepth = 2;

    /// <summary>Maximum number of blocks in a program, counting moves and repeats recursively.</summary>
    public const int MaxProgramCommands = 200;
}
