using HeroPath.Modules.Game.Application;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>Adds the <see cref="GameSession"/> to <see cref="LocalizedComponentBase"/>: re-renders on <see cref="GameSession.Changed"/>.</summary>
public abstract class SessionComponentBase : LocalizedComponentBase
{
    [Inject]
    protected GameSession Session { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Session.Changed += HandleChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Session.Changed -= HandleChanged;
        }

        base.Dispose(disposing);
    }
}
