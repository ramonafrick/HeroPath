using HeroPath.Modules.Levels.Domain;
using HeroPath.Modules.Levels.UI.Resources;
using HeroPath.Shared.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>A native dropdown to switch the level. Solved levels get a check mark in front of the name.</summary>
public partial class LevelPicker : LocalizedComponentBase
{
    private static int _nextId;
    private readonly string _selectId = $"level-picker-{Interlocked.Increment(ref _nextId)}";

    [Parameter, EditorRequired]
    public required IReadOnlyList<LevelDefinition> Levels { get; set; }

    [Parameter]
    public string? SelectedId { get; set; }

    [Parameter]
    public Func<string, bool>? IsSolved { get; set; }

    [Parameter]
    public EventCallback<string> SelectedIdChanged { get; set; }

    [Inject]
    private IStringLocalizer<LevelsResources> Localizer { get; set; } = default!;

    private Task HandleChange(ChangeEventArgs args) =>
        args.Value is string id && id != SelectedId ? SelectedIdChanged.InvokeAsync(id) : Task.CompletedTask;
}
