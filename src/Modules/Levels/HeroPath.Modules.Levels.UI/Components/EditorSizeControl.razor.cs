using Microsoft.AspNetCore.Components;

namespace HeroPath.Modules.Levels.UI.Components;

/// <summary>A number between 3 and 20 with minus and plus buttons (width or height of the board). The texts come from the parent.</summary>
public partial class EditorSizeControl
{
    /// <summary>Short id used in the test ids and element ids: <c>width</c> or <c>height</c>.</summary>
    [Parameter, EditorRequired]
    public required string Name { get; set; }

    [Parameter, EditorRequired]
    public required string Label { get; set; }

    [Parameter, EditorRequired]
    public required string DecreaseLabel { get; set; }

    [Parameter, EditorRequired]
    public required string IncreaseLabel { get; set; }

    [Parameter]
    public int Value { get; set; }

    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    private string LabelId => $"editor-{Name}-label";
}
