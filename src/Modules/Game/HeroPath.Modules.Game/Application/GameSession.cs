using HeroPath.Modules.Game.Domain;
using HeroPath.Modules.Levels.Domain;

namespace HeroPath.Modules.Game.Application;

/// <summary>State of the current play page: loaded grid and the program the child assembles. Memory only.</summary>
public sealed class GameSession
{
    private readonly List<MoveCommand> _commands = [];

    /// <summary>Raised after every change of the session.</summary>
    public event Action? Changed;

    /// <summary>The loaded level grid, or <c>null</c> before <see cref="Load"/> was called.</summary>
    public Grid? Grid { get; private set; }

    /// <summary>Blocks of the program in execution order.</summary>
    public IReadOnlyList<MoveCommand> Commands => _commands;

    /// <summary>Whether the program may be changed. Driven by the execution runner.</summary>
    public bool IsEditable { get; private set; } = true;

    /// <summary>Whether another block fits into the program.</summary>
    public bool CanAdd => _commands.Count < ProgramLimits.MaxProgramCommands;

    /// <summary>Position of the hero on the board: the start cell of the loaded grid.</summary>
    public Position HeroPosition => Grid?.Start ?? default;

    /// <summary>Loads a level grid and keeps the current program.</summary>
    public void Load(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        Grid = grid;
        Changed?.Invoke();
    }

    /// <summary>Appends a move block. Does nothing when the program is full or locked.</summary>
    public void Add(Direction direction)
    {
        if (!IsEditable || !CanAdd)
        {
            return;
        }

        _commands.Add(new MoveCommand(direction));
        Changed?.Invoke();
    }

    /// <summary>Removes the block with <paramref name="id"/>. Does nothing when it is unknown or the program is locked.</summary>
    public void Remove(Guid id)
    {
        if (!IsEditable)
        {
            return;
        }

        var index = _commands.FindIndex(command => command.Id == id);
        if (index < 0)
        {
            return;
        }

        _commands.RemoveAt(index);
        Changed?.Invoke();
    }

    /// <summary>Removes all blocks. Does nothing when the program is empty or locked.</summary>
    public void Clear()
    {
        if (!IsEditable || _commands.Count == 0)
        {
            return;
        }

        _commands.Clear();
        Changed?.Invoke();
    }

    /// <summary>Locks or unlocks editing. Called by the execution runner.</summary>
    public void SetEditable(bool isEditable)
    {
        if (IsEditable == isEditable)
        {
            return;
        }

        IsEditable = isEditable;
        Changed?.Invoke();
    }

    /// <summary>Builds the program the interpreter runs.</summary>
    public ProgramDefinition ToProgram() => new([.. _commands]);
}
