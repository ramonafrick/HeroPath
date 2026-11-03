# Plan: M2 – Blazor-Grundgerüst, Spielfeld, Figur, Play, Debug, Reset

**Date**: 2026-11-03 · Grundlage: `docs/requirements.md` (freigegeben) · Vorgänger: `plan-M1.md`

## Modul-Überblick

| Projekt | Neu in M2 |
|---|---|
| `HeroPath.Shared` | `Application/Contracts/StorageKeys` (Schlüssel `Language`) |
| `HeroPath.Shared.UI` | `wwwroot/css/theme.css` (Palette, Basis-Styles), `Graphics/` (HeroMax, StartField, GoalCastle, ObstacleTree, ObstacleRock, ObstaclePond), `Layout/MainLayout` mit Kopfzeile, `Localization/LanguageService`, `Components/LanguageSwitcher`, `Resources/SharedResources` |
| `HeroPath.Modules.Levels` | `Infrastructure/Levels/00-demo.json`, `Infrastructure/StandardLevels` (lädt eingebettete Levels, siehe D2) |
| `HeroPath.Modules.Levels.UI` | `Components/GameBoard`, `Resources/LevelsResources`, Registrierung in `AddLevelsModule()` |
| `HeroPath.Modules.Game` | `Application/GameSession`, `Application/ExecutionRunner`, `Application/RunStatus`, `Application/Tempo` |
| `HeroPath.Modules.Game.UI` | `Pages/PlayPage` (Layout), `Components/BlockPalette`, `Components/ProgramEditor`, `Components/ControlBar` (Play/Debug/Reset, Tempo, Status), `Resources/GameResources`, Registrierung in `AddGameModule()` |
| `Hosts/HeroPath.Web` | `Services/LocalStorageAppStorage`, Registrierung, Sprache beim Start setzen, `index.html` verlinkt `theme.css` |
| Tests | `HeroPath.Shared.Tests` (neu, siehe D3), `HeroPath.Modules.Levels.Tests`, `HeroPath.Modules.Game.Tests`, `HeroPath.E2E` |

Kein neues Modul. Abhängigkeiten bleiben `Shared ← Levels ← Game` (gleich für `.UI`). `GameBoard` (Levels.UI) kennt keine Game-Typen: Es bekommt `Grid`, Heldposition, hervorgehobenes Feld und Animationsdauer als Parameter. Den Helden selbst rendert es über einen `RenderFragment`-Parameter (`HeroContent`), den `PlayPage` mit `HeroMax` aus Shared.UI befüllt.

## Slices

| # | Name | Ziel | Skills | Abhängig von |
|---|---|---|---|---|
| S1 | Grundgerüst + Sprache | Kopfzeile mit App-Titel und DE/EN-Umschalter, Sprache gespeichert, Theme | architecture, dotnet10, blazor-ui, localization, e2e-playwright | – |
| S2 | Spielfeld + Figur | Demo-Level mit Start, Burg, Hindernissen und Max auf `/` sichtbar | architecture, dotnet10, game-rules, level-design, svg-assets, blazor-ui, localization, e2e-playwright | S1 (Theme, Texte) |
| S3 | Programm per Klick | Palette und Programm-Editor: anhängen, entfernen, alles löschen, Limit 200 | architecture, dotnet10, game-rules, blazor-ui, localization, e2e-playwright | S2 |
| S4 | Play, Debug, Reset | Ausführung mit Tempo, Status, Hervorhebung, Sperren, Animation | architecture, dotnet10, game-rules, blazor-ui, localization, e2e-playwright | S3 |

### S1 – Grundgerüst + Sprache
- **Dateien:** `Shared/Application/Contracts/StorageKeys.cs`; `Shared.UI/wwwroot/css/theme.css`, `Layout/MainLayout.razor(.css)`, `Localization/LanguageService.cs`, `Components/LanguageSwitcher.razor(.cs/.css)`, `Resources/SharedResources.cs/.resx/.en.resx` (`App_Title`, `Language_German`, `Language_English`, `Language_Switch_Label`); `SharedUIExtensions` registriert `LanguageService`; `Hosts/HeroPath.Web/Services/LocalStorageAppStorage.cs` (JS-Interop `localStorage.getItem/setItem/removeItem`, fängt `JSException` ab), `Program.cs` (`AddScoped<IAppStorage, …>`, nach `Build()` `ApplyStoredCultureAsync()`), `index.html` (Link auf `_content/HeroPath.Shared.UI/css/theme.css`).
- **Verhalten:** Erlaubte Kulturen `de-CH`, `en`. Unbekannter oder fehlender Wert → `de-CH`. Umschalten setzt `CultureInfo.Default*`/`Current*`, speichert und löst `Changed` aus. Komponenten mit Texten abonnieren `Changed`. `<PageTitle>` zeigt den lokalisierten App-Titel.
- **Tests:** xUnit `LanguageServiceTests` (Standard, gültiger gespeicherter Wert, Müll → `de-CH`, Speichern beim Wechsel, Speicher wirft → Spiel läuft weiter), bUnit `LanguageSwitcherTests` (aktive Sprache markiert, Klick wechselt) mit `InMemoryAppStorage`.
- **E2E:** `LanguageTests.Switch_ChangesTexts_AndPersists`: `/` öffnen, Titel «HeldenWeg», auf EN wechseln, «Hero's Path», neu laden, weiterhin EN. Screenshots `play-page.start.desktop.de`, `play-page.start.desktop.en`.
- **Akzeptanz:** FR18, FR19 (ohne Programmteil, der in S4 ergänzt wird), localStorage blockiert → keine Konsolenfehler.

### S2 – Spielfeld + Figur
- **Dateien:** `Levels/Infrastructure/Levels/00-demo.json`, `Levels/Infrastructure/StandardLevels.cs` (D2); `Shared.UI/Graphics/*.razor` (6 SVGs, `viewBox 0 0 100 100`, `<title>` über `Graphic_<Name>` in `SharedResources`); `Levels.UI/Components/GameBoard.razor(.cs/.css)`, `Levels.UI/Resources/LevelsResources` (`Board_Label`, `Level_demo`); `Game.UI/Pages/PlayPage.razor` lädt `demo`, zeigt `GameBoard` mit Max auf dem Start; `Levels.UI`-Assembly bleibt in `App.razor`.
- **Board:** CSS Grid `repeat(var(--cols), 1fr)`, `aspect-ratio: 1` pro Zelle, Zellen mit `data-testid="cell-{x}-{y}"` und `data-kind` (`start`, `goal`, `tree`, `rock`, `pond`, `empty`). Held als eigene Ebene: Breite = 1 Zelle, `transform: translate(calc(var(--x) * 100%), calc(var(--y) * 100%))`, `transition` mit `var(--hp-step-duration)`, `data-testid="hero"` mit `data-x`/`data-y`. Markiertes Feld: Parameter `MarkedCell`, Klasse + `data-marked="true"`.
- **Tests:** xUnit `StandardLevelsTests` (jedes eingebettete Level lädt, ist gültig, per BFS lösbar, Id passt zum Dateinamen), bUnit `GameBoardTests` (Zellenzahl = Breite×Höhe, Start/Ziel/Hindernisse an der richtigen Stelle, Held-Koordinaten, markiertes Feld).
- **E2E:** `BoardTests.PlayPage_ShowsDemoLevel`: Board sichtbar, Held auf `cell-1-6`. Screenshots `board.start.{desktop,laptop}.{de,en}`.
- **Akzeptanz:** FR1 bis FR4, NFR5 (Graustufen-Screenshot).

### S3 – Programm per Klick
- **Dateien:** `Game/Application/GameSession.cs` (Level/`Grid`, Liste der `MoveCommand`, `Add(Direction)`, `Remove(Guid)`, `Clear()`, `CanAdd`, `IsEditable`, `event Action? Changed`, `ToProgram()` → `ProgramDefinition`); `Game.UI/Components/BlockPalette.razor(.cs/.css)` (`data-testid="palette-up|down|left|right"`), `ProgramEditor.razor(.cs/.css)` (`data-testid="program"`, Bausteine `program-block` mit `data-index`, `data-command-id`, Entfernen `program-remove-{index}`, `program-clear`); `GameResources` (`Block_Up/Down/Left/Right`, `Program_Title`, `Program_Empty`, `Program_Remove`, `Program_Clear`, `Palette_Title`); `AddGameModule()` registriert `GameSession` scoped.
- **Tests:** xUnit `GameSessionTests` (Anhängen in Reihenfolge, Entfernen per Id, Leeren, Limit 200 → `CanAdd = false` und kein Throw, `Changed` feuert), bUnit `BlockPaletteTests`, `ProgramEditorTests` (Liste, Entfernen, Leeren, leeres Programm zeigt Hinweis, deaktiviert wenn nicht editierbar).
- **E2E:** `ProgramEditingTests.Click_AddsAndRemovesBlocks`. Screenshots `program.editing.{desktop,laptop}.{de,en}`.
- **Akzeptanz:** FR5 bis FR7.

### S4 – Play, Debug, Reset
- **Dateien:** `Game/Application/RunStatus.cs` (`Ready`, `Running`, `Paused`, `Failed`, `Succeeded`), `Tempo.cs` (`Slow` 900 ms, `Medium` 500 ms, `Fast` 200 ms), `ExecutionRunner.cs` (besitzt `Interpreter` und `CancellationTokenSource`; `PlayAsync()`, `StepOnce()`, `Reset()`, `Tempo`; wendet jeden `ExecutionStep` auf `GameSession` an); `GameSession` ergänzt `Status`, `CurrentStep`, `HeroPosition`; `Game.UI/Components/ControlBar.razor(.cs/.css)` (`control-play`, `control-debug`, `control-reset`, `control-tempo` als `<input type="range" min=0 max=2>`, `status` mit `data-state` und `data-error`); `ProgramEditor` hebt `CurrentStep.CommandId` hervor (`data-current="true"`, bei Fehler `data-error="true"`); `PlayPage` übergibt Heldposition, markiertes Feld und Schrittdauer an `GameBoard`.
- **Regeln:** Gleicher `Interpreter` für Play und Debug. Play nach Debug macht weiter. Play/Debug gesperrt bei leerem Programm, während Play (beides) und nach Endzustand. Editor gesperrt ab dem ersten Schritt bis Reset. Reset bricht das Token ab, erstellt einen neuen `Interpreter`, Programm bleibt. Nach Abbruch wird kein Schritt mehr angewendet. Markiertes Feld bei `Obstacle` = Hindernisfeld (Position + Richtung), bei `OutOfBounds` = Feld des Helden.
- **Tests:** xUnit `ExecutionRunnerTests` mit `FakeTimeProvider` (D1): Play bis `Succeeded`, Wartezeit je Tempo, Debug genau ein Schritt → `Paused`, Debug dann Play setzt fort, Reset mitten in Play (kein weiterer Schritt), Fehlerfälle `Obstacle`/`OutOfBounds`/`GoalNotReached`, doppelter Play-Aufruf startet keinen zweiten Lauf. bUnit `ControlBarTests` (Sperrlogik je Status, `data-state`/`data-error`, Tempo-Wechsel).
- **E2E:** `RunFlowTests`:
  1. `Play_ReachesCastle` → Screenshots `run.success.{desktop,laptop}.{de,en}`
  2. `Debug_StepsAndContinuesWithPlay` → `run.debug-paused.*`
  3. `Play_HitsObstacle` / `Play_LeavesBoard` → `run.error-obstacle.*`, `run.error-edge.*`
  4. `Reset_RestoresStart_AndUnlocksEditor` → `run.reset.*`
  5. `LanguageSwitch_DuringPause_KeepsProgramAndHero` → `run.language-paused.desktop.{de,en}`
- Tempo in E2E auf «schnell» setzen. Keine Zeitassertionen.
- **Akzeptanz:** FR8 bis FR17, alle Edge Cases aus `requirements.md`.

## Entscheidungen (am 2026-11-03 freigegeben: D1 A, D2 A, D3 A, D4 wie skizziert)

**D1 – Fake-Zeit für `ExecutionRunner`-Tests**
- A) Paket `Microsoft.Extensions.TimeProvider.Testing` (Microsoft, MIT, aktuell 10.11.0 laut `dotnet package search`), nur im Game-Testprojekt. `FakeTimeProvider.Advance()` steuert `Task.Delay(…, timeProvider, ct)` korrekt. So steht es in `.claude/skills/dotnet10/SKILL.md:95`. Doku: https://learn.microsoft.com/dotnet/api/microsoft.extensions.time.testing.faketimeprovider
- B) Eigener Fake im Testprojekt. Dann muss `CreateTimer` samt `ITimer` nachgebaut werden, das ist fehleranfällig. So steht es in `.claude/agents/implementer.md:48` («no mocking library», ein Fake-TimeProvider wird als Beispiel genannt).
- **Empfehlung A.** Es ist ein offizielles Test-Hilfspaket und keine Mocking-Bibliothek. `implementer.md` wird im selben Commit präzisiert.

**D2 – Laden des Demo-Levels**
- A) `StandardLevels` (statisch, Infrastructure) liest alle eingebetteten `Infrastructure/Levels/*.json` über `LevelJson.Read`, sortiert nach Dateipräfix und bietet `All` und `Get(id)`. Die UI ruft es direkt bzw. über `AddLevelsModule()` auf. Der `level-design`-Skill erwartet `StandardLevels.All` bereits.
- B) Wie die Ziel-Architektur: `ILevelSource` (Application/Contracts), `EmbeddedLevelSource` (Infrastructure) und `LevelRepository` (Application). Für ein einziges Level sind das drei Typen auf Vorrat.
- **Empfehlung A.** `LevelRepository` und `ILevelSource` kommen in M5, wenn eigene Levels über `IAppStorage` dazukommen.

**D3 – Tests für `Shared`/`Shared.UI` (LanguageService, LanguageSwitcher, Grafiken)**
- A) Neues Projekt `tests/HeroPath.Shared.Tests` (xUnit + bUnit, referenziert `Shared` und `Shared.UI`), eingetragen in `HeroPath.slnx` und in der Ordnerstruktur `Architektur C4.md` §9.
- B) Diese Tests in `HeroPath.Modules.Game.Tests` ablegen (es referiert `Shared.UI` transitiv). Kein neues Projekt, aber die Zuordnung ist unsauber.
- **Empfehlung A.**

**D4 – Demo-Level-Layout** (Skizze nach `level-design`: `S` Start, `G` Ziel, `T` Baum, `R` Stein, `P` Teich, Ursprung oben links)

```
   x 0123456789
y0   ..........
y1   ..........
y2   ..........
y3   ....R.G...
y4   ..........
y5   ......P...
y6   .S.T......
y7   ..........
y8   ..........
y9   ..........
```
- Start (1,6), Ziel (6,3). Lösung mit 8 Bausteinen: Hoch, Hoch, Rechts ×5, Hoch.
- Fehlerwege für E2E: Rechts, Rechts → Baum (Feld 3,6). Links, Links → Rand. Hoch ×3, Rechts ×3 → Stein. Hoch → Programm zu Ende ohne Burg (`GoalNotReached`).
- Erfüllt die Regeln «≤ 10 Bausteine» und «alle Hindernisarten früh sichtbar». Start und Ziel liegen nicht nebeneinander.
- Datei `00-demo.json`, Id `demo`. In M4 kann es eine Vorlage werden oder entfallen.

## Befehle

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx
dotnet format HeroPath.slnx --verify-no-changes
$env:HEROPATH_E2E = '1'; dotnet test --project tests/HeroPath.E2E     # PowerShell
dotnet watch --project src/Hosts/HeroPath.Web
```

## Pro Slice nachzuführen

- `game-rules`-Skill: neue öffentliche Typen aus Levels/Game (`StandardLevels`, `GameSession`, `ExecutionRunner`, `RunStatus`, `Tempo`), sonst schlägt `ClaudeWorkflowTests` fehl.
- `blazor-ui`-Skill: `data-testid`-Konventionen (`cell-x-y`, `hero`, `program-block`, `control-*`, `status`).
- Commit pro Slice nach grünem Build (Freigabe zum Committen liegt vor).
