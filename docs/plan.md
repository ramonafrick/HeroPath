# Plan: M3 – Drag & Drop, Wiederhole-Block, Block-Hervorhebung, Fehlermeldungen

**Date**: 2026-11-03 · Grundlage: `docs/requirements.md` (freigegeben) · Vorgänger: `plan-M2.md` · Freigegeben mit D1–D5 wie empfohlen, D6 Variante A, Reihenfolge S1 → S4

## Modul-Überblick

| Projekt | Neu bzw. geändert in M3 |
|---|---|
| `HeroPath.Modules.Game` (Domain) | `ProgramEditing` (Einfügen, Verschieben, Entfernen, Anzahl ändern, Ziel prüfen, ▲▼), `BlockSlot` (Einfügestelle), `ProgramDefinition` erhält `PathOf(id)` (Nummer 3.2.1), `FindEmptyRepeat()`, `CountAll`; `ExecutionStep` erhält `Repeats` (Runden), `RepeatProgress` |
| `HeroPath.Modules.Game` (Application) | `GameSession`: Programm als `ProgramDefinition` statt `List<MoveCommand>`, Auswahl und Einfügeziel, Hinweis ohne Lauf; `ExecutionRunner`: Vorprüfung leerer Wiederhole-Block; `GameMessage` (Art, Code, Werte) |
| `HeroPath.Modules.Game.UI` | `BlockPalette` (Wiederhole, Ziehen, Sperren), `ProgramEditor` + neues `ProgramBlock` (rekursiv, einspaltig, Auswahl, ▲▼🗑, −/+, Drop-Zonen), neues `MessageBox`, `ProgramDragState` (Services), `GameResources` (DE/EN) |
| `HeroPath.Shared.UI` | ggf. Symbol für Wiederhole und Meldungsarten (eigene SVG), `theme.css` (Farben für Meldungen) |
| `HeroPath.Modules.Levels(.UI)`, Host | keine Änderung erwartet (Ausnahme D6, Variante B) |
| Tests | `HeroPath.Modules.Game.Tests` (xUnit + bUnit), `HeroPath.E2E` (4 neue Flows), bestehende Tests an Baum-API anpassen |

Kein neues Modul. Abhängigkeiten bleiben `Shared ← Levels ← Game`. Keine neuen NuGet-Pakete.

## Entscheidungen (zur Freigabe)

### D1 – Wo liegt die Baum-Bearbeitung?
- **A (Empfehlung):** Reine Funktionen in `Game/Domain/ProgramEditing` auf der unveränderlichen `ProgramDefinition`: Jede Operation baut den betroffenen Pfad neu (`RepeatCommand` mit gleicher `Id`, Konstruktor erlaubt explizite Id) und liefert eine neue `ProgramDefinition` bzw. `null`, wenn ungültig. `GameSession` hält nur `ProgramDefinition Program`. + Domain mit xUnit testbar, Interpreter bekommt dieselbe Instanz, Grenzen prüft weiterhin der Konstruktor (Schutznetz), Ids stabil für `@key`. − Neubau pro Bearbeitung (bei ≤ 200 Bausteinen unerheblich).
- **B:** Veränderbarer Entwurfs-Baum in `Application` (eigene Knotenklassen), `ToProgram()` konvertiert. − Zweites Modell derselben Sache, Grenzen doppelt.
- Quelle: `RepeatCommand.cs` (Id im Konstruktor, Kopie des Rumpfs), `Architektur C4.md` §6 Nr. 3a (geschlossene Hierarchie), `game-rules` (Fehler sind Werte, Exceptions nur Programmierfehler).

### D2 – Rundenzähler
- **A (Empfehlung):** `ExecutionStep` erhält `IReadOnlyList<RepeatProgress> Repeats` (aussen → innen; `RepeatProgress(Guid RepeatId, int Round, int Count)`). Der `Interpreter` kennt die Runde schon im `Frame` (`Remaining`), es fehlt nur die Repeat-Id. + Play und Debug identisch, da gleiche Quelle. − `record`-Gleichheit vergleicht die Liste per Referenz; heute vergleicht kein Test ganze `ExecutionStep`s (geprüft: `grep "Assert.Equal(new ExecutionStep"` ohne Treffer). Gleichheit wird im Doc-Kommentar vermerkt.
- **B:** UI rechnet Runden aus der Schrittfolge nach. − Spiellogik in der UI, bei Debug-Sprüngen fehleranfällig.

### D3 – Leerer Wiederhole-Block startet keinen Lauf
- **A (Empfehlung):** `ProgramDefinition.FindEmptyRepeat()` (bisher privat im `Interpreter`) wird öffentlich. `ExecutionRunner.PlayAsync`/`StepOnce` prüfen vorher; bei Treffer setzt die Session einen Hinweis (`GameMessage` EmptyRepeat + markierte Id), Status bleibt `Ready`, kein Interpreter wird erstellt. Die nächste Bearbeitung löscht den Hinweis. Der Interpreter behält `EmptyRepeat` als Schutznetz. `CanStart` bleibt unverändert (Knöpfe aktiv, damit der Klick den Hinweis auslöst).
- **B:** `CanStart` liefert `false` bei leerem Wiederhole. − Kind sieht nur gesperrte Knöpfe, ohne Grund (widerspricht FR21).

### D4 – Wer bestimmt die Meldung?
- **A (Empfehlung):** `Application/GameMessage` (`Kind`: Error/Success/Hint; `Code`: z. B. `Obstacle`, `OutOfBounds`, `EmptyRepeat`, `ProgramFull`, `NestingTooDeep`; Werte: Baustein-Pfad, Hinderniss-Art, Anzahl). `GameSession.Message` wird beim Anwenden eines Schritts bzw. Hinweises berechnet. Die UI-Komponente `MessageBox` übersetzt nur `Code` → resx-Schlüssel und setzt Pfad (`string.Join(".", path)`) und Heldenname ein. + xUnit-testbar, Sprachwechsel übersetzt sofort (FR23). − Ein Typ mehr.
- **B:** `MessageBox` leitet alles aus `Status` und `CurrentStep` ab. − Ableitungslogik (z. B. Pfad suchen, Hinweise) in der UI.

### D5 – Auswahl und Drag-Zustand
- **Auswahl (Empfehlung):** In `GameSession` (`SelectedId`, `Select(id)`, abgeleitet `InsertTarget` als `BlockSlot`), weil `BlockPalette` und `ProgramEditor` sie teilen und FR12/FR13 Regeln sind. Beginn eines Laufs hebt sie auf.
- **Drag-Zustand (Empfehlung):** Scoped Dienst `Game.UI/Services/ProgramDragState` (gezogener Palette-Baustein oder Programm-Id). Reiner UI-Zustand, die Gültigkeit eines Ziels fragt er bei `ProgramEditing.CanInsert/CanMove` ab. Laut `blazor-ui`-Skill: «Keep the currently dragged block in the component state (C#), not in `DataTransfer`». Ein Dienst statt Komponentenzustand, weil Palette (Quelle und Lösch-Ziel) und Editor (Quelle und Ziel) getrennte Komponenten sind.
- Alternative: Drag-Zustand ebenfalls in `GameSession`. − Vermischt UI-Interaktion mit Spielzustand.

### D6 – Native Drag & Drop in Blazor
- Blazor: `@ondragover:preventDefault="ausdruck"` ist erlaubt, damit nur gültige Zielzonen den Drop annehmen ([Microsoft Learn: Blazor event handling, «Prevent default actions»](https://learn.microsoft.com/aspnet/core/blazor/components/event-handling?view=aspnetcore-10.0)). Weil der gezogene Baustein ab `dragstart` im C#-Zustand liegt und neu gerendert wird, ist der Wert pro Zone beim `dragover` bekannt. `DataTransfer` ist in Blazor nur ein Abbild, `setData`/`dropEffect` sind ohne JS nicht setzbar; Microsoft empfiehlt für Drag & Drop JS-Interop (gleiche Quelle, Tabelle der Ereignisargumente).
- Ungültige Ziele zeigt die UI selbst (CSS-Klasse mit «Nein»-Symbol während `dragenter`/`dragover`), nicht über `dropEffect`. MDN: Ein Element ist nur Drop-Ziel, wenn `dragover` abgebrochen wird ([MDN: Drag operations](https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API/Drag_operations)).
- **Firefox und `setData` (nicht verifiziert):** Mehrere Quellen berichten, dass Firefox einen Drag nur startet, wenn in `dragstart` Daten gesetzt werden. MDN bestätigt das auf der geprüften Seite nicht. Der Firmenbrowser der Laptops ist unbekannt (`Projekt_Kontext.md` §9: «Test mit Firmenbrowser vorab»).
  - **A (Empfehlung, freigegeben):** Nur Blazor-Ereignisse, keine eigene JS-Datei. E2E läuft in Chromium (Edge/Chrome). Firefox wird beim Probelauf (M6) manuell geprüft; die Klick-Alternative funktioniert dort in jedem Fall. + Kein JS, MAUI-neutral.
  - **B:** Eigenes kleines JS-Modul in `Game.UI/wwwroot` (`setData('text/plain','')` per Listener), geladen über `IJSRuntime`. + Firefox sicher. − Erster eigener JS-Code im Modul, braucht Tests und Lebenszyklus.
- Playwright: `DragToAsync` bzw. `Hover → Mouse.Down → Hover (2×) → Mouse.Up`, weil `dragover` mindestens zwei Mausbewegungen braucht ([Playwright .NET: Input, Drag and drop](https://playwright.dev/dotnet/docs/input)).

### D7 – Layout der Programmliste
Einspaltig, Zeilenhöhe 2.75 rem (44 px), Einrückung pro Ebene, Wiederhole-Rumpf mit linker Leiste. ▲▼🗑 nur am ausgewählten Baustein (freigegeben, FR11). Die Liste scrollt innerhalb der Seitenleiste. Ziel: ca. 8 Zeilen auf 1366×768 (NFR4). Prüfung über Screenshot mit 10 Bausteinen.

## Slices

| # | Name | Ziel | Skills | Abhängig von |
|---|---|---|---|---|
| S1 | Wiederhole per Klick | Programm als Baum: Wiederhole einfügen, −/+, Auswahl, Einfügeziel, ▲▼🗑, Nummern 3.2.1, einspaltige Liste | architecture, dotnet10, game-rules, blazor-ui, localization, e2e-playwright | – |
| S2 | Lauf mit Wiederhole | Rundenzähler im Interpreter, Hervorhebung des Wiederhole-Blocks mit «Runde X von N» | dotnet10, game-rules, blazor-ui, localization, e2e-playwright | S1 |
| S3 | Meldungen | `GameMessage`, `MessageBox`, alle Texte DE/EN, Vorprüfung leerer Wiederhole, Hinweise voll/leer/Verschachtelung | dotnet10, game-rules, blazor-ui, localization, svg-assets, e2e-playwright | S1, S2 (Pfad, Runden) |
| S4 | Drag & Drop | Ziehen aus Palette, Umsortieren, Herausziehen löscht, ungültige Ziele | architecture, dotnet10, blazor-ui, e2e-playwright | S1 (Editier-API), S3 (Hinweis Verschachtelung) |

Reihenfolge folgt dem Risiko-Hinweis in `Projekt_Kontext.md` §9 und dem `blazor-ui`-Skill: erst Baum mit Klick, dann Drag & Drop.

### S1 – Wiederhole per Klick
- **Dateien:** `Game/Domain/ProgramEditing.cs`, `BlockSlot.cs`, `ProgramDefinition.cs` (`PathOf`, `CountAll`, `DepthAt`), `CommandNesting.cs`; `Game/Application/GameSession.cs` (Programm als `ProgramDefinition`, `Add(BlockKind)` mit Einfügeziel, `AddRepeat`, `Remove`, `MoveUp/MoveDown`, `SetRepeatCount`, `Select`, `SelectedId`, `InsertTarget`, `CanAddRepeat`), `ExecutionRunner.cs` (leere Programm-Prüfung über Baum); `Game.UI/Components/BlockPalette.*` (Wiederhole-Knopf `palette-repeat`), `ProgramEditor.*`, neu `ProgramBlock.razor(.cs/.css)` (rekursiv), `Resources/GameResources(.en).resx`.
- **Test-Ids:** `program-block` mit `data-path` (`3.2`), `data-kind` (`move|repeat`), `data-selected`; `program-select-{path}` (Klickfläche), `program-up`, `program-down`, `program-remove` (nur am ausgewählten), `repeat-count-{path}`, `repeat-minus-{path}`, `repeat-plus-{path}`, `repeat-body-{path}`, `insert-target` (Zeile «Neue Bausteine kommen in …»). Bestehende Ids `program-remove-{index}` entfallen (E2E/bUnit anpassen).
- **Tests:** xUnit `ProgramEditingTests` (Einfügen oben/Rumpf/verschachtelt, Tiefe 3 abgelehnt, 200 voll, Verschieben ▲▼ inkl. Rumpf verlassen und gesperrt, Wiederhole nicht in eigenen Rumpf, Entfernen mit Rumpf, Anzahl 1/20 Grenzen, Ids bleiben), `ProgramDefinitionTests` (`PathOf`), `GameSessionTests` (Auswahl, Einfügeziel, Sperre während Lauf, `Changed` nur bei echter Änderung); bUnit `ProgramEditorTests`, `BlockPaletteTests`.
- **E2E:** `RepeatEditingTests.ClickOnly_BuildsNestedProgram` (Flow 2). Screenshots `program.repeat.{desktop,laptop}.{de,en}`, `program.ten-blocks.laptop.de` (NFR4).
- **Akzeptanz:** FR1–FR5, FR11–FR15 (ohne Hinweistexte aus S3), FR4 Nummern.

### S2 – Lauf mit Wiederhole
- **Dateien:** `Game/Domain/ExecutionStep.cs`, neu `RepeatProgress.cs`, `Interpreter.cs` (Frame mit Repeat-Id, `Repeats` pro Schritt); `GameSession` (Auswahl bei Laufbeginn aufheben); `ProgramBlock` (`data-active-repeat`, «Runde X von N»), `GameSession.MarkedCellOf` (Richtung im Baum suchen), Resx `Repeat_Round`.
- **Tests:** xUnit `InterpreterRepeatProgressTests` (einfach, verschachtelt, Fehler im Rumpf, Schrittlimit, Play = Debug), bUnit Hervorhebung.
- **E2E:** `RepeatRunTests.Debug_ShowsRound_ThenPlaySucceeds` (Flow 3). Screenshots `run.repeat-round.{desktop,laptop}.{de,en}`.
- **Akzeptanz:** FR16–FR18.

### S3 – Meldungen
- **Dateien:** `Game/Application/GameMessage.cs` (+ `GameMessageKind`, `GameMessageCode`), `GameSession` (`Message`, Hinweis setzen/löschen, `HintedId`), `ExecutionRunner` (Vorprüfung D3); `Game.UI/Components/MessageBox.razor(.cs/.css)` unter dem Status, Resx (alle Texte FR20–FR22, Einzahl/Mehrzahl über eigene Schlüssel); Leer-Text ersetzen; ggf. Symbole in `Shared.UI/Graphics`.
- **Test-Ids:** `message` mit `data-kind` (`error|success|hint`) und `data-code`.
- **Tests:** xUnit `GameMessageTests` (jede Fehlerart, Erfolg, Hinweise, Reset löscht, Bearbeitung löscht Hinweis), `ExecutionRunnerTests` (leerer Wiederhole: Status `Ready`, kein Schritt); bUnit `MessageBoxTests` (alle Codes DE/EN, Einzahl/Mehrzahl, Sprachwechsel).
- **E2E:** `MessageTests` (Flow 4): Baum, Rand, Ziel nicht erreicht, leerer Wiederhole bleibt bearbeitbar, Verschachtelung abgelehnt. Screenshots `message.{obstacle,edge,goal-not-reached,empty-repeat,success}.{desktop,laptop}.{de,en}`.
- **Akzeptanz:** FR19–FR23.

### S4 – Drag & Drop
- **Dateien:** `Game.UI/Services/ProgramDragState.cs` (Registrierung in `AddGameModule`), `BlockPalette` (`draggable`, Lösch-Ziel), `ProgramEditor`/`ProgramBlock` (`draggable`, Drop-Zonen zwischen Bausteinen, am Ende, im Rumpf; `@ondragover:preventDefault="IsValid(zone)"`, Klassen für Einfüge-Linie und «Nein»), Domain: `ProgramEditing.CanMove/Move` (aus S1) wiederverwenden; Resx für `aria-label` der Zonen.
- **Test-Ids:** `drop-zone` mit `data-slot` (`root:0`, `3.2:1`) und `data-valid`.
- **Tests:** bUnit (Ereignisse `dragstart`/`drop` per `TriggerEvent`, ungültige Zone ändert nichts, gesperrt während Lauf); xUnit für `ProgramDragState`, falls Logik darin.
- **E2E:** `DragDropTests` (Flow 1): Palette → Position, Umsortieren in/aus Wiederhole, Herausziehen löscht. Screenshots `drag.insert-line.desktop.{de,en}`, `drag.invalid.desktop.de`.
- **Akzeptanz:** FR6–FR10.

## Befehle

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx
HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E
dotnet format HeroPath.slnx --verify-no-changes
```
