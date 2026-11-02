---
name: level-design
description: Creating and checking HeroPath levels — the 7 standard level templates and their learning goals, difficulty curve, writing level JSON as embedded resources in HeroPath.Modules.Levels, and a mandatory automated check that every level is valid and solvable (BFS) within the game limits. Use when adding, changing or reviewing standard levels, or when building level-editor validation.
---

# Level design

The JSON schema and validation rules are in the `game-rules` skill. This skill covers *which* levels to build and how to check them.

## Standard templates (docs/Projektidee.md)

| # | id | Idea | Learning goal | Shape |
|---|---|---|---|---|
| 1 | `free-field` | 10×10, no obstacles | trying things out, sequence | start bottom-left, goal top-right |
| 2 | `easy-1` | one obstacle between start and goal | detour | goal in a line with start, one tree in between |
| 3 | `easy-2` | two obstacles | planning | two detours in different directions |
| 4 | `strawberries` | a few items on the way, `requireAllItems: true` | problem decomposition | 2–3 strawberries close to the direct path |
| 5 | `small-maze` | "3 forward, 2 up, 1 down", meandering | sequence, debugging | narrow corridors made of rocks/ponds |
| 6 | `long-road` | long straight stretches | **repeat block** | e.g. 15×5, a long row, a turn, another long row |
| 7 | `castle-maze` | bigger board for the skilled | everything combined, nested repeat | 15×15 to 20×20 |

Must-have: at least 4 levels (1–4). Levels 5–7 are a bonus.

## Design rules

- The first levels are solvable in **≤ 10 blocks**, so early wins are quick.
- Each level introduces **at most one** new concept compared to the previous one.
- Mix obstacles (tree, rock, pond) so all graphics are visible early.
- No dead ends that only lead to frustration in the early levels. In `small-maze` and `castle-maze` they are allowed, but not too many.
- `long-road`: the shortest solution without repeat should need at least 3 times as many blocks as with repeat.
- Start and goal never directly adjacent (except in a tutorial level).
- Every level fits the limits: 3–20 per side, solution far below 1000 steps.

## Files

- `src/Modules/Levels/HeroPath.Modules.Levels/Infrastructure/Levels/<nn>-<id>.json` (e.g. `01-free-field.json`). The order comes from the file prefix.
- Embedded via `<EmbeddedResource Include="Infrastructure\Levels\*.json" />` (already a wildcard in the csproj, so no change per level).
- Add the display name as `Level_<id>` in **both** `LevelsResources` resx files (see the `localization` skill).

## Mandatory check (test in HeroPath.Modules.Levels.Tests)

One test class `StandardLevelsTests`, which for **every** embedded level:

1. deserializes it (source-gen context),
2. validates it with `LevelValidator` (no errors),
3. checks with **BFS** over the grid (4 directions, obstacles blocked) that the goal is reachable. With `requireAllItems`, check that every required item and then the goal are reachable from the start,
4. checks that ids are unique and match the file name.

```csharp
public static TheoryData<string> LevelIds => [.. StandardLevels.All.Select(l => l.Id)];

[Theory, MemberData(nameof(LevelIds))]
public void Level_IsSolvable(string id) { /* BFS */ }
```

The BFS helper stays in the test project. The game itself has no solver (and no hint system, that's out of scope).

## When designing a new level

1. Sketch the board as ASCII first (`S` start, `G` goal, `T` tree, `R` rock, `P` pond, `*` strawberry, `o` pizza, `.` free) and show it to the user.
2. Note the intended solution as a block sequence and count its length.
3. Write the JSON, add the resx keys, run the tests.
