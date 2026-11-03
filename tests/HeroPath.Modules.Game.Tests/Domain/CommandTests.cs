using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.Tests.Domain;

public class CommandTests
{
    private static MoveCommand Move() => new(Direction.Right);

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void RepeatCommand_CountInRange_IsAccepted(int count)
    {
        var repeat = new RepeatCommand(count, [Move()]);

        Assert.Equal(count, repeat.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public void RepeatCommand_CountOutOfRange_Throws(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RepeatCommand(count, [Move()]));
    }

    [Fact]
    public void RepeatCommand_NestedDepthTwo_IsAccepted()
    {
        var outer = new RepeatCommand(2, [new RepeatCommand(3, [Move()])]);

        Assert.IsType<RepeatCommand>(outer.Body[0]);
    }

    [Fact]
    public void RepeatCommand_NestedDepthThree_Throws()
    {
        var inner = new RepeatCommand(2, [new RepeatCommand(2, [Move()])]);

        Assert.Throws<ArgumentOutOfRangeException>(() => new RepeatCommand(2, [inner]));
    }

    [Fact]
    public void RepeatCommand_EmptyBody_IsAllowed()
    {
        Assert.Empty(new RepeatCommand(2, []).Body);
    }

    [Fact]
    public void RepeatCommand_NullBody_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RepeatCommand(2, null!));
    }

    [Fact]
    public void RepeatCommand_NullElement_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RepeatCommand(2, [null!]));
    }

    [Fact]
    public void RepeatCommand_Body_IsDefensiveCopy()
    {
        var source = new List<Command> { Move() };
        var repeat = new RepeatCommand(2, source);

        source.Add(Move());

        Assert.Single(repeat.Body);
    }

    [Fact]
    public void Ids_AreUniqueAndIdentityBased()
    {
        var a = new MoveCommand(Direction.Up);
        var b = new MoveCommand(Direction.Up);
        var r1 = new RepeatCommand(2, []);
        var r2 = new RepeatCommand(2, []);

        Assert.NotEqual(a.Id, b.Id);
        Assert.NotEqual(r1.Id, r2.Id);
        Assert.NotEqual(a, b);
        Assert.NotEqual(r1, r2);
    }

    [Fact]
    public void ExplicitId_IsPreserved()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, new MoveCommand(Direction.Left, id).Id);
        Assert.Equal(id, new RepeatCommand(2, [], id).Id);
    }

    [Fact]
    public void ProgramDefinition_KeepsOrder()
    {
        var first = new MoveCommand(Direction.Up);
        var second = new MoveCommand(Direction.Down);

        var program = new ProgramDefinition([first, second]);

        Assert.Equal([first, second], program.Commands);
    }

    [Fact]
    public void ProgramDefinition_IsDefensiveCopy()
    {
        var source = new List<Command> { Move() };
        var program = new ProgramDefinition(source);

        source.Clear();

        Assert.Single(program.Commands);
    }

    [Fact]
    public void ProgramDefinition_EmptyIsAllowed()
    {
        Assert.Empty(new ProgramDefinition([]).Commands);
        Assert.Empty(ProgramDefinition.Empty.Commands);
    }

    [Fact]
    public void ProgramDefinition_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new ProgramDefinition(null!));
        Assert.Throws<ArgumentNullException>(() => new ProgramDefinition([null!]));
    }

    [Fact]
    public void ProgramDefinition_ExactlyMaxCommands_IsAccepted()
    {
        var program = new ProgramDefinition(Moves(ProgramLimits.MaxProgramCommands));

        Assert.Equal(ProgramLimits.MaxProgramCommands, program.Commands.Count);
    }

    [Fact]
    public void ProgramDefinition_OneMoreThanMaxCommands_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgramDefinition(Moves(ProgramLimits.MaxProgramCommands + 1)));
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void ProgramDefinition_CountsNestedCommandsRecursively(int topLevelMoves, bool accepted)
    {
        var inner = new RepeatCommand(2, Moves(98));
        var outer = new RepeatCommand(2, [inner, .. Moves(98)]);
        Command[] commands = [outer, .. Moves(topLevelMoves)];

        if (accepted)
        {
            Assert.Equal(topLevelMoves + 1, new ProgramDefinition(commands).Commands.Count);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProgramDefinition(commands));
        }
    }

    private static Command[] Moves(int count) => [.. Enumerable.Range(0, count).Select(_ => (Command)Move())];
}
