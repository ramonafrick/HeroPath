namespace HeroPath.Shared.Application.Contracts;

/// <summary>A key (the <c>KeyboardEvent.key</c> value, for example <c>ArrowUp</c> or a space) together with whether Shift is held.</summary>
public readonly record struct KeyChord(string Key, bool Shift);
