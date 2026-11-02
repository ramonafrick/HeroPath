---
name: game-rules
description: HeroPath domain rules in the Levels and Game modules — coordinate system, Grid/Person/Move semantics, commands (Move, Repeat with nesting), the single Interpreter used by Play and Debug, ExecutionStep/ExecutionState, error kinds, win condition with RequireAllItems, limits, level JSON schema and validation, and which tests every rule needs. Use when writing or changing domain/application code in HeroPath.Modules.Levels or HeroPath.Modules.Game or their tests, when the UI needs to interpret execution results, or when a question is about how the game behaves.
---

# HeroPath game rules (Modules Levels + Game)

Docs: `docs/Projektidee.md` (behaviour), `docs/Architektur C4.md` §4.2 (class model), `docs/Projekt_Kontext.md` §4 (limits). Decisions not spelled out in the docs are marked with their decision date and recorded in `Projekt_Kontext.md` §2.

## Which module owns what

| Module | Domain | Application | Infrastructure |
|---|---|---|---|
| `HeroPath.Modules.Levels` | `Position`, `Grid`, `ObstacleKind`, `ItemKind`, `LevelDefinition`, `LevelLimits` | `LevelValidator`, `ILevelSource`, `LevelRepository` | embedded `Levels/*.json`, `LevelJsonContext`, storage-backed custom levels |
| `HeroPath.Modules.Game` | `Direction`, `Person`, `MoveResult`/`MoveOutcome`, `ICommand`, `MoveCommand`, `RepeatCommand`, `ProgramDefinition`, `Interpreter`, `ExecutionStep`/`ExecutionState`/`ExecutionError`, `ProgramLimits` | `GameSession`, `ExecutionRunner` | none so far |

Game depends on Levels (the interpreter runs on a `Grid`), never the other way round.

## Hard boundaries

- Both module projects have **no** Blazor, JS, `HttpClient` or platform references. Pure C#, `Domain` synchronous and deterministic. `tests/HeroPath.ArchitectureTests` enforces this.
- All game behaviour lives here. The UI only calls `Interpreter.Step()` (via `ExecutionRunner`) and renders `ExecutionStep`.
- Every rule change ships with an xUnit test in `tests/HeroPath.Modules.<X>.Tests`.

## Coordinates and grid

- Origin `(0,0)` is **top-left**, `X` grows to the right, `Y` grows downward. `Up` = `Y - 1`, `Down` = `Y + 1`.
- `Position` is a `readonly record struct (int X, int Y)`.
- Width and height each `3..20`, default `10×10`, not necessarily square.
- `Grid` is **immutable** after construction (start, goal, obstacles, items). Run state (person position, collected items, step count) lives in the `Interpreter`, so Reset = create a new interpreter. No undo/"step back" (out of scope).

## Movement (`Person.Move(Direction, Grid)`)

| Target cell | Outcome | Person position |
|---|---|---|
| outside the grid | `MoveOutcome.OutOfBounds` | unchanged |
| obstacle (`Tree`, `Rock`, `Pond`) | `MoveOutcome.Blocked`, `Hit = kind` | unchanged |
| free / item / start / goal | `MoveOutcome.Moved` | target cell |

Items are collected when the person **enters** the cell. The start cell is walkable again.

## Commands

- `ICommand` has a stable `Guid Id` — the UI highlights the block with this id.
- `MoveCommand(Direction)`.
- `RepeatCommand(Count, Body)`: `Count` 1–20, body may contain `MoveCommand` and `RepeatCommand`, **max. nesting depth 2** (a repeat inside a repeat, no deeper).
- `ProgramDefinition` = ordered list of commands. Validate depth and count when constructing / before running, not halfway through execution.
- An empty repeat body is **not** skipped. The run doesn't start and returns `ExecutionError.EmptyRepeat` with the `CommandId` of the empty repeat, so the UI can highlight it (decided 2026-11-02).

## Interpreter (one engine for Play and Debug)

`new Interpreter(program, grid, person)`. `Step()` executes **exactly one move** (one expanded `MoveCommand`) and returns an `ExecutionStep`:

| Field | Meaning |
|---|---|
| `CommandId` | id of the `MoveCommand` that just ran (for highlighting) |
| `StepNumber` | 1-based count of executed moves in this run |
| `Position` | person position after the step |
| `State` | `Running`, `Succeeded`, `Failed` |
| `Error` | `null` or the error kind (see below) |

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

- camelCase properties, enums as camelCase strings (`JsonSourceGenerationOptions(UseStringEnumConverter = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)`).
- Standard levels: `name` is `null`; the UI shows the resx text `Level_<id>`. Custom levels: `name` is the user's text (no resx).
- `schemaVersion` allows later migration of stored custom levels. Unknown versions are rejected with a validation error, not a crash.

### Level validation (`LevelValidator`, returns a list of errors)

- width/height in `3..20`
- start, goal, every obstacle and item inside the grid
- start ≠ goal; no obstacle on start or goal; at most one obstacle or item per cell; no item on start
- `requireAllItems = true` requires at least one required item
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
