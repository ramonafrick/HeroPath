# Requirements: M1 – Domain der Module Levels und Game

**Date**: 2026-11-02

## Problem

Alle Spielregeln (Spielfeld, Bewegung, Bausteine, Interpreter, Gewinn- und Fehlerbedingungen) sollen vor der Oberfläche als reiner, getesteter C#-Code vorliegen. Die Meilensteine M2 bis M5 bauen darauf auf: Die UI ruft nur noch `Interpreter.Step()` auf und zeigt `ExecutionStep` an. Ohne diese Grundlage würde Spiellogik ins Razor-Markup wandern. Play und Debug könnten sich dann unterschiedlich verhalten, und die Kinder (11 bis 13 Jahre) bekämen am Zukunftstag widersprüchliche Ergebnisse oder Abstürze.

## Users & Context

- Direkte Nutzer von M1 sind Entwickler:innen und die späteren UI-Meilensteine (M2 bis M5). Es gibt noch keine sichtbare Oberfläche.
- Indirekt die Kinder: Sie sehen die Ergebnisse der Domain später als Bewegung, Hervorhebung und kindgerechte Meldung.
- Laufzeit: Blazor WebAssembly im Browser (Host `HeroPath.Web`). Die Domain ist plattformneutral und läuft auch in einem späteren MAUI-Host, der aber nicht Teil von M1 ist.

## Functional Requirements

### Modul Levels (`HeroPath.Modules.Levels`)

- FR1: `Position` als `readonly record struct (int X, int Y)`. Der Ursprung `(0,0)` liegt oben links, X wächst nach rechts, Y nach unten.
- FR2: `ObstacleKind` (`Tree`, `Rock`, `Pond`) und `ItemKind` (`Strawberry`, `Pizza`) als Enums.
- FR3: `Grid` ist nach dem Erstellen unveränderlich. Es enthält `Width`, `Height`, `Start`, `Goal`, die Hindernisse und die Items sowie `RequireAllItems`. Methoden: `IsInside(Position)`, `GetObstacle(Position) → ObstacleKind?`, `GetItem(Position) → ItemKind?`, dazu eine Abfrage der Pflicht-Items (nur Erdbeeren). Ungültige Daten beim Erstellen führen zu einer Exception, weil sie ein Programmierfehler sind. Geprüfte Daten kommen über den `LevelValidator`.
- FR4: `LevelDefinition` als unveränderliches Record, das dem JSON-Schema aus dem `game-rules`-Skill entspricht: `schemaVersion`, `id`, `name` (null bei Standard-Levels), `width`, `height`, `start`, `goal`, `requireAllItems`, `obstacles[]`, `items[]`. `LevelDefinition` erzeugt ein `Grid`.
- FR5: `LevelValidator` (Application) liefert eine Liste von Fehlerwerten (Enum plus betroffene Position, wo sinnvoll) und wirft keine Exception. Er prüft:
  - Breite und Höhe je 3 bis 20
  - Start, Ziel, jedes Hindernis und jedes Item liegen im Feld
  - Start ≠ Ziel
  - kein Hindernis auf Start oder Ziel, kein Item auf dem Start
  - höchstens ein Hindernis oder Item pro Feld
  - `requireAllItems = true` braucht mindestens eine Erdbeere
  - unbekannte `schemaVersion` ergibt einen Fehler
- FR6: Level-JSON (Infrastructure): ein source-generierter `LevelJsonContext` mit camelCase-Eigenschaften und Enums als camelCase-Strings, dazu eine Lese- und Schreibmethode. Ungültiges oder kaputtes JSON gibt ein Fehlerergebnis zurück und stürzt nie ab.

### Modul Game (`HeroPath.Modules.Game`)

- FR7: `Direction` (`Up`, `Down`, `Left`, `Right`). `Up` bedeutet `Y − 1`, `Down` bedeutet `Y + 1`.
- FR8: `Person` hält **nur** die `Position`, ohne Name und ohne Avatar (Entscheid 2026-11-02). `Move(Direction, Grid) → MoveResult` mit `MoveOutcome` `Moved`, `Blocked` (mit `Hit = ObstacleKind`) oder `OutOfBounds`. Bei `Blocked` und `OutOfBounds` bleibt die Position unverändert.
- FR9: `ICommand` mit stabiler `Guid Id`, dazu `MoveCommand(Direction)` und `RepeatCommand(Count, Body)`. Gilt `Count ∉ 1..20` oder die Verschachtelung ist tiefer als 2, wirft der Konstruktor von `RepeatCommand` bzw. `ProgramDefinition` eine `ArgumentOutOfRangeException` (Entscheid 2026-11-02). Ein leerer Body ist beim Erstellen erlaubt.
- FR10: `ProgramDefinition` ist eine geordnete Liste von `ICommand`.
- FR11: Der `Interpreter` wird mit `new Interpreter(program, grid)` erstellt. Er legt die `Person` selbst auf `grid.Start`. Vor dem ersten Schritt ist `State = Ready`. `Step()` führt genau einen `MoveCommand` aus. Wiederhole-Blöcke werden über einen Ausführungsstapel schrittweise aufgelöst, nicht vorab flachgeklopft.
- FR12: `ExecutionStep` (Record) enthält `CommandId`, `StepNumber` (ab 1), `Position`, `State` (`Ready`, `Running`, `Succeeded`, `Failed`), `Error` (`ExecutionError?`), `CollectedItem` (`ItemKind?`, das in diesem Schritt eingesammelte Item), `HitObstacle` (`ObstacleKind?`) und `MissingItems` (Anzahl, bei `ItemsMissing`).
- FR13: Der Interpreter stellt `CollectedItems` (`IReadOnlySet<Position>`) bereit. Ein Item wird beim Betreten des Feldes eingesammelt, und zwar nur einmal.
- FR14: Fehler und Endzustände sind Werte, keine Exceptions:

  | Situation | Ergebnis |
  |---|---|
  | Programm leer | `Failed`, `EmptyProgram`, keine Bewegung |
  | Ein Wiederhole-Block (auch verschachtelt) ist leer | `Failed`, `EmptyRepeat`, `CommandId` des leeren Blocks, keine Bewegung. Geprüft wird vor dem ersten Schritt |
  | Hindernis | `Failed`, `Obstacle`, `HitObstacle`, `CommandId` des auslösenden Bausteins |
  | Spielfeldrand | `Failed`, `OutOfBounds`, `CommandId` des auslösenden Bausteins |
  | Mehr als 1000 Schritte | `Failed`, `StepLimitExceeded`. 1000 Schritte sind erlaubt, der 1001. wird nicht ausgeführt |
  | Programm zu Ende, Ziel nicht erreicht | `Failed`, `GoalNotReached` |
  | Programm zu Ende auf dem Ziel, Erdbeeren fehlen (`RequireAllItems`) | `Failed`, `ItemsMissing`, `MissingItems` = Anzahl |
  | Ziel erreicht und Item-Bedingung erfüllt | `Succeeded`, sofort. Übrige Bausteine laufen nicht |

- FR15: Betritt der Held das Ziel, während noch Erdbeeren fehlen, läuft das Programm weiter. Die Pizza zählt nie für `RequireAllItems`.
- FR16: Nach einem Endzustand liefert jeder weitere `Step()` denselben End-Schritt und ändert nichts mehr. Reset bedeutet: einen neuen Interpreter erstellen.

## Non-Functional Requirements

- NFR1: Die Domain ist synchron und deterministisch, ohne Blazor, JS, `HttpClient` oder Plattformzugriff. `HeroPath.ArchitectureTests` bleibt grün.
- NFR2: Keine NuGet-Pakete in den reinen Projekten. JSON nur mit `System.Text.Json` aus dem Framework, mit Source-Generator (trimming-fest für WebAssembly).
- NFR3: Zu jeder Regel gibt es einen xUnit-Test in `tests/HeroPath.Modules.<X>.Tests`.
- NFR4: Kein Heldenname in der Domain, also auch nicht in Logs, `ToString()` oder Exceptions.
- NFR5: `dotnet build`, `dotnet test --solution HeroPath.slnx` und `dotnet format --verify-no-changes` sind grün.

## Technical Constraints

- Zuständige Projekte: `HeroPath.Modules.Levels` (Domain, Application/`LevelValidator`, Infrastructure/JSON) und `HeroPath.Modules.Game` (Domain). Tests in `HeroPath.Modules.Levels.Tests` und `HeroPath.Modules.Game.Tests`.
- Abhängigkeitsrichtung `Shared ← Levels ← Game`. Game verwendet `Grid`, `Position`, `ObstacleKind` und `ItemKind` aus Levels.
- Grenzwerte stehen bereits in `LevelLimits` und `ProgramLimits` und werden wiederverwendet.
- Kein Speicher, kein `IAppStorage`, keine UI, keine resx-Texte. Die Domain liefert Fehlerarten und Zahlen, die Texte folgen in M2/M3.
- Kein E2E-Test, weil sich nichts Sichtbares ändert. Phase 5 (Visual Check) entfällt.
- Dokumentation nachführen: `docs/Architektur C4.md` §4.2 (Person ohne Name/Avatar, `Interpreter(program, grid)`, neue Felder in `ExecutionStep`, `ItemKind`) und der `game-rules`-Skill.

## Acceptance Criteria

- [ ] Bewegung in alle 4 Richtungen, Rand auf jeder Seite, jede Hindernisart ist getestet.
- [ ] Wiederhole einfach und verschachtelt (Tiefe 2) läuft. Tiefe 3 sowie Anzahl 0 und 21 werfen eine Exception.
- [ ] Ein leerer Wiederhole-Block (auch innen) ergibt `EmptyRepeat` mit der richtigen `CommandId`, ohne Bewegung.
- [ ] `CommandId` zeigt in verschachtelten Wiederhole-Blöcken auf den richtigen inneren `MoveCommand`.
- [ ] Ist das Ziel erreicht und es bleiben Bausteine übrig, endet der Lauf mit `Succeeded` nach genau diesem Schritt.
- [ ] Ziel mit fehlenden Erdbeeren: Der Lauf geht weiter. Fehlen am Programmende noch welche, gibt es `ItemsMissing` mit der richtigen Anzahl. Eine nicht eingesammelte Pizza verhindert `Succeeded` nicht.
- [ ] Items werden nur einmal eingesammelt. `CollectedItem` erscheint nur im Schritt des Einsammelns.
- [ ] Schrittlimit: genau 1000 Schritte sind erlaubt, der 1001. ergibt `StepLimitExceeded`.
- [ ] Der Endzustand bleibt bei wiederholtem `Step()` stabil.
- [ ] `LevelValidator`: zu jeder Regel aus FR5 ein positiver und ein negativer Test.
- [ ] JSON: Round-Trip (schreiben, lesen) ergibt dasselbe `LevelDefinition`. Das Beispiel-JSON aus dem `game-rules`-Skill wird gelesen. Kaputtes JSON, eine unbekannte `schemaVersion` und unbekannte Enum-Werte geben ein Fehlerergebnis ohne Exception.
- [ ] Build, Format und alle Tests inklusive ArchitectureTests sind grün.

## Edge Cases & Failure Modes

- Held läuft über das Startfeld zurück → erlaubt, `Moved`.
- Held läuft über ein Item, ohne dass `RequireAllItems` gesetzt ist → das Item wird eingesammelt, die Gewinnbedingung ändert sich nicht.
- `RequireAllItems` ist gesetzt, im Level liegt aber nur Pizza → der Validator meldet einen Fehler. Erstellt man das `Grid` direkt, gibt es eine Exception.
- `Wiederhole 20 × (Wiederhole 20 × …)` mit mehr als 1000 Schritten → `StepLimitExceeded` nach dem 1000. Schritt, ohne Hänger.
- Der Fehler tritt beim ersten Schritt eines Wiederhole-Blocks auf → `CommandId` des inneren `MoveCommand`, nicht des Wiederhole-Blocks.
- Kaputtes oder manipuliertes JSON aus dem Speicher (später) → Fehlerergebnis, nie ein Absturz.
- Darf nie passieren: Eine Exception erreicht die UI während eines regulären Laufs, oder Play und Debug liefern unterschiedliche Ergebnisse.

## Out of Scope

- UI, `GameSession`, `ExecutionRunner`, Tempo, Animation (M2)
- Standard-Level-Dateien und Lösbarkeits-Check per BFS (M4, `level-design`-Skill)
- `ILevelSource`, `LevelRepository`, Speichern eigener Levels über `IAppStorage` (M5)
- Kindgerechte Meldungstexte und resx (M2/M3)
- Heldenname und Avatar (UI-Zustand, M2/M4)
- Schritt zurück, relative Steuerung, Wenn-Blöcke
- MAUI-Host

## Open Questions

- Keine. Diese Punkte habe ich selbst aus den Docs abgeleitet:
  - Der 1001. Schritt wird nicht mehr ausgeführt.
  - Leere Wiederhole-Blöcke werden vor dem ersten Schritt im ganzen Programm gesucht. Gemeldet wird der erste in Ausführungsreihenfolge.
  - Der Interpreter erzeugt die `Person` selbst. Abweichung von §4.2: `new Interpreter(program, grid)` statt `new Interpreter(program, grid, person)`, damit der Held immer auf dem Start beginnt.
