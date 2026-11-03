# Plan: M4 – Level-Vorlagen, Start-Dialog, Items

**Date**: 2026-11-03 · Grundlage: `docs/requirements.md` (freigegeben) · Vorgänger: `plan-M3.md` · Freigegeben mit D1–D5 wie empfohlen (Variante A), Reihenfolge S1 → S2 → S3 → S4

## Modul-Überblick

| Projekt | Änderung |
|---|---|
| `HeroPath.Modules.Levels` | 7 Level-JSON (`01-…` bis `07-…`), `00-demo.json` entfernt |
| `HeroPath.Modules.Levels.UI` | `GameBoard` zeigt Items (mit Einsammel-Animation) und hat einen Vorschau-Modus; `LevelPicker`; Level-Namen in resx |
| `HeroPath.Shared.UI` | Grafiken `HeroLisa`, `ItemStrawberry`, `ItemPizza`; Palette-Variablen |
| `HeroPath.Modules.Game` (Application) | `HeroChoice` + `HeroAvatar` (Held, Name, Namensregeln), `LevelProgress` (Reihenfolge, gelöst, nächstes), `GameSession.Load(LevelDefinition)` (leert das Programm bei anderem Level), `GameSession.CollectedItems`, `GameMessage.PizzaCount` |
| `HeroPath.Modules.Game.UI` | `StartPage` (`/`), `PlayPage` zieht auf `/play` um, `HeroBadge`, `ItemCounter`, `HeroFigure` (Max oder Lisa), `MessageBox` mit Namen, Pizza und «alle geschafft», Knopf «Nächstes Level» |
| `HeroPath.Web` | keine Änderung (Router kennt die Assemblies schon) |
| Tests | Levels-, Game-, Shared-Tests; E2E: Hilfsmethode «über die Startseite spielen», neue `StartPageTests`, `LevelSwitchTests`, `ItemTests` |

Abhängigkeiten bleiben `Shared ← Levels ← Game`. Kein neues Modul, kein neues Paket (siehe D4).

## Level-Skizzen (FR15 bis FR18)

Legende: `S` Start, `G` Ziel, `T` Baum, `R` Stein, `P` Teich, `*` Erdbeere, `o` Pizza, `.` frei. (0,0) oben links.

**1 `free-field`** (10×10, Freies Feld / Open field): S (2,7), G (7,3), keine Hindernisse. Lösung: 5× Rechts, 4× Hoch = **9 Bausteine**. (Abweichung vom Skill «bottom-left → top-right» auf 10×10: so wären es 18 Bausteine, die Regel «erste Level ≤ 10» geht vor.)

**2 `easy-1`** (10×10, Einfach 1 / Easy 1): S (2,5), G (7,5), Baum (5,5).
Lösung: R R U R R R D = **7 Bausteine**.

**3 `easy-2`** (10×10, Einfach 2 / Easy 2): bisheriges Demo-Level: S (1,6), G (6,3), Baum (3,6), Stein (4,3), Teich (6,5). Lösung: U U R R R R R U = **8 Bausteine**. Drei statt zwei Hindernisse, dafür alle drei Arten früh sichtbar (siehe D5).

**4 `strawberries`** (10×10, Erdbeeren sammeln / Collect strawberries), `requireAllItems: true`:
```
y2  . . . . . P G . . .
y4  . T . * . . * . . .
y6  . . . . R o . . . .
y7  . S . * . . . . . .
```
(nur Zeilen mit Inhalt; Erdbeeren (3,7), (3,4), (6,4); Pizza (5,6); Baum (1,4), Stein (4,6), Teich (5,2)). Lösung: R R, U U U, R R R, U U = **10 Bausteine**, Pizza als Umweg.

**5 `small-maze`** (8×5, Kleines Labyrinth / Small maze):
```
     0 1 2 3 4 5 6 7
y0   . . . . . . . .
y1   . . . . . T . .
y2   . . P . . . R .
y3   . . . . P . . G
y4   S . . . R . . .
```
Lösung: R R R, U U, R R, D, R R = **10 Bausteine** («3 vor, 2 hoch, 1 runter»). Umwege über Zeile 0/1 oder 4 sind länger (12).

**6 `long-road`** (15×5, Lange Strasse / Long road): Zeile 3 von x=0 bis x=13 ist ein Fluss (14× Teich), S (0,4), G (0,2).
Lösung ohne Wiederhole: 14× R, 2× U, 14× L = **30 Bausteine**; mit Wiederhole: [14× R] [2× U] [14× L] = **6 Bausteine** (Faktor 5, Regel ≥ 3).

**7 `castle-maze`** (15×15, Burg-Labyrinth / Castle maze), `requireAllItems: true`: Treppe von S (0,14) nach G (12,2) in Stufen zu 2. Hindernisse (abwechselnd Baum, Stein, Teich) rechts jeder Stufe (3,14), (5,12), (7,10), (9,8), (11,6), (13,4) und über jeder Stufenmitte (1,13), (3,11), (5,9), (7,7), (9,5), (11,3). Erdbeeren auf der Treppe (4,11), (8,7); Pizza (14,14) als Bonus.
Lösung: [6× [2× R] [2× U]] = **5 Bausteine** mit verschachteltem Wiederhole (24 Züge).

Alle Level prüft `StandardLevelsTests` (gültig, BFS lösbar, mit Erdbeeren). Neu im Test: Level 1 bis 4 haben einen kürzesten Weg ≤ 10 Züge.

## Entscheide (freigegeben: jeweils Variante A)

**D1 – Wo liegen Held und Name?**
- A (empfohlen): `HeroChoice` (scoped) in `Modules.Game/Application`: `Avatar` (`HeroAvatar` Max/Lisa), `Name` (normalisiert), `IsChosen`, `Choose(avatar, rawName)`, `DefaultName(avatar)`, statisch `NormalizeName`. Mit xUnit testbar (NFR1). Nicht in `Domain`, nicht in `GameSession`/`GameMessage`, kein `ToString` mit Namen, keine Speicherung (Entscheid 3b bleibt: der Name gelangt nie in die Domain).
- B: als UI-Dienst in `Game.UI/Services`. Weniger «richtig» nach NFR1, sonst gleich.

**D2 – Levelwechsel leert das Programm (FR13)**
- A (empfohlen): `GameSession.Load(LevelDefinition level)` ersetzt `Load(Grid)`. Die Session hält `Level` (Id + Grid). Gleiches Level (Id und Grid gleich) = nichts; anderes Level = Lauf verworfen **und** Programm, Auswahl, Hinweis gelöscht. Tests: `TestLevels.Demo` liefert ein `LevelDefinition` mit dem bisherigen Demo-Layout im Code (37 Aufrufe, mechanisch).
- B: `Load(Grid)` behalten, `PlayPage` ruft zusätzlich `Clear()`. Regel in der UI, widerspricht NFR1.

**D3 – Gelöst-Status und Level-Reihenfolge**
- A (empfohlen): `LevelProgress` (scoped, `Modules.Game/Application`) mit der geordneten Level-Liste (in `AddGameModule` aus `StandardLevels.All`): `Levels`, `IsSolved(id)`, `AllSolved`, `NextAfter(id)`, `MarkSolved(id)`, `Changed`. Der `ExecutionRunner` ruft `MarkSolved` beim Schritt mit `Succeeded` auf (er ist schon der einzige Schreiber des Laufs). Ctor `ExecutionRunner(GameSession, LevelProgress, TimeProvider)`, 3 Testaufrufe ändern sich. `MessageBox` liest `AllSolved` für FR23.
- B: `LevelProgress` hört auf `GameSession.Changed`. Kein Ctor-Wechsel, aber er markiert nur, wenn ihn vorher jemand aufgelöst hat (scoped, lazy): versteckte Reihenfolge-Abhängigkeit.

**D4 – Fluent UI für Startseite und LevelPicker?** (Entscheid Nr. 6 sah `FluentDialog` und `FluentSelect` vor)
- A (empfohlen): keine Fluent-Komponenten in M4. Startseite mit eigenen Karten (`<button aria-pressed>` bzw. Radio-Gruppe), LevelPicker als natives `<select>` mit «✓ » vor gelösten Leveln. Kein Setup, gleicher Stil wie Palette, MAUI-neutral. Entscheid Nr. 6 wird in `Architektur C4.md` angepasst.
- B: `FluentSelect` für den LevelPicker. Erste Verwendung des Pakets: Dienste, `FluentProviders` im Layout (laut NuGet-Seite v5), Skript-Einbindung für v5 **unverifiziert**, Styling gegen den Fluent-Look, bUnit-Setup. Quellen: [NuGet 5.0.0](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/5.0.0).

**D5 – Bisherige Tests auf das Demo-Layout**
- A (empfohlen): `easy-2` übernimmt das Demo-Layout (3 Hindernisse statt 2). Unit-Tests behalten ihr Layout (`TestLevels` im Code), E2E-Tests ändern nur den Einstieg (Startseite → `easy-2`) und den Level-Namen.
- B: neue Layouts, alle E2E-Koordinaten umschreiben (Board, Hindernis, Rand, Lösungen in 10 Dateien).

**D6 – Item-Animation (FR20)**, nur ein Weg sinnvoll: `GameBoard` bekommt `CollectedItems` (`IReadOnlySet<Position>`). Ein eingesammeltes Item bleibt im DOM mit `data-collected="true"` und einer CSS-Animation (Hüpfen, dann `opacity: 0`, `animation-fill-mode: forwards`); bei `prefers-reduced-motion` direkt `visibility: hidden`. Reset leert die Menge, das Item ist wieder da. `GameSession.CollectedItems` sammelt `step.Position`, wenn `step.CollectedItem` gesetzt ist.

**D7 – Navigation:** relative Pfade (`NavigationManager.NavigateTo("play")` bzw. `""`), damit GitHub Pages mit Unterpfad funktioniert (`release`-Skill, `base href`). `/play` ohne `HeroChoice.IsChosen` → `NavigateTo("", replace: true)`.

## Slices

| # | Name | Ziel | Hängt ab von |
|---|---|---|---|
| S1 | Level-Vorlagen | 7 Level, Demo weg, Namen DE/EN, Tests | – |
| S2 | Startseite und Held | `/` mit Held, Name, Level-Karten; `/play`; Lisa; Name in Meldungen | S1 |
| S3 | Levelwechsel und Fortschritt | LevelPicker, «Nächstes Level», Haken, «alle geschafft» | S2 |
| S4 | Items | Grafiken, Anzeige, Animation, Zähler, Pizza im Erfolgstext | S2 (S3 nicht nötig) |

### S1 – Level-Vorlagen
- **Dateien:** `Levels/Infrastructure/Levels/01-free-field.json` … `07-castle-maze.json`, `00-demo.json` löschen; `LevelsResources(.en).resx` (`Level_<id>`, `Level_demo` weg); `PlayPage.razor.cs` lädt vorläufig `easy-2`; `tests/HeroPath.Modules.Game.Tests/TestLevels.cs` (Demo-Layout im Code); `StandardLevelsTests` (7 Ids in Reihenfolge, Level 1–4 kürzester Weg ≤ 10, `long-road`-Faktor ≥ 3 über die Skizzen-Lösung ist nicht automatisch prüfbar → nur BFS-Länge 30 ≥ 3 × 6 als Konstante dokumentiert).
- **Tests:** xUnit (`StandardLevelsTests`, alle Game-Tests mit neuem `TestLevels`); E2E `BoardTests` (Name «Einfach 2»).
- **Skills:** `level-design`, `game-rules`, `localization`.
- **Abnahme:** FR15–FR18, `dotnet test` grün.

### S2 – Startseite und Held
- **Dateien:** `Game/Application/HeroChoice.cs`, `HeroAvatar.cs`; `GameSession.Load(LevelDefinition)` (D2); `Shared.UI/Graphics/HeroLisa.razor` (+ Palette in `theme.css`, `Graphic_HeroLisa`); `Game.UI/Pages/StartPage.razor(.cs,.css)`; `Game.UI/Components/HeroFigure` (Max/Lisa nach `HeroChoice`), `HeroBadge` (Bild, Name, «Held wechseln»); `GameBoard` Parameter `Preview` (ohne Held, `aria-hidden`, Grösse vom Container); `PlayPage` auf `/play`, Umleitung (D7), lädt `HeroChoice`-Level; `MessageBox` nutzt `HeroChoice.Name`; resx DE/EN; `AddGameModule` registriert `HeroChoice`.
- **Test-Ids:** `start-page`, `hero-max`, `hero-lisa` (`aria-pressed`), `hero-name`, `level-card-{id}` (`aria-pressed`, `data-solved`), `level-preview-{id}`, `start-go`, `hero-badge`, `hero-badge-name`, `hero-change`.
- **Tests:** xUnit `HeroChoiceTests` (Trim, Steuerzeichen, 20 Zeichen, leer → Standard, Heldwechsel mit/ohne eigenen Namen), `GameSessionTests` (Load gleiches/anderes Level, Programm geleert); bUnit `StartPageTests`, `MessageBoxTests` (Name), `GraphicsTests` (Lisa); E2E `StartPageTests`: Lisa + Name + Level → `/play` (Screenshots `start-page`, `start-page-lisa`, `play-lisa`), `/play` direkt → `/` , Sprachwechsel behält Eingaben (`start-page-en`). Alle bestehenden E2E-Tests starten über die Hilfsmethode `PlayAsync(page, "easy-2")`.
- **Skills:** `architecture`, `blazor-ui`, `svg-assets`, `localization`, `e2e-playwright`, `game-rules`.
- **Abnahme:** FR1–FR10, FR13 (gleiches/anderes Level über Startseite), FR14, NFR2, NFR4, NFR8.

### S3 – Levelwechsel und Fortschritt
- **Dateien:** `Game/Application/LevelProgress.cs` (D3); `ExecutionRunner` ruft `MarkSolved`; `Levels.UI/Components/LevelPicker.razor` (Parameter Levels, gewählte Id, gelöste Ids, `EventCallback<string>`; natives `<select>` nach D4); `PlayPage` (Picker, «Nächstes Level» bei `Succeeded` und `NextAfter` ≠ null); Haken auf `StartPage`-Karten; `MessageBox` FR23; resx.
- **Test-Ids:** `level-picker`, `next-level`, `data-solved="true"` auf Karten.
- **Tests:** xUnit `LevelProgressTests`, `ExecutionRunnerTests` (Erfolg markiert, Fehler nicht); bUnit `LevelPickerTests`, `MessageBoxTests` (alle geschafft); E2E `LevelSwitchTests`: `easy-1` lösen → Haken, «Nächstes Level» → `easy-2` mit leerem Programm, Picker-Wechsel leert Programm (Screenshots `play-success-next`, `level-picker`, `start-page-solved`).
- **Skills:** `game-rules`, `blazor-ui`, `localization`, `e2e-playwright`.
- **Abnahme:** FR5, FR11–FR13, FR23.

### S4 – Items
- **Dateien:** `Shared.UI/Graphics/ItemStrawberry.razor`, `ItemPizza.razor` (+ Palette, `Graphic_*`); `GameBoard` zeigt Items (`data-kind` `strawberry|pizza`, `data-collected`), Parameter `CollectedItems` (D6); `GameSession.CollectedItems` (Reset/Load leeren); `GameMessage.PizzaCount` (in `FromStep` aus der Session befüllt); `Game.UI/Components/ItemCounter`; `MessageBox` FR22; resx.
- **Test-Ids:** `item-counter-strawberry`, `item-counter-pizza` (mit `data-collected`, `data-total`), Zellen `data-kind="strawberry|pizza"`, `data-collected="true"`.
- **Tests:** xUnit `GameSessionTests` (CollectedItems nach Schritten, Reset, Load), `GameMessageTests` (PizzaCount); bUnit `GameBoardTests` (Items, collected), `ItemCounterTests`, `MessageBoxTests` (Pizza Einzahl/Mehrzahl, DE/EN), `GraphicsTests`; E2E `ItemTests` auf `strawberries`: Debug schrittweise, Zähler 1 von 3, Item verschwindet, Pizza-Umweg, Erfolg mit Pizza-Text (Screenshots `items-start`, `items-collected`, `items-success`), ohne Erdbeeren → «Es fehlen noch …».
- **Skills:** `svg-assets`, `blazor-ui`, `game-rules`, `localization`, `e2e-playwright`.
- **Abnahme:** FR19–FR22, NFR4, NFR5.

## Befehle

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx
dotnet format HeroPath.slnx --verify-no-changes
HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E
```
