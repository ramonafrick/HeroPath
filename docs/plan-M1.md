# Plan: M1 – Domain der Module Levels und Game

**Date**: 2026-11-03 · Grundlage: `docs/requirements.md`

## Modul-Überblick

| Projekt | Ordner | Neu in M1 |
|---|---|---|
| `HeroPath.Modules.Levels` | `Domain/` | `Position`, `ObstacleKind`, `ItemKind`, `PlacedObstacle`, `PlacedItem`, `Grid`, `LevelDefinition` |
| | `Application/` | `LevelValidator`, `LevelValidationError`, `LevelValidationErrorKind` |
| | `Infrastructure/` | `LevelJsonContext`, `LevelJson` (Read/Write), `LevelReadResult` |
| `HeroPath.Modules.Game` | `Domain/` | `Direction`, `Person`, `MoveOutcome`, `MoveResult`, `ICommand`, `MoveCommand`, `RepeatCommand`, `ProgramDefinition`, `ExecutionState`, `ExecutionError`, `ExecutionStep`, `Interpreter` |
| Tests | `HeroPath.Modules.Levels.Tests`, `HeroPath.Modules.Game.Tests` | xUnit-Tests pro Slice |

Keine UI-, Host- oder `.UI`-Projekte betroffen, keine neuen Pakete, kein neues Modul. Die Abhängigkeitsrichtung `Shared ← Levels ← Game` bleibt unverändert, Game nutzt `Grid`/`Position`/`ObstacleKind`/`ItemKind` aus Levels.

Hinweis zum Schnitt: M1 hat keine sichtbare Oberfläche. Die Slices schneiden deshalb nicht durch die UI, sondern durch die Schichten Domain → Application → Infrastructure eines Moduls. Jeder Slice ist für sich bau- und testbar.

## Slices

| # | Name | Ziel | Projekte/Dateien | Skills | Abhängig von |
|---|---|---|---|---|---|
| S1 | Levels-Domain | Unveränderliches Spielfeld | `Modules.Levels/Domain/*` · `Levels.Tests/Domain/GridTests.cs`, `LevelDefinitionTests.cs` | architecture, dotnet10, game-rules | – |
| S2 | Validator + JSON | Level-Daten prüfen, lesen und schreiben, ohne Absturz | `Modules.Levels/Application/LevelValidator*.cs`, `Infrastructure/LevelJson*.cs` · `Levels.Tests/Application/LevelValidatorTests.cs`, `Infrastructure/LevelJsonTests.cs` | architecture, dotnet10, game-rules | S1 |
| S3 | Bewegung + Bausteine | Held bewegen, Programm aufbauen | `Modules.Game/Domain/{Direction,Person,MoveOutcome,MoveResult,ICommand,MoveCommand,RepeatCommand,ProgramDefinition}.cs` · `Game.Tests/Domain/PersonTests.cs`, `CommandTests.cs` | architecture, dotnet10, game-rules | S1 |
| S4 | Interpreter | Ein Ausführungsweg für Play und Debug | `Modules.Game/Domain/{Interpreter,ExecutionStep,ExecutionState,ExecutionError}.cs` · `Game.Tests/Domain/InterpreterTests.cs` | architecture, dotnet10, game-rules | S3 |

### S1 – Levels-Domain
- `Position` (`readonly record struct`), `ObstacleKind`, `ItemKind`, `PlacedObstacle(Position, ObstacleKind)`, `PlacedItem(Position, ItemKind)`.
- `Grid`: Konstruktor prüft die Grenzen aus `LevelLimits`, Start/Ziel/Objekte im Feld, Start ≠ Ziel und Belegungskonflikte, und wirft bei Verstössen `ArgumentException`. Methoden: `IsInside`, `GetObstacle`, `GetItem`, `Items`, `RequiredItemCount` (Anzahl Erdbeeren), `RequireAllItems`.
- `LevelDefinition` (Record, Felder gemäss JSON-Schema, `CurrentSchemaVersion = 1`) mit `ToGrid()`.
- **Tests:** `IsInside` an allen vier Rändern, `GetObstacle`/`GetItem`, ungültige Konstruktion wirft, `RequiredItemCount` zählt keine Pizza, `ToGrid` überträgt alle Felder.
- **Akzeptanz:** Build grün, Tests grün, Domain ohne Abhängigkeit zu Application/Infrastructure.

### S2 – Validator + JSON
- `LevelValidator.Validate(LevelDefinition) → IReadOnlyList<LevelValidationError>`, alle Regeln aus FR5. `LevelValidationError(Kind, Position?)`.
- `LevelJsonContext` (source-generiert, camelCase, `UseStringEnumConverter`), `LevelJson.Write(LevelDefinition) → string`, `LevelJson.Read(string) → LevelReadResult` (Level oder Fehlerliste). `Read` fängt `JsonException`/`NotSupportedException` und ruft den Validator auf. Eine unbekannte `schemaVersion` gibt einen Fehler.
- **Tests:** pro Regel ein positiver und ein negativer Test, ein gültiges Level gibt eine leere Liste. JSON-Round-Trip, das Beispiel-JSON aus `game-rules`, kaputtes JSON, `null`, unbekannte Enum-Werte, `schemaVersion: 2`.
- **Akzeptanz:** keine Exception aus `Read` bei beliebigem Text, Tests grün.

### S3 – Bewegung + Bausteine
- `Direction`, `MoveOutcome`, `MoveResult(Outcome, NewPosition, Hit)`, `Person(Position)` mit `Move(Direction, Grid)`.
- `ICommand { Guid Id }`, `MoveCommand(Direction)`, `RepeatCommand(Count, Body)`. Ist `Count ∉ 1..20` oder geht der Body tiefer als 2 Ebenen, wirft der Konstruktor `ArgumentOutOfRangeException`. `ProgramDefinition(Commands)` prüft die Tiefe ebenfalls.
- **Tests:** Bewegung in 4 Richtungen, Rand auf 4 Seiten, 3 Hindernisarten, Bewegung auf Start/Ziel/Item. Anzahl 0/1/20/21, Tiefe 2 ok, Tiefe 3 wirft, leerer Body ist erlaubt, Ids sind eindeutig.
- **Akzeptanz:** Tests grün, `Person` ohne Name/Avatar.

### S4 – Interpreter
- `ExecutionState` (`Ready`, `Running`, `Succeeded`, `Failed`), `ExecutionError` (`EmptyProgram`, `EmptyRepeat`, `Obstacle`, `OutOfBounds`, `StepLimitExceeded`, `GoalNotReached`, `ItemsMissing`), `ExecutionStep` gemäss FR12.
- `Interpreter(ProgramDefinition, Grid)`: `State`, `Position`, `CollectedItems`, `Step()`. Ausführungsstapel `(commands, index, remaining)`. Vor dem ersten Schritt werden ein leeres Programm und leere Wiederhole-Blöcke geprüft (der erste in Ausführungsreihenfolge). Ein Endzustand bleibt stabil.
- **Tests:** alle Akzeptanzkriterien zum Interpreter aus `requirements.md`, inkl. `CommandId` in verschachtelten Blöcken, Ziel mit Restbausteinen, fehlenden Erdbeeren (weiterlaufen, `ItemsMissing`), Pizza optional, einmaliges Einsammeln, Schritt 1000/1001, stabilem Endzustand, Fehler beim ersten inneren Schritt.
- **Akzeptanz:** alle Tests grün, keine Exception im regulären Lauf.

E2E: keiner, weil sich nichts Sichtbares ändert. Phase 5 entfällt.

## Befehle

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx
dotnet format HeroPath.slnx --verify-no-changes
```

Reihenfolge: S1 → S2 → S3 → S4. S2 und S3 sind unabhängig voneinander.
