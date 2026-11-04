using HeroPath.Modules.Game.Application;
using HeroPath.Modules.Game.UI.Components;
using HeroPath.Modules.Game.UI.Resources;
using HeroPath.Modules.Game.UI.Services;
using HeroPath.Modules.Levels.Application;
using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Game.UI.Pages;

/// <summary>
/// The play page. Opening it loads the chosen level into the <see cref="GameSession"/> (the same level keeps program and run) or
/// redirects to the start page when nothing was chosen;
/// leaving it stops a running Play and ends a drag in progress, so no loop keeps running and the run resumes consistently (Paused) on return.
/// </summary>
public partial class PlayPage : SessionComponentBase
{
    private readonly CancellationTokenSource _shortcutsCancellation = new();
    private IAsyncDisposable? _subscription;
    private bool _shortcutsActive;

    [Inject]
    private HeroChoice Choice { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ExecutionRunner Runner { get; set; } = default!;

    [Inject]
    private ProgramDragState Drag { get; set; } = default!;

    // Injecting creates the scoped RunSounds, which subscribes to the session once for its lifetime.
    [Inject]
    protected RunSounds Sounds { get; set; } = default!;

    [Inject]
    private IKeyboardShortcuts Shortcuts { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResources> SharedLocalizer { get; set; } = default!;

    [Inject]
    private IStringLocalizer<LevelsResources> LevelsLocalizer { get; set; } = default!;

    [Inject]
    private LevelProgress Progress { get; set; } = default!;

    [Inject]
    private LevelRepository Repository { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GameResources> GameLocalizer { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (Choice.IsChosen && Choice.LevelId is { } id && Progress.Find(id) is { } chosen)
        {
            Session.Load(chosen);
        }
        else
        {
            Navigation.NavigateTo(string.Empty, replace: true);
        }

        base.OnInitialized();
        Progress.Changed += HandleChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !Choice.IsChosen || Session.Grid is null)
        {
            return;
        }

        try
        {
            var subscription = await Shortcuts.SubscribeAsync(HandleKeyAsync, _shortcutsCancellation.Token);
            if (_shortcutsCancellation.IsCancellationRequested)
            {
                await subscription.DisposeAsync();
                return;
            }

            _subscription = subscription;
            _shortcutsActive = true;
            StateHasChanged();
        }
        catch (OperationCanceledException) when (_shortcutsCancellation.IsCancellationRequested)
        {
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Progress.Changed -= HandleChanged;
        }

        base.Dispose(disposing);
        if (disposing)
        {
            _shortcutsCancellation.Cancel();
            _shortcutsCancellation.Dispose();
            if (_subscription is { } subscription)
            {
                _subscription = null;
                _ = subscription.DisposeAsync().AsTask();
            }

            Runner.Stop();
            Drag.End();
        }
    }

    private Task HandleKeyAsync(string key, bool shift) => InvokeAsync(async () =>
    {
        if (ShortcutKeys.Resolve(key, shift) is not { } action || _shortcutsCancellation.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await RunAsync(action);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    });

    private async Task RunAsync(ShortcutAction action)
    {
        if (ShortcutKeys.InsertDirection(action) is { } direction)
        {
            Session.Add(direction);
            return;
        }

        switch (action)
        {
            case ShortcutAction.MoveSelectedUp when Session.SelectedId is { } id:
                Session.MoveUp(id);
                break;
            case ShortcutAction.MoveSelectedDown when Session.SelectedId is { } id:
                Session.MoveDown(id);
                break;
            case ShortcutAction.DeleteSelected when Session.SelectedId is { } id:
                Session.Remove(id);
                break;
            case ShortcutAction.ClearSelection:
                Session.Select(null);
                break;
            case ShortcutAction.Play:
                await Runner.PlayAsync();
                break;
            case ShortcutAction.Debug:
                Runner.StepOnce();
                break;
            case ShortcutAction.Reset:
                Runner.Reset();
                break;
        }
    }

    private void Edit(string id) => Navigation.NavigateTo($"editor/{id}");

    private void SwitchLevel(string id)
    {
        if (Progress.Find(id) is { } level)
        {
            LoadLevel(level);
        }
    }

    private void LoadLevel(LevelDefinition level)
    {
        Choice.SelectLevel(level.Id);
        Session.Load(level);
    }
}
