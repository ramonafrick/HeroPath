namespace HeroPath.Modules.Game.Domain;

/// <summary>Execution limits from docs/Projekt_Kontext.md, section 4.</summary>
public static class ProgramLimits
{
    public const int MaxExecutedSteps = 1000;
    public const int MinRepeatCount = 1;
    public const int MaxRepeatCount = 20;
    public const int MaxRepeatNestingDepth = 2;
}
