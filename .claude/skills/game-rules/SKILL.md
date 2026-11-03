---
name: game-rules
description: HeroPath domain rules in the Levels and Game modules — coordinate system, Grid/Person/Move semantics, commands (Move, Repeat with nesting), the single Interpreter used by Play and Debug, ExecutionStep/ExecutionState, error kinds, win condition with RequireAllItems, limits, level JSON schema and validation, and which tests every rule needs. Use when writing or changing domain/application code in HeroPath.Modules.Levels or HeroPath.Modules.Game or their tests, when the UI needs to interpret execution results, or when a question is about how the game behaves.
---

# HeroPath game rules (Modules Levels + Game)

Docs: `docs/Projektidee.md` (behaviour), `docs/Architektur C4.md` §4.2 (class model), `docs/Projekt_Kontext.md` §4 (limits). Decisions not spelled out in the docs are marked with their decision date and recorded in `Projekt_Kontext.md` §2.

## Which module owns what

| Module | Domain | Application | Infrastructure |
|---|---|---|---|
| `HeroPath.Modules.Levels` | `Position`, `Grid`, `ObstacleKind`, `ItemKind`, `LevelObstacle`, `LevelItem`, `LevelDefinition`, `LevelLimits` | `LevelValidator`, `LevelValidationError`/`LevelValidationErrorKind`, `LevelReadResult`; later `ILevelSource`, `LevelRepository` | `LevelJson` (Read/Write), `LevelJsonContext`; `StandardLevels` (static: `All`, `Get(id)` over the embedded `Levels/*.json`, throws `KeyNotFoundException` for unknown ids and `InvalidOperationException` for an invalid embedded level); later storage-backed custom levels |
| `HeroPath.Modules.Game` | `Direction` + `DirectionExtensions` (`Offset`, `MoveFrom`), `Person`, `MoveResult`/`MoveOutcome`, `Command` (abstract), `MoveCommand`, `RepeatCommand`, `ProgramDefinition`, `BlockSlot`, `ProgramEditing`, `Interpreter`, `ExecutionStep`/`RepeatProgress`/`ExecutionState`/`ExecutionError`, `ProgramLimits` | `GameSession`, `ExecutionRunner`, `RunStatus`, `GameMessage` + `GameMessageKind` + `GameMessageCode`, `Tempo` + `TempoExtensions` (`Duration`), `HeroChoice` + `HeroAvatar`, `LevelProgress` | none so far |

Game depends on Levels (the interpreter runs on a `Grid`), never the other way round.

## Hard boundaries

- Both module projects have **no** Blazor, JS, `HttpClient` or platform references. Pure C#, `Domain` synchronous and deterministic. `tests/HeroPath.ArchitectureTests` enforces this.
- All game behaviour lives here. The UI only calls `Interpreter.Step()` (via `ExecutionRunner`) and renders `ExecutionStep`.
- Every rule change ships with an xUnit test in `tests/HeroPath.Modules.<X>.Tests`.

## Coordinates and grid

- Origin `(0,0)` is **top-left**, `X` grows to the right, `Y` grows downward. `Up` = `Y - 1`, `Down` = `Y + 1`.
- `Position` is a `readonly record struct (int X, int Y)`.
- Width and height each `3..20`, default `10×10`, not necessarily square.
- `Grid` is **immutable** after construction (start, goal, obstacles, items) and has value equality (size, start, goal, `RequireAllItems`, obstacles, items), so `GameSession.Load` recognises a reloaded level. Run state (person position, collected items, step count) lives in the `Interpreter`, so Reset = create a new interpreter. No undo/"step back" (out of scope).

## Movement (`Person.Move(Direction, Grid)`)

| Target cell | Outcome | Person position |
|---|---|---|
| outside the grid | `MoveOutcome.OutOfBounds` | unchanged |
| obstacle (`Tree`, `Rock`, `Pond`) | `MoveOutcome.Blocked`, `Hit = kind` | unchanged |
| free / item / start / goal | `MoveOutcome.Moved` | target cell |

Items are collected when the person **enters** the cell. The start cell is walkable again.

`Person` holds **only** `Position` (decided 2026-11-02). Hero name and avatar are UI state and never enter the domain, so they can't leak into logs, `ToString()` or exceptions.

## Commands

- `Command` is an abstract class with a `private protected` constructor, i.e. a **closed** hierarchy: only `MoveCommand` and `RepeatCommand` exist; the interpreter switches over them without casts. A new block type (e.g. «Wenn» later) is added deliberately here. Every command has a stable `Guid Id` (optional explicit id in the ctor), equality is identity. The UI highlights the block with this id.
- `MoveCommand(Direction)`.
- `RepeatCommand(Count, Body)`: `Count` 1–20, body may contain `MoveCommand` and `RepeatCommand`, **max. nesting depth 2** (a repeat inside a repeat, no deeper).
- `ProgramDefinition` = ordered list of commands, max. `ProgramLimits.MaxProgramCommands` = 200 blocks (counted recursively).
- `ProgramDefinition` also answers tree questions: `CountAll()` (recursive), `Find(id)`, `PathOf(id)` (1-based hierarchical number, e.g. `[3, 2]`; the UI shows `3.2`), `SlotOf(id)` (parent list + index) and `DepthAt(repeatId)` (0 for the top level, `null` for unknown or non-repeat ids) and `FindEmptyRepeat()` (the first empty `RepeatCommand` in program order, outer before inner, or `null`).
- `BlockSlot(Guid? ParentId, int Index)` is an insertion point: the list of a repeat body (`null` = top level) and the position in it. `BlockSlot.Root(index)` is a top-level slot.
- `ProgramEditing` (static, pure) edits the immutable tree: `CanInsert`/`Insert`, `Remove` (a repeat with its body), `Move`/`CanMove` (general move; slot indexes refer to the program before the move; used by drag & drop; `CanMove`, `CanMoveUp/Down` are cheap checks that do not rebuild the tree), `CanInsertMove`/`CanInsertRepeat` (does a new move or empty repeat fit a slot, without building a block), `MoveUp`/`MoveDown`/`CanMoveUp`/`CanMoveDown` (siblings; the first/last block of a repeat body leaves the body directly before/after the repeat; top-level edges are not possible), `SetRepeatCount` (1-20). Each operation rebuilds only the affected path, keeps ids (`RepeatCommand` via its explicit id ctor) and returns a new `ProgramDefinition`, or `null` when the request is invalid or changes nothing (unknown id or slot, depth above 2, a repeat into its own body, 200 blocks, count out of range). User-reachable invalid input never throws.
- Invalid count, depth or block total is a programming error (the UI never builds such a program): `ArgumentOutOfRangeException` in the constructor. Depth is checked only in `RepeatCommand` (`CommandNesting.DepthOf`).
- An empty repeat body is **not** skipped. The run doesn't start. The `ExecutionRunner` checks `FindEmptyRepeat()` before it creates the interpreter (see «Messages»); the interpreter keeps `ExecutionError.EmptyRepeat` with the `CommandId` of the empty repeat, so the UI can highlight it (decided 2026-11-02).

## Interpreter (one engine for Play and Debug)

`new Interpreter(program, grid)`; the interpreter places the `Person` on `grid.Start` itself. `Step()` executes **exactly one move** (one expanded `MoveCommand`) and returns an `ExecutionStep`:

| Field | Meaning |
|---|---|
| `CommandId` | id of the `MoveCommand` that ran or failed; the empty `RepeatCommand` for `EmptyRepeat`; `Guid.Empty` for `EmptyProgram` |
| `StepNumber` | 1-based move number. On `Obstacle`/`OutOfBounds`/`StepLimitExceeded` it is the **attempted** move (executed + 1), so «Schritt N» can be shown directly. Otherwise the last executed move, `0` for `EmptyProgram`/`EmptyRepeat`. Counts expanded moves, not blocks |
| `Position` | person position after the step |
| `State` | `Ready`, `Running`, `Succeeded`, `Failed` |
| `Error` | `null` or the error kind (see below) |
| `CollectedItem` | `ItemKind?` collected in this step only |
| `HitObstacle` | `ObstacleKind?` on `Obstacle` |
| `MissingItems` | missing strawberries, only with `ItemsMissing` |
| `Repeats` | `IReadOnlyList<RepeatProgress>` (`RepeatId`, 1-based `Round`, `Count`), outermost repeat first, for the repeats enclosing the move of `CommandId` (attempted move on `Obstacle`/`OutOfBounds`/`StepLimitExceeded`, otherwise the last executed move); empty outside repeats and for `EmptyProgram`/`EmptyRepeat`. Compared by reference in record equality (M3 S2) |

The interpreter also exposes `State`, `Position` and `CollectedItems` (`IReadOnlySet<Position>`). The step that executes the **last** move already carries the terminal state, so Play needs no extra empty step.

- Repeats are expanded lazily (an execution stack of `(commands, index, remainingIterations)`), not by flattening the whole program up front. That is what keeps `CommandId` correct inside nested repeats.
- Play = UI calls `Step()` in a loop with a delay; Debug = one `Step()` per click. Same results, guaranteed by using the same object.
- After a terminal state (`Succeeded`/`Failed`) further `Step()` calls return the same terminal step and change nothing.
- Before the first step, `State` is `Ready`.

### Errors and end states

| Situation | Result |
|---|---|
| program empty | `Failed`, `ExecutionError.EmptyProgram` (no move happens) |
| a repeat block has an empty body | `Failed`, `ExecutionError.EmptyRepeat` + `CommandId` of that repeat (no move happens) |
| move into obstacle | `Failed`, `ExecutionError.Obstacle` + obstacle kind |
| move outside the grid | `Failed`, `ExecutionError.OutOfBounds` |
| more than **1000** executed moves | `Failed`, `ExecutionError.StepLimitExceeded` |
| program finished, not on goal | `Failed`, `ExecutionError.GoalNotReached` |
| program finished on goal, items missing (`RequireAllItems`) | `Failed`, `ExecutionError.ItemsMissing` + missing count |
| goal reached and item condition met | `Succeeded` |

- Reaching the goal ends the run **immediately** with `Succeeded` once the item condition is met. Remaining blocks are not executed. If items are still missing, execution continues, so the hero can walk on and collect them (decided 2026-11-02).
- Errors are **values**, not exceptions. Exceptions only for programming errors (null arguments, invalid construction).
- Core returns error kinds and numbers. Localized kid-friendly text is the UI's job (see the `localization` skill).

## Items

- Kinds: `Strawberry` (default), `Pizza` (bonus). They can be exchanged via the level data, so model them as an enum plus data, not as hardcoded behaviour.
- `RequireAllItems` (per level, default `false`): the goal only counts once the required items are collected.
- Only `Strawberry` counts towards `RequireAllItems`. `Pizza` is an optional bonus; the UI can celebrate it, but it never blocks success (decided 2026-11-02).

## Level JSON schema

Standard levels are embedded resources in `HeroPath.Modules.Levels/Infrastructure/Levels/*.json`. Custom levels use the same format in storage.

```json
{
  "schemaVersion": 1,
  "id": "free-field",
  "name": null,
  "width": 10,
  "height": 10,
  "start": { "x": 0, "y": 9 },
  "goal": { "x": 9, "y": 0 },
  "requireAllItems": false,
  "obstacles": [ { "x": 4, "y": 5, "kind": "tree" } ],
  "items": [ { "x": 2, "y": 7, "kind": "strawberry" } ]
}
```

- camelCase properties, enums as camelCase strings. `LevelJsonContext` registers a string-only enum converter (`allowIntegerValues: false`) instead of `UseStringEnumConverter`, because the latter also accepts integers. `MaxDepth = 8`.
- Use `LevelJson.Read(string)` / `Write(level)`. `Read` never throws: input over `LevelJson.MaxJsonLength` (64 KB), broken JSON or rule violations return `LevelReadResult.Failure(errors)`.
- The source generator ignores property initializers: an omitted field becomes `default`, so a missing `schemaVersion` or list is a validation error, not a default value.
- Standard levels: `name` is `null`; the UI shows the resx text `Level_<id>`. Custom levels: `name` is the user's text (no resx).
- `schemaVersion` allows later migration of stored custom levels. Unknown versions are rejected with a validation error, not a crash.

### Level validation (`LevelValidator`, returns a list of errors)

- width/height in `3..20`
- start, goal, every obstacle and item inside the grid
- start ≠ goal; no obstacle on start or goal; at most one obstacle or item per cell; no item on start
- `requireAllItems = true` requires at least one required item
- `id` matches `^[a-z0-9-]{1,40}$`; `name` is `null` or 1–50 chars, not blank, no control characters
- more objects than cells → one `TooManyObjects` error, per-object checks skipped (no error amplification)
- `Grid` throws on the same rules (defensive net). Guarantee, covered by tests: validator-valid ⇒ `ToGrid()` doesn't throw
- Validation runs for embedded levels (in a test) **and** for custom levels loaded from storage. An invalid custom level is reported and skipped, never crashes.

## Required tests (HeroPath.Modules.Game.Tests / HeroPath.Modules.Levels.Tests)

- Movement in all 4 directions, edge on every side, every obstacle kind.
- Repeat: simple, nested (depth 2), depth 3 rejected, count 0/21 rejected, empty body → `EmptyRepeat` with the correct `CommandId`.
- Goal reached with blocks remaining → `Succeeded`, the remaining blocks don't run.
- Pizza not collected + `RequireAllItems` → still `Succeeded`.
- `CommandId` points at the correct inner block in nested repeats.
- Step limit exactly at 1000 / 1001.
- Items collected once, `RequireAllItems` true/false, goal with missing items continues.
- Terminal state is stable on repeated `Step()`.
- Every embedded level deserializes, validates and is solvable (see the `level-design` skill).

## Messages (GameSession.Message)

`GameMessage(Kind, Code, BlockPath, Obstacle, Count, PizzaCount)` is a record of values, no text and no hero name (the UI adds the name from `HeroChoice`). `GameMessageKind`: `Error`, `Success`, `Hint`. `GameMessageCode`: `Obstacle`, `OutOfBounds`, `GoalNotReached`, `ItemsMissing`, `StepLimitExceeded`, `Succeeded`, `EmptyRepeat`, `EmptyProgram`, `ProgramFull`, `NestingTooDeep`. `BlockPath` is `Program.PathOf(step.CommandId)` (the attempted move for Obstacle, OutOfBounds, StepLimitExceeded; for EmptyRepeat the repeat; `null` otherwise), `Obstacle` the hit `ObstacleKind`, `Count` the missing strawberries.

`GameSession.Message` returns, in this priority: the run message (set when a terminal step is applied, cleared by Reset and by loading a different grid), then the hint of a refused start (`EmptyRepeat` with `HintedId`, cleared by the next program edit, the first applied step, Reset and loading a different grid; a repeated refusal of the same repeat raises no `Changed`), then, only while the session is editable, a hint recomputed on every read: `EmptyProgram` (no blocks), `ProgramFull` (200 blocks), `NestingTooDeep` (`!CanAddRepeat`, program not full). `HintedId` marks the empty repeat in the editor.

## Run state (ExecutionRunner, GameSession)

- `RunStatus`: `Ready`, `Running` (Play), `Paused` (Debug between steps), `Failed`, `Succeeded`. `Tempo`: `Slow` 900 ms, `Medium` 500 ms (default), `Fast` 200 ms; `Tempo.Duration` (in `TempoExtensions`) maps to the wait and to the board animation.
- `ExecutionRunner(GameSession, LevelProgress, TimeProvider)` is the only writer of run state: `PlayAsync()`, `StepOnce()` (Debug), `Stop()`, `Reset()`, `CanStart`. `GameSession.UpdateRun/PauseRun/ResetRun` are `internal`; tests drive the state through the runner. `Tempo` lives in `GameSession` (settable, raises `Changed`), the runner only reads it.
- `CanStart` is the single start rule for Play and Debug: grid loaded, program not empty (`Program.Commands.Count > 0`), status `Ready` or `Paused`. `PlayAsync`/`StepOnce` are no-ops otherwise, and `ControlBar` binds both buttons to it.
- One `Interpreter` per run, created lazily from `session.ToProgram()` + `session.Grid` on the first Play/Debug after Ready. Play after Debug continues on it.
- Play executes the first step immediately, then `Task.Delay(Tempo.Duration, timeProvider, token)` between steps; a tempo change applies to the next wait. Reset cancels the token; the token is checked after the wait, so no step is applied after Reset. `OperationCanceledException` is caught only in the runner.
- `PlayAsync` runs in `try/finally`: if anything throws mid-run (for example a `Changed` handler), the run ends `Paused` (position and program kept, Debug or Reset still work) and the exception propagates. It never stays `Running`. Its `CancellationTokenSource` is disposed when the run ends and only touched while it is still the runner's current one (an old loop never pauses a newer run).
- `Stop()` cancels a running Play and sets `Paused` (interpreter kept, Play continues where it stopped). `PlayPage` calls it in `Dispose`, so leaving the page leaves no running loop and returning shows a consistent paused state. `Reset()` is the only way back to `Ready`.
- `GameSession.Load(LevelDefinition)`: the same level (same `Id` and an equal record, otherwise an equal grid built only when the record differs) is a no-op (no event; program, run and interpreter stay, so returning to the page or re-rendering never resets anything). A different level discards the run (status `Ready`, no `CurrentStep`, no `MarkedCell`, hero on the new start, editable) and also clears the program, the selection and the hints (decision D2 of M4, FR13). `Level` is the loaded `LevelDefinition`, `Grid` is `Level.ToGrid()` built once per load. The runner listens to the session's internal `RunDiscarded` event, cancels a running Play and drops its interpreter.
- Before the interpreter is created (status `Ready`), `PlayAsync` and `StepOnce` check `Program.FindEmptyRepeat()`: on a hit the session shows the `EmptyRepeat` hint and `HintedId`, status stays `Ready`, there is no interpreter and no step, and the program stays editable. `CanStart` is unchanged.
- Play while `Running`, Play/Debug after an end state and on an empty program are no-ops. Debug while `Running` is ignored. The first step locks editing (`GameSession.IsEditable` is `Status == Ready`); only Reset unlocks it.
- `GameSession` holds the program as `ProgramDefinition Program` (immutable, every edit replaces it) plus the selection. Edits: `Add(Direction)`, `AddRepeat()` (empty, count `GameSession.DefaultRepeatCount` = 3), `Remove`, `Clear`, `MoveUp/MoveDown` (+ `CanMoveUp/CanMoveDown`), `Move(id, BlockSlot)`, `CanMove(id, BlockSlot)`, `CanRemove(id)`, `Add(Direction, BlockSlot)`, `AddRepeat(BlockSlot)`, `CanAddAt(BlockSlot)`/`CanAddRepeatAt(BlockSlot)` (drag & drop validity, built on `ProgramEditing.CanInsertMove/CanInsertRepeat/CanMove`, false while locked; `Add(dir)`/`AddRepeat()` delegate to the slot overloads with `InsertTarget`), `SetRepeatCount`, `Select(Guid?)` (selecting the selected block again toggles it off; unknown ids do nothing). `InsertTarget` (a `BlockSlot`) is the end of the selected repeat's body, the end of the body the selected block sits in, otherwise the end of the program. `CanAdd` = fewer than 200 blocks in total; `CanAddRepeat` = additionally the depth at `InsertTarget` allows a repeat. All edits and `Select` are silent no-ops while not editable, `Changed` fires only for real changes, deleting the selected block clears the selection, and the first applied step of a run clears it. `MarkedCellOf` looks the failed `MoveCommand` up in the whole tree.
- `GameSession.CollectedItems` (`IReadOnlySet<Position>`, the cells whose item was collected: added in `UpdateRun` at `step.Position` when `step.CollectedItem` is set, cleared by `ResetRun` and by `Load` of a different level) and `CollectedCount(ItemKind)` (for the counter). `GameMessage.PizzaCount` is set for `Succeeded` to the pizzas collected in the run (`FromStep` stays internal, the session passes the count; the collecting step is already counted); `MessageBox` turns it into `Message_Succeeded_Pizza_One/_Other` (FR22), the all-solved sentence still appends.
- `GameSession.CurrentStep`, `HeroPosition` (= `CurrentStep.Position`, else `Grid.Start`), `MarkedCell` (computed once when a step is applied): for `Obstacle` the obstacle cell (hero position + direction of the failed `MoveCommand`), for `OutOfBounds` the hero cell, otherwise `null`.

## Level progress (LevelProgress, M4 S3)

- `LevelProgress` (scoped, `Modules.Game/Application`, built in `AddGameModule` from `StandardLevels.All`) holds the ordered levels and which are solved in this tab: `Levels`, `Find(id)`, `IsSolved(id)`, `AllSolved` (false for an empty list), `NextAfter(id)` (the next level, `null` for the last or an unknown id), `MarkSolved(id)` (idempotent, unknown ids ignored, `Changed` only when a level becomes solved). Memory only, never `IAppStorage`.
- `ExecutionRunner` marks `session.Level.Id` solved when it applies a step with state `Succeeded` (Play and Debug), before `GameSession` raises `Changed`. A failed run marks nothing.
- `HeroChoice.SelectLevel(id)` switches the level on the play page (hero and name stay; no-op before a choice or for the same id).

## Hero choice (HeroChoice, HeroAvatar)

- `HeroAvatar`: `Max`, `Lisa`. `HeroChoice` (scoped, `Modules.Game/Application`) holds what the child confirmed on the start page: `Avatar`, `Name`, `IsChosen`, `LevelId` (`string?`, `null` until chosen; the start page pre-selects the first level of `LevelProgress`, `HeroChoice` has no dependency on `StandardLevels`) and `Changed`. `Choose(avatar, rawName, levelId)` normalizes the name and sets `IsChosen`. It never touches `IAppStorage`, has no `ToString` with the name and is never logged: the name stays out of the domain (`Person`, `GameMessage`), the URL and storage.
- `HeroChoice.NormalizeName(raw, avatar)`: removes control characters, invisible format characters (Unicode category Format: zero-width, bidi marks and overrides) and line/paragraph separators, trims, cuts to `MaxNameLength` = 20 text elements (never inside a surrogate pair or a combining sequence), trims again, and an empty result becomes `DefaultName(avatar)` («Max» / «Lisa»).
- `HeroChoice.NameAfterSwitch(input, previous, next)` is the rule for the name field when the child switches hero: an empty input or the previous default name becomes the new default name, any other input is kept.
- `MessageBox` appends `Message_AllSolved` to the success message once `LevelProgress.AllSolved` (FR23), and subscribes to `LevelProgress.Changed`. It shows `HeroChoice.Name` in the messages that name the hero. Before a choice it is the default name of Max.
