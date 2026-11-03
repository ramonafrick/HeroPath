namespace HeroPath.Modules.Game.Application;

/// <summary>Where a program run currently stands.</summary>
public enum RunStatus
{
    /// <summary>No step executed yet; the hero stands on the start cell and the program can be edited.</summary>
    Ready,

    /// <summary>Play is executing steps.</summary>
    Running,

    /// <summary>Debug executed a step and waits for the next click.</summary>
    Paused,

    /// <summary>The run ended with an error.</summary>
    Failed,

    /// <summary>The run reached the goal.</summary>
    Succeeded,
}
