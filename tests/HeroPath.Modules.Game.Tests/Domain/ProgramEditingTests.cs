using HeroPath.Modules.Game.Domain;

namespace HeroPath.Modules.Game.Tests.Domain;

public class ProgramEditingTests
{
    private static MoveCommand Move(Direction direction = Direction.Up) => new(direction);

    private static ProgramDefinition Program(params Command[] commands) => new(commands);

    private static RepeatCommand Repeat(int count, params Command[] body) => new(count, body);

    /// <summary>Compact text form: moves as u/d/l/r, repeats as N(...).</summary>
    private static string Show(ProgramDefinition program) => Show(program.Commands);

    private static string Show(IEnumerable<Command> commands) => string.Concat(commands.Select(c => c switch
    {
        MoveCommand move => move.Direction.ToString()[..1].ToLowerInvariant(),
        RepeatCommand repeat => $"{repeat.Count}({Show(repeat.Body)})",
        _ => "?",
    }));

    [Fact]
    public void Insert_AtEnd_AppendsBlock()
    {
        var first = Move(Direction.Up);
        var program = Program(first);

        var result = ProgramEditing.Insert(program, Move(Direction.Left), BlockSlot.Root(1));

        Assert.Equal("ul", Show(result!));
        Assert.Equal("u", Show(program));
    }

    [Theory]
    [InlineData(0, "lud")]
    [InlineData(1, "uld")]
    [InlineData(2, "udl")]
    public void Insert_AtIndex_PutsBlockThere(int index, string expected)
    {
        var program = Program(Move(Direction.Up), Move(Direction.Down));

        var result = ProgramEditing.Insert(program, Move(Direction.Left), BlockSlot.Root(index));

        Assert.Equal(expected, Show(result!));
    }

    [Fact]
    public void Insert_IntoEmptyRepeatBody_Works()
    {
        var repeat = Repeat(3);

        var result = ProgramEditing.Insert(Program(repeat), Move(Direction.Right), new BlockSlot(repeat.Id, 0));

        Assert.Equal("3(r)", Show(result!));
    }

    [Fact]
    public void Insert_IntoNestedBody_KeepsIdsAndCounts()
    {
        var inner = Repeat(4);
        var outer = Repeat(2, Move(Direction.Up), inner);
        var program = Program(outer);

        var result = ProgramEditing.Insert(program, Move(Direction.Down), new BlockSlot(inner.Id, 0))!;

        Assert.Equal("2(u4(d))", Show(result));
        var newOuter = Assert.IsType<RepeatCommand>(result.Commands[0]);
        Assert.Equal(outer.Id, newOuter.Id);
        Assert.Equal(inner.Id, newOuter.Body[1].Id);
        Assert.Equal(2, newOuter.Count);
    }

    [Fact]
    public void Insert_RepeatIntoRepeat_IsAllowedAtDepthTwo()
    {
        var outer = Repeat(2);

        var result = ProgramEditing.Insert(Program(outer), Repeat(3), new BlockSlot(outer.Id, 0));

        Assert.Equal("2(3())", Show(result!));
    }

    [Fact]
    public void Insert_RepeatIntoDepthTwo_IsRejected()
    {
        var inner = Repeat(3);
        var program = Program(Repeat(2, inner));
        var slot = new BlockSlot(inner.Id, 0);

        Assert.False(ProgramEditing.CanInsert(program, Repeat(1), slot));
        Assert.Null(ProgramEditing.Insert(program, Repeat(1), slot));
    }

    [Fact]
    public void Insert_MoveIntoDepthTwo_IsAllowed()
    {
        var inner = Repeat(3);
        var program = Program(Repeat(2, inner));

        Assert.True(ProgramEditing.CanInsert(program, Move(), new BlockSlot(inner.Id, 0)));
    }

    [Fact]
    public void Insert_RepeatWithBodyIntoRepeat_IsRejectedWhenTooDeep()
    {
        var outer = Repeat(2);
        var deep = Repeat(3, Repeat(1));

        Assert.False(ProgramEditing.CanInsert(Program(outer), deep, new BlockSlot(outer.Id, 0)));
        Assert.True(ProgramEditing.CanInsert(Program(outer), deep, BlockSlot.Root(0)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Insert_IndexOutOfRange_ReturnsNull(int index)
    {
        var program = Program(Move());

        Assert.Null(ProgramEditing.Insert(program, Move(), BlockSlot.Root(index)));
    }

    [Fact]
    public void Insert_UnknownParentOrParentThatIsAMove_ReturnsNull()
    {
        var move = Move();
        var program = Program(move);

        Assert.Null(ProgramEditing.Insert(program, Move(), new BlockSlot(Guid.NewGuid(), 0)));
        Assert.Null(ProgramEditing.Insert(program, Move(), new BlockSlot(move.Id, 0)));
    }

    [Fact]
    public void Insert_BlockAlreadyInProgram_ReturnsNull()
    {
        var move = Move();

        Assert.Null(ProgramEditing.Insert(Program(move), move, BlockSlot.Root(0)));
    }

    [Fact]
    public void Insert_AtTwoHundredBlocks_ReturnsNull()
    {
        var program = new ProgramDefinition([.. Enumerable.Range(0, ProgramLimits.MaxProgramCommands).Select(_ => (Command)Move())]);

        Assert.False(ProgramEditing.CanInsert(program, Move(), BlockSlot.Root(0)));
        Assert.Null(ProgramEditing.Insert(program, Move(), BlockSlot.Root(0)));
    }

    [Fact]
    public void Insert_CountsNestedBlocksTowardsLimit()
    {
        var body = Enumerable.Range(0, ProgramLimits.MaxProgramCommands - 2).Select(_ => (Command)Move()).ToArray();
        var program = Program(Repeat(2, body));
        Assert.Equal(ProgramLimits.MaxProgramCommands - 1, program.CountAll());

        Assert.NotNull(ProgramEditing.Insert(program, Move(), BlockSlot.Root(1)));
        Assert.Null(ProgramEditing.Insert(program, Repeat(1, Move()), BlockSlot.Root(1)));
    }

    [Fact]
    public void Remove_TopLevelBlock_RemovesOnlyIt()
    {
        var middle = Move(Direction.Left);
        var program = Program(Move(Direction.Up), middle, Move(Direction.Down));

        Assert.Equal("ud", Show(ProgramEditing.Remove(program, middle.Id)!));
    }

    [Fact]
    public void Remove_Repeat_RemovesBodyToo()
    {
        var repeat = Repeat(3, Move(), Repeat(2, Move()));
        var program = Program(Move(Direction.Down), repeat);

        var result = ProgramEditing.Remove(program, repeat.Id)!;

        Assert.Equal("d", Show(result));
        Assert.Equal(1, result.CountAll());
    }

    [Fact]
    public void Remove_BlockInBody_KeepsRepeatIdAndCount()
    {
        var inside = Move(Direction.Left);
        var repeat = Repeat(5, Move(), inside);

        var result = ProgramEditing.Remove(Program(repeat), inside.Id)!;

        var newRepeat = Assert.IsType<RepeatCommand>(result.Commands[0]);
        Assert.Equal(repeat.Id, newRepeat.Id);
        Assert.Equal(5, newRepeat.Count);
        Assert.Single(newRepeat.Body);
    }

    [Fact]
    public void Remove_UnknownId_ReturnsNull() =>
        Assert.Null(ProgramEditing.Remove(Program(Move()), Guid.NewGuid()));

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(7)]
    public void SetRepeatCount_InRange_ChangesCountAndKeepsBodyAndId(int count)
    {
        var repeat = Repeat(3, Move(Direction.Right));

        var result = ProgramEditing.SetRepeatCount(Program(repeat), repeat.Id, count)!;

        var changed = Assert.IsType<RepeatCommand>(result.Commands[0]);
        Assert.Equal(count, changed.Count);
        Assert.Equal(repeat.Id, changed.Id);
        Assert.Equal(repeat.Body[0].Id, changed.Body[0].Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-5)]
    [InlineData(3)]
    public void SetRepeatCount_OutOfRangeOrUnchanged_ReturnsNull(int count)
    {
        var repeat = Repeat(3);

        Assert.Null(ProgramEditing.SetRepeatCount(Program(repeat), repeat.Id, count));
    }

    [Fact]
    public void SetRepeatCount_NestedRepeat_ChangesOnlyThatOne()
    {
        var inner = Repeat(2, Move());
        var outer = Repeat(4, inner);

        var result = ProgramEditing.SetRepeatCount(Program(outer), inner.Id, 9)!;

        Assert.Equal("4(9(u))", Show(result));
    }

    [Fact]
    public void SetRepeatCount_OnMoveOrUnknownId_ReturnsNull()
    {
        var move = Move();
        var program = Program(move);

        Assert.Null(ProgramEditing.SetRepeatCount(program, move.Id, 5));
        Assert.Null(ProgramEditing.SetRepeatCount(program, Guid.NewGuid(), 5));
    }

    [Fact]
    public void MoveUp_SwapsWithPreviousSibling()
    {
        var down = Move(Direction.Down);
        var program = Program(Move(Direction.Up), down);

        Assert.Equal("du", Show(ProgramEditing.MoveUp(program, down.Id)!));
    }

    [Fact]
    public void MoveDown_SwapsWithNextSibling()
    {
        var up = Move(Direction.Up);
        var program = Program(up, Move(Direction.Down));

        Assert.Equal("du", Show(ProgramEditing.MoveDown(program, up.Id)!));
    }

    [Fact]
    public void MoveUp_OverRepeat_DoesNotEnterItsBody()
    {
        var up = Move(Direction.Up);
        var program = Program(Repeat(2, Move(Direction.Left)), up);

        Assert.Equal("u2(l)", Show(ProgramEditing.MoveUp(program, up.Id)!));
    }

    [Fact]
    public void MoveUp_FirstInBody_LeavesBodyDirectlyBeforeRepeat()
    {
        var first = Move(Direction.Left);
        var repeat = Repeat(2, first, Move(Direction.Right));
        var program = Program(Move(Direction.Up), repeat);

        var result = ProgramEditing.MoveUp(program, first.Id)!;

        Assert.Equal("ul2(r)", Show(result));
    }

    [Fact]
    public void MoveDown_LastInBody_LeavesBodyDirectlyAfterRepeat()
    {
        var last = Move(Direction.Right);
        var repeat = Repeat(2, Move(Direction.Left), last);
        var program = Program(repeat, Move(Direction.Down));

        var result = ProgramEditing.MoveDown(program, last.Id)!;

        Assert.Equal("2(l)rd", Show(result));
    }

    [Fact]
    public void MoveUp_FirstInInnerBody_LandsInOuterBodyBeforeInner()
    {
        var first = Move(Direction.Left);
        var inner = Repeat(3, first);
        var outer = Repeat(2, Move(Direction.Up), inner);

        var result = ProgramEditing.MoveUp(Program(outer), first.Id)!;

        Assert.Equal("2(ul3())", Show(result));
    }

    [Fact]
    public void MoveUp_FirstTopLevelBlock_IsNotPossible()
    {
        var first = Move();
        var program = Program(first, Move());

        Assert.Null(ProgramEditing.MoveUp(program, first.Id));
        Assert.False(ProgramEditing.CanMoveUp(program, first.Id));
    }

    [Fact]
    public void MoveDown_LastTopLevelBlock_IsNotPossible()
    {
        var last = Move();
        var program = Program(Move(), last);

        Assert.Null(ProgramEditing.MoveDown(program, last.Id));
        Assert.False(ProgramEditing.CanMoveDown(program, last.Id));
    }

    [Fact]
    public void MoveUpAndDown_UnknownId_ReturnsNull()
    {
        var program = Program(Move(), Move());

        Assert.Null(ProgramEditing.MoveUp(program, Guid.NewGuid()));
        Assert.Null(ProgramEditing.MoveDown(program, Guid.NewGuid()));
    }

    [Fact]
    public void MoveUpAndDown_Repeat_MovesWithBody()
    {
        var repeat = Repeat(2, Move(Direction.Left), Repeat(3, Move(Direction.Right)));
        var program = Program(Move(Direction.Up), repeat, Move(Direction.Down));

        var up = ProgramEditing.MoveUp(program, repeat.Id)!;
        var down = ProgramEditing.MoveDown(program, repeat.Id)!;

        Assert.Equal("2(l3(r))ud", Show(up));
        Assert.Equal("ud2(l3(r))", Show(down));
        Assert.Equal(repeat.Id, up.Commands[0].Id);
    }

    [Fact]
    public void Move_ToEarlierIndex_ReordersAndKeepsId()
    {
        var last = Move(Direction.Down);
        var program = Program(Move(Direction.Up), Move(Direction.Left), last);

        var result = ProgramEditing.Move(program, last.Id, BlockSlot.Root(0))!;

        Assert.Equal("dul", Show(result));
        Assert.Equal(last.Id, result.Commands[0].Id);
    }

    [Fact]
    public void Move_ToLaterIndex_UsesIndexOfProgramBeforeTheMove()
    {
        var first = Move(Direction.Up);
        var program = Program(first, Move(Direction.Left), Move(Direction.Down));

        Assert.Equal("lud", Show(ProgramEditing.Move(program, first.Id, BlockSlot.Root(2))!));
        Assert.Equal("ldu", Show(ProgramEditing.Move(program, first.Id, BlockSlot.Root(3))!));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Move_ToOwnPosition_ReturnsNull(int index)
    {
        var second = Move();
        var program = Program(Move(), second, Move());

        Assert.Null(ProgramEditing.Move(program, second.Id, BlockSlot.Root(index)));
        Assert.False(ProgramEditing.CanMove(program, second.Id, BlockSlot.Root(index)));
    }

    [Fact]
    public void Move_IntoRepeatBody_Works()
    {
        var up = Move(Direction.Up);
        var repeat = Repeat(2);
        var program = Program(up, repeat);

        var result = ProgramEditing.Move(program, up.Id, new BlockSlot(repeat.Id, 0))!;

        Assert.Equal("2(u)", Show(result));
    }

    [Fact]
    public void Move_RepeatWithBodyIntoOtherRepeat_KeepsBody()
    {
        var moved = Repeat(3, Move(Direction.Left));
        var target = Repeat(2);
        var program = Program(moved, target);

        var result = ProgramEditing.Move(program, moved.Id, new BlockSlot(target.Id, 0))!;

        Assert.Equal("2(3(l))", Show(result));
    }

    [Fact]
    public void Move_RepeatIntoOwnBody_ReturnsNull()
    {
        var repeat = Repeat(3, Move());

        Assert.Null(ProgramEditing.Move(Program(repeat), repeat.Id, new BlockSlot(repeat.Id, 0)));
    }

    [Fact]
    public void Move_RepeatIntoBodyOfItsDescendant_ReturnsNull()
    {
        var inner = Repeat(1);
        var outer = Repeat(3, inner);

        Assert.Null(ProgramEditing.Move(Program(outer), outer.Id, new BlockSlot(inner.Id, 0)));
    }

    [Fact]
    public void Move_RepeatIntoDepthThree_ReturnsNull()
    {
        var moved = Repeat(3);
        var inner = Repeat(1);
        var program = Program(moved, Repeat(2, inner));

        Assert.Null(ProgramEditing.Move(program, moved.Id, new BlockSlot(inner.Id, 0)));
    }

    [Fact]
    public void Move_RepeatWithNestedRepeatIntoRepeat_ReturnsNull()
    {
        var moved = Repeat(3, Repeat(1));
        var target = Repeat(2);

        Assert.Null(ProgramEditing.Move(Program(moved, target), moved.Id, new BlockSlot(target.Id, 0)));
    }

    [Fact]
    public void Move_UnknownIdOrBadSlot_ReturnsNull()
    {
        var move = Move();
        var program = Program(move, Move());

        Assert.Null(ProgramEditing.Move(program, Guid.NewGuid(), BlockSlot.Root(0)));
        Assert.Null(ProgramEditing.Move(program, move.Id, BlockSlot.Root(5)));
        Assert.Null(ProgramEditing.Move(program, move.Id, BlockSlot.Root(-1)));
        Assert.Null(ProgramEditing.Move(program, move.Id, new BlockSlot(Guid.NewGuid(), 0)));
    }

    [Fact]
    public void Move_AtFullProgram_StillWorks()
    {
        var last = Move(Direction.Down);
        var commands = Enumerable.Range(0, ProgramLimits.MaxProgramCommands - 1).Select(_ => (Command)Move()).Append(last).ToArray();
        var program = new ProgramDefinition(commands);

        var result = ProgramEditing.Move(program, last.Id, BlockSlot.Root(0));

        Assert.NotNull(result);
        Assert.Equal(last.Id, result.Commands[0].Id);
    }

    [Fact]
    public void Operations_DoNotChangeTheOriginalProgram()
    {
        var repeat = Repeat(3, Move());
        var program = Program(repeat);

        ProgramEditing.Insert(program, Move(), new BlockSlot(repeat.Id, 1));
        ProgramEditing.SetRepeatCount(program, repeat.Id, 8);
        ProgramEditing.Remove(program, repeat.Id);

        Assert.Equal("3(u)", Show(program));
    }

    private static (ProgramDefinition Program, Guid[] Ids, Guid?[] Parents) NestedSample()
    {
        var inner = Repeat(2, Move(Direction.Left), Move(Direction.Right));
        var outer = Repeat(3, Move(Direction.Down), inner, Move(Direction.Up));
        var empty = Repeat(2);
        var program = Program(Move(), outer, Move(Direction.Right), empty, Repeat(4, Move(Direction.Left)));
        Guid[] ids = [.. Flatten(program.Commands).Select(c => c.Id), Guid.NewGuid()];
        Guid?[] parents = [null, .. Flatten(program.Commands).OfType<RepeatCommand>().Select(r => (Guid?)r.Id), Guid.NewGuid()];
        return (program, ids, parents);
    }

    private static IEnumerable<Command> Flatten(IEnumerable<Command> commands) =>
        commands.SelectMany(c => c is RepeatCommand r ? new[] { c }.Concat(Flatten(r.Body)) : [c]);

    [Fact]
    public void CanMove_MatchesMove_ForEverySlotOfANestedProgram()
    {
        var (program, ids, parents) = NestedSample();
        var checkedSlots = 0;

        foreach (var id in ids)
        {
            foreach (var parent in parents)
            {
                for (var index = -1; index <= 7; index++)
                {
                    var slot = new BlockSlot(parent, index);
                    Assert.True(
                        ProgramEditing.Move(program, id, slot) is not null == ProgramEditing.CanMove(program, id, slot),
                        $"id {id}, slot {parent}:{index}");
                    checkedSlots++;
                }
            }
        }

        Assert.True(checkedSlots > 500);
    }

    [Fact]
    public void CanMoveUpAndDown_MatchMoveUpAndDown_ForEveryBlock()
    {
        var (program, ids, _) = NestedSample();

        foreach (var id in ids)
        {
            Assert.Equal(ProgramEditing.MoveUp(program, id) is not null, ProgramEditing.CanMoveUp(program, id));
            Assert.Equal(ProgramEditing.MoveDown(program, id) is not null, ProgramEditing.CanMoveDown(program, id));
        }
    }

    [Fact]
    public void CanInsertMoveAndRepeat_MatchCanInsert_ForEverySlot()
    {
        var (program, _, parents) = NestedSample();
        var full = new ProgramDefinition([.. Enumerable.Range(0, ProgramLimits.MaxProgramCommands).Select(_ => (Command)Move())]);

        foreach (var candidate in new[] { program, full, ProgramDefinition.Empty })
        {
            foreach (var parent in parents)
            {
                for (var index = -1; index <= 7; index++)
                {
                    var slot = new BlockSlot(parent, index);
                    Assert.Equal(ProgramEditing.CanInsert(candidate, Move(), slot), ProgramEditing.CanInsertMove(candidate, slot));
                    Assert.Equal(ProgramEditing.CanInsert(candidate, Repeat(3), slot), ProgramEditing.CanInsertRepeat(candidate, slot));
                }
            }
        }
    }
}
