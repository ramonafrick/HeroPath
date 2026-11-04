using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace HeroPath.Modules.Game.UI.Components;

/// <summary>Decorative confetti over the board after a success. Pure CSS (about 2 seconds), a fixed set of pieces, no timer and no script. Under reduced motion it is a calm golden frame instead.</summary>
public partial class SuccessConfetti
{
    private const int PieceCount = 28;

    private static readonly string[] _colors = ["--hp-goal", "--hp-strawberry", "--hp-pond", "--hp-start", "--hp-lisa-dress", "--hp-max-pants"];

    private static readonly string[] _pieceStyles = [.. Enumerable.Range(0, PieceCount).Select(StyleOf)];

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Attributes { get; set; }

    private static string StyleOf(int index) => string.Create(
        CultureInfo.InvariantCulture,
        $"--x: {(index * 37 + 5) % 96 + 2}%; --delay: {index * 53 % 400}ms; --time: {1500 + index * 71 % 300}ms; --turn: {(index % 2 == 0 ? 1 : -1) * (360 + index * 47 % 360)}deg; --drift: {(index % 5 - 2) * 2}rem; --color: var({_colors[index % _colors.Length]});");
}
