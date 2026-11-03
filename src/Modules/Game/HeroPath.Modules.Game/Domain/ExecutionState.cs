namespace HeroPath.Modules.Game.Domain;

/// <summary>State of a run. Succeeded and Failed are terminal.</summary>
public enum ExecutionState
{
    Ready,
    Running,
    Succeeded,
    Failed,
}
