# Plan: M5 – Level-Editor und Speicherung

**Date**: 2026-11-03 · Grundlage: `docs/requirements.md` (freigegeben) · Vorgänger: `plan-M4.md` · Freigegeben mit D1–D12 wie empfohlen (Variante A), Reihenfolge S1 → S2 → S3 → S4 → S5

## Modul-Überblick

| Projekt | Änderung |
|---|---|
| `HeroPath.Shared` | `IAppStorage.SetAsync` liefert `bool` (D2); `StorageKeys.CustomLevels`; neuer Vertrag `IFileDownload` (D7); `TextCleaner` für Namen (D5) |
| `HeroPath.Shared.UI` | `ConfirmDialog` (Rückfragen, NFR8); Texte für «Ja/Abbrechen» |
| `HeroPath.Modules.Levels` | Domain: `LevelSolver` (aus den Tests), `LevelLimits.MaxStrawberries/MaxPizzas`, `EditorTool`, `LevelEditing` (Werkzeug anwenden, Grösse ändern, leeren, neues Level, Vorlage); Application: `LevelValidator` (Item-Grenze, D4), `LevelCheck` (gültig + lösbar + ≤ 1000 Schritte), `LevelRepository` (Standard + eigene, Laden, Speichern, Löschen, Import, `Changed`), `Contracts/ICustomLevelStore`, `LevelEditorState` (Entwurf, Werkzeug, ungespeichert, Hinweis); Infrastructure: `StorageCustomLevelStore` (eine JSON-Liste, D1, D8) |
| `HeroPath.Modules.Levels.UI` | `GameBoard` mit klickbaren Feldern (D9); `LevelEditor` (Werkzeuge, Grösse, Name, Schalter, Prüf-Anzeige, Speichern, Meine Levels, Export/Import); `LevelPicker` mit Gruppe «Meine Levels» und `Name`; `AddLevelsModule` registriert die Dienste; `LevelsResources` |
| `HeroPath.Modules.Game` | `LevelProgress` liest aus `LevelRepository` (Standard für «Nächstes»/«alle geschafft») |
| `HeroPath.Modules.Game.UI` | `EditorPage` (`/editor`, `/editor/{LevelId}`) mit `LevelEditor` und «Ausprobieren» (D3); `StartPage` «Level bauen» und Abschnitt «Meine Levels»; `PlayPage` «Bearbeiten», Level-Name für eigene Levels |
| `HeroPath.Web` | `LocalStorageAppStorage` meldet Erfolg; `BrowserFileDownload` + `wwwroot/js/file-download.js` (D7); `Program.cs` lädt eigene Levels vor dem ersten Render |
| Tests | Levels-Tests (Solver, Editing, Validator, Repository, Store, Editor-State, bUnit Editor/Board/Picker); Game-Tests (LevelProgress, Start/Play/EditorPage); Shared-Tests (ConfirmDialog, TextCleaner); `HeroPath.Testing` (`InMemoryAppStorage` mit Fehlermodus, `FakeFileDownload`); E2E `EditorTests`, `CustomLevelTests`, `LevelFileTests` |

Abhängigkeiten bleiben `Shared ← Levels ← Game`. Kein neues Modul, kein neues NuGet-Paket.

## Entscheide (zur Freigabe, jeweils mit Empfehlung)

**D1 – Speicheraufbau**
- **A (empfohlen):** Ein Schlüssel `heropath.levels` mit einer JSON-Liste von Level-Objekten. Beim Laden wird jedes Element einzeln mit `LevelJson.Read` geprüft (`JsonDocument`, Element-Text). Vorteile: Speichern und Löschen ist ein einziger Schreibvorgang, also nie ein halber Stand (Edge Case «Speicher voll»); Reihenfolge der Levels ist mitgespeichert. Nachteil: Jedes Speichern schreibt alle Levels (höchstens 20 × 64 KB ≈ 1,3 MB, typisch 20 × 2 KB).
- B: Ein Schlüssel pro Level (`heropath.level.<id>`) plus Index-Schlüssel. Vorteil: kleine Schreibvorgänge. Nachteil: zwei Schreibvorgänge pro Speichern, Index und Inhalt können auseinanderlaufen.
- Grenze beim Lesen der Liste: höchstens 21 × 64 KB Zeichen, sonst gilt der Eintrag als kaputt (D8).

**D2 – Speicherfehler erkennen (FR18)**
- **A (empfohlen):** `IAppStorage.SetAsync` und `RemoveAsync` liefern `ValueTask<bool>` (`true` = geschrieben). `LocalStorageAppStorage` gibt bei abgefangenem Fehler `false` zurück (heute `default`). `localStorage.setItem` wirft bei vollem Speicher `QuotaExceededError`, das kommt als `JSException` an (MDN: https://developer.mozilla.org/docs/Web/API/Storage/setItem#exceptions). `LanguageService` ignoriert das Ergebnis. Anpassen: `InMemoryAppStorage`, `ThrowingAppStorage`, `LanguageServiceTests`.
- B: Vertrag bleibt, nach dem Schreiben zurücklesen und vergleichen. Kein Vertragsbruch, aber ein Aufruf mehr und bei 1 MB ein grosser Vergleich.

**D3 – Wo liegt die Editor-Seite («Ausprobieren» braucht `HeroChoice` aus Game)**
- **A (empfohlen):** Die Bearbeitung liegt als Komponente `LevelEditor` in `Levels.UI` (wie in `Architektur C4.md` §4). Die routbare Seite `EditorPage` (`/editor`, `/editor/{LevelId}`) liegt in `Game.UI`. Sie bettet `LevelEditor` ein und reicht `EventCallback`s für «Ausprobieren» und «Zurück» hinein. Dort ruft sie `HeroChoice.SelectLevel` und navigiert zu `play`. Ohne gewählten Helden navigiert sie zur Startseite mit vorgewähltem Level (neu: `HeroChoice.Preselect(levelId)`). Die Regel `Levels` kennt `Game` nicht bleibt erhalten.
- B: Seite in `Levels.UI`, «Ausprobieren» navigiert zu `/play?level=<id>`. `PlayPage` liest die Query. Nachteil: M4 hat «Level in der URL» bewusst verschoben, und der Held fehlt nach Neuladen trotzdem.

**D4 – Item-Grenze (FR11)**
- **A (empfohlen):** `LevelLimits.MaxStrawberries = 10`, `MaxPizzas = 10` gelten im `LevelValidator` für alle Levels (neue Fehlerarten `TooManyStrawberries`, `TooManyPizzas`). Damit ist auch der Löser für jedes gültige Level schnell (2^10 Masken). Die Standard-Level haben höchstens 3 Erdbeeren (`04-strawberries.json`), `StandardLevelsTests` bleibt grün.
- B: Grenze nur im Editor. Nachteil: Import und Speicher könnten Levels mit 30 Erdbeeren liefern, der Löser bräuchte eine eigene Sicherung.

**D5 – Namensbereinigung (FR9)**
- **A (empfohlen):** Die Bereinigung aus `HeroChoice.NormalizeName` (Steuer- und Formatzeichen, Rand-Leerraum, Kürzen nach Textelementen) zieht als `TextCleaner.Clean(raw, maxTextElements)` nach `HeroPath.Shared` (reines C#, kein Plattformbezug). `HeroChoice` und `LevelEditing` nutzen sie. `HeroChoiceTests` bleiben unverändert grün.
- B: Kopie in `Levels`. Nachteil: zwei Stellen mit derselben Regel.
- Hinweis: `LevelValidator.IsValidName` zählt `string.Length` (UTF-16), `TextCleaner` Textelemente. Der Editor schneidet zusätzlich auf 50 UTF-16-Zeichen, damit der Validator nie widerspricht.

**D6 – Rückfrage beim Verlassen (FR20)**
- **A (empfohlen):** `NavigationLock` mit `OnBeforeInternalNavigation`. Bei ungespeicherten Änderungen: `context.PreventNavigation()`, Ziel merken, `ConfirmDialog` zeigen. Bei «Weggehen» ein Flag setzen und `NavigateTo(ziel)`. Quelle: Microsoft Learn, «ASP.NET Core Blazor navigation», Abschnitt «Handle/prevent location changes» und «NavigationLock component» (https://learn.microsoft.com/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0). Dort steht, dass sich überlappende Navigationen gegenseitig abbrechen («the latest request always cancels earlier requests»). Deshalb wird nicht im Handler auf den Dialog gewartet. `ConfirmExternalNavigation` bleibt `false` (Neuladen ungeschützt, FR20).
- B: Im Handler auf den Dialog warten (`TaskCompletionSource`). Nachteil: Ein zweiter Klick bricht den ersten Handler ab, das Verhalten ist unverifiziert.

**D7 – Export (FR26)**
- **A (empfohlen):** Vertrag `IFileDownload.SaveTextAsync(fileName, content, ct) → ValueTask<bool>` in `Shared/Application/Contracts`, allgemein statt `ILevelFileService`, weil `Shared` keine Levels kennt. Web: `BrowserFileDownload` lädt das JS-Modul `wwwroot/js/file-download.js` per `import`, Muster `downloadFileFromStream` mit `DotNetStreamReference`, `Blob`, Anker-Klick und `URL.revokeObjectURL`. Quelle: Microsoft Learn, «ASP.NET Core Blazor file downloads», Abschnitt «Download from a stream» (https://learn.microsoft.com/aspnet/core/blazor/file-downloads?view=aspnetcore-10.0). Der Code wird selbst geschrieben, nicht kopiert. `C4 §4.3` wird von `ILevelFileService` auf `IFileDownload` angepasst.
- B: Globale Funktion per `<script>` in `index.html`. Nachteil: `index.html` wächst, ein MAUI-Host bräuchte dieselbe Datei.

**D8 – Import (FR27)**
- **A (empfohlen):** Blazors `InputFile` (`Microsoft.AspNetCore.Components.Forms`, kein eigenes JS) mit `accept=".json,application/json"`, `OpenReadStream(maxAllowedSize: 256 KB)`. Zu gross wirft eine Exception, die zur Meldung «Datei zu gross» wird. Danach greift die Grenze `LevelJson.MaxJsonLength`. Dateiname und Inhalt gelten als nicht vertrauenswürdig (Microsoft Learn, «ASP.NET Core Blazor file uploads», «Security considerations», https://learn.microsoft.com/aspnet/core/blazor/file-uploads?view=aspnetcore-10.0). Der Dateiname wird nicht angezeigt und nicht verwendet. Ob `InputFile` im MAUI-WebView geht, steht dort nicht (unverifiziert, M7).
- Kaputte Einträge im Speicher: beim Laden übergehen. Beim nächsten Speichern fallen sie weg, weil die gültige Liste geschrieben wird. Ist der ganze Schlüssel kein JSON, gilt «keine eigenen Levels». Das nächste Speichern ersetzt ihn.

**D9 – Spielfeld im Editor**
- **A (empfohlen):** `GameBoard` bekommt `EventCallback<Position> CellClicked` und einen `ariaLabel`-Text pro Feld. Ist er gesetzt, ist jede Zelle ein `<button>` mit `data-testid="edit-cell-{x}-{y}"`, ohne Held. So gibt es nur eine Darstellung des Felds, Vorschau, Spiel und Editor sehen gleich aus.
- B: Neues `EditorBoard`. Nachteil: Zellen-Markup und CSS doppelt.
- Zellgrösse: so gross wie Platz ist, höchstens 44 px. Bei 20×20 auf 1366×768 werden die Zellen etwa 30 px gross. Das ist die Grenze für NFR7, 44 px gilt für alle anderen Knöpfe. Die Visual-Prüfung bestätigt die Klickbarkeit.

**D10 – Id und Name**
- Eigene Ids: `custom-` plus 12 Hex-Zeichen aus `RandomNumberGenerator` (passt zu `^[a-z0-9-]{1,40}$`), bei Kollision neu ziehen. Import und Vorlage bekommen immer eine neue Id.
- «Mein Level N»: `LevelEditing.NextDefaultNumber(names, format)` mit dem lokalisierten Format aus resx (`Editor_DefaultName` = «Mein Level {0}» / «My level {0}»). N ist die kleinste Zahl ≥ 1, deren Text kein eigenes Level trägt.

**D11 – Löser und Leistung (FR13, NFR2)**
- `LevelSolver.ShortestSolution(Grid) → int?` in `Levels/Domain`, BFS über (Feld, Erdbeer-Maske) mit `int[]`-Abständen statt `Dictionary` (höchstens 400 × 1024 Zustände ≈ 1,6 MB). Die Tests nutzen danach diesen Löser, `tests/…/LevelSolver.cs` fällt weg.
- Messung: xUnit-Test mit dem schwersten Fall (20×20, 10 Erdbeeren, `RequireAllItems`). E2E `EditorTests` setzt die 10. Erdbeere auf 20×20 und misst die Zeit bis zur Anzeige «mindestens N Schritte». Die Zeit wird in die Testausgabe geschrieben, geprüft wird ≤ 1 s, Ziel ≤ 200 ms. Bleibt WebAssembly darüber, wird die Prüfung entprellt (Vorschlag dann im Review).

**D12 – Laden beim Start**
- `Program.cs` ruft nach `ApplyStoredCultureAsync` `await host.Services.LoadCustomLevelsAsync()` (neue Erweiterung in `Levels.UI`, gleiches Muster wie `LanguageServiceExtensions`). Damit kennt die Startseite die eigenen Levels schon beim ersten Render. `LevelRepository` ist scoped wie `LanguageService`.

## Slices

| # | Name | Ziel | Hängt ab von |
|---|---|---|---|
| S1 | Editor-Regeln und Löser | Domain/Application ohne UI: Löser, Item-Grenze, `LevelEditing`, `LevelCheck`, `TextCleaner` | – |
| S2 | Eigene Levels speichern und spielen | `IAppStorage` mit Ergebnis, `LevelRepository` + Store, Laden beim Start, Startseite «Meine Levels», LevelPicker-Gruppe, Haken | S1 |
| S3 | Editor-Seite | `/editor`: Werkzeuge, Klick-Feld, Grösse, Name, Schalter, Prüf-Anzeige, Speichern mit Grenzen und Fehlern, Liste «Meine Levels», «Level bauen» | S2 |
| S4 | Verwalten und Wechseln | `ConfirmDialog`, Vorlage, Löschen, Alles leeren, Rückfrage beim Verlassen, «Ausprobieren», «Bearbeiten» | S3 |
| S5 | Export und Import | `IFileDownload` + Web-JS, `InputFile`-Import mit voller Prüfung | S3 (S4 nicht nötig) |

### S1 – Editor-Regeln und Löser
- **Dateien:** `Shared/Application/TextCleaner.cs`; `Game/Application/HeroChoice.cs` nutzt ihn; `Levels/Domain/LevelSolver.cs`, `EditorTool.cs` (Start, Goal, Tree, Rock, Pond, Strawberry, Pizza, Eraser), `LevelEditing.cs` (`NewLevel`, `FromTemplate`, `Apply(level, tool, position) → EditResult` mit Hinweis-Code, `Resize(level, w, h) → (level, removedCount)`, `ClearObjects`, `NextDefaultNumber`, `CleanName`), `LevelLimits` (Max-Items, `MaxCustomLevels = 20`); `Levels/Application/LevelValidator.cs` (D4), `LevelValidationErrorKind`, `LevelCheck.cs` (`Evaluate(level) → LevelCheckResult`: Fehler, `Unsolvable`, `MissingStrawberries`, `TooManySteps`, `MinimumSteps`); `tests/…/Infrastructure/LevelSolver.cs` entfernen, `StandardLevelsTests` auf Domain-Löser.
- **Tests:** xUnit `LevelSolverTests` (frei, eingemauert, Erdbeere eingemauert mit/ohne Pflicht, Pizza zählt nicht, schwerster Fall schnell), `LevelEditingTests` (alle Werkzeugregeln aus FR6, Ersetzen, Entfernen per gleichem Werkzeug, Start auf Ziel verboten, Item auf Ziel erlaubt, Grenze 10, Resize mit Rücken/Entfernen/Zusammenfallen bis 3×3, Schalter aus bei letzter Erdbeere, Vorlage behält Original, `NextDefaultNumber` mit Lücken), `LevelValidatorTests` (neue Fehler), `LevelCheckTests`, `TextCleanerTests`; `HeroChoiceTests` unverändert grün.
- **Skills:** `architecture`, `dotnet10`, `game-rules`, `level-design`.
- **Abnahme:** FR6, FR8–FR11, FR13–FR15 als Regeln, NFR1, NFR2 (xUnit-Teil); keine sichtbare Änderung.

### S2 – Eigene Levels speichern und spielen
- **Dateien:** `Shared/Application/Contracts/IAppStorage.cs` (D2), `StorageKeys.CustomLevels`; `Hosts/…/LocalStorageAppStorage.cs`; `Shared.UI/Localization/LanguageService.cs`; `Levels/Application/Contracts/ICustomLevelStore.cs`, `Levels/Application/LevelRepository.cs` (`Standard`, `Custom`, `All`, `Find`, `IsCustom`, `LoadAsync`, `SaveAsync → SaveOutcome` {Saved, LimitReached, Invalid, StorageFailed}, `DeleteAsync`, `ImportAsync(json)`, `Changed`); `Levels/Infrastructure/StorageCustomLevelStore.cs` (D1, D8); `Levels.UI/LevelsModuleExtensions.cs` (Registrierung, `LoadCustomLevelsAsync`); `Game/Application/LevelProgress.cs` (aus Repository, `NextAfter`/`AllSolved` nur Standard); `GameModuleExtensions`; `Program.cs` (D12); `StartPage` Abschnitt «Meine Levels» (nur wenn vorhanden, Name aus `LevelDefinition.Name`); `LevelPicker` (`<optgroup>` «Meine Levels», Name-Rückfall); `PlayPage` Level-Name; resx DE/EN; `HeroPath.Testing/InMemoryAppStorage` (`FailWrites`).
- **Test-Ids:** `custom-levels` (Abschnitt), `level-card-{id}` wie bisher, `level-picker-group-custom`.
- **Tests:** xUnit `LevelRepositoryTests` (Laden leer/gültig/kaputt/zu gross/manipuliert, Speichern neu/überschreiben, 21. abgelehnt, Speicherfehler → `StorageFailed` und Speicherstand unverändert, Löschen, Import neue Id, `Changed`), `StorageCustomLevelStoreTests`, `LevelProgressTests` (eigene Levels, «Nächstes» nur Standard, `AllSolved` nur Standard), `LanguageServiceTests` angepasst; bUnit `LevelPickerTests` (Gruppe, Name), `StartPageTests` (Abschnitt nur mit eigenen Levels); E2E `CustomLevelTests`: localStorage vor dem Start mit einem Level füllen (`AddInitScriptAsync`), Karte unter «Meine Levels», Level spielen und lösen, Haken, Neuladen → Level noch da, kaputter Eintrag daneben → kein Absturz (Screenshots `start-custom-levels`, `play-custom-level`).
- **Skills:** `architecture`, `dotnet10`, `blazor-ui`, `localization`, `e2e-playwright`, `game-rules`.
- **Abnahme:** FR21, FR23, FR24, NFR4, Edge Cases Speicher.

### S3 – Editor-Seite
- **Dateien:** `Levels/Application/LevelEditorState.cs` (scoped: Entwurf, Id oder neu, Werkzeug, `IsDirty`, letzter Hinweis, `Check`, `OpenNew`, `Open(id)`, `Apply`, `Resize`, `SetName`, `SetRequireAll`, `SaveAsync`); `Levels.UI/Components/GameBoard` (D9); `Levels.UI/Components/LevelEditor.razor(.cs,.css)` mit Teilkomponenten `EditorToolbar`, `EditorSizeControl`, `EditorStatus`, `CustomLevelList`; `Game.UI/Pages/EditorPage.razor(.cs,.css)` (`/editor`, `/editor/{LevelId}`, D3); `StartPage` Knopf «Level bauen»; resx DE/EN (Werkzeuge, Hinweise, Datenschutz-Hinweis, Mindestschritte Einzahl/Mehrzahl, Fehler).
- **Test-Ids:** `editor-page`, `editor-tool-{tool}` (`aria-pressed`), `edit-cell-{x}-{y}` (`data-kind`, `data-item`), `editor-width`, `editor-height` (+ `-minus`/`-plus`), `editor-name`, `editor-name-hint`, `editor-require-all`, `editor-status` (`data-state` = `solvable|unsolvable|invalid`), `editor-hint`, `editor-save`, `editor-saved`, `custom-level-list`, `custom-level-{id}`, `start-build`.
- **Tests:** xUnit `LevelEditorStateTests` (Dirty-Flag, Speichern setzt zurück, gesperrt bei unlösbar, Grenze 20, Speicherfehler behält Entwurf, Sprachwechsel hat keinen Einfluss); bUnit `LevelEditorTests` (Werkzeug wählen, Klick setzt, Hinweise, Name leer sperrt, Schalter deaktiviert ohne Erdbeere, Status-Texte DE/EN), `GameBoardTests` (klickbar nur mit `CellClicked`), `EditorPageTests`; E2E `EditorTests`: Ablauf 1 (Startseite → «Level bauen» → Hindernisse, Erdbeeren, Mindestschritte, Speichern → Karte unter «Meine Levels» → spielen), Ablauf 3 (Ziel einmauern → gesperrt mit Hinweis; verkleinern mit Hinweis; 11. Erdbeere abgelehnt; 20×20-Messung D11) (Screenshots `editor-new`, `editor-built`, `editor-unsolvable`, `editor-resized`, `editor-20x20`, je DE/EN).
- **Skills:** `architecture`, `blazor-ui`, `localization`, `svg-assets` (Werkzeug-Icons aus bestehenden Grafiken, Radiergummi neu), `e2e-playwright`, `game-rules`.
- **Abnahme:** FR1, FR2, FR4 (Liste), FR5–FR11, FR13–FR18, NFR3, NFR5–NFR7, NFR9.

### S4 – Verwalten und Wechseln
- **Dateien:** `Shared.UI/Components/ConfirmDialog.razor(.cs,.css)` (`role="dialog"`, `aria-modal`, Fokus auf erste Schaltfläche per `ElementReference.FocusAsync`, Escape = Abbrechen); `LevelEditor` (Vorlage wählen, Löschen, Alles leeren, `NavigationLock` nach D6, «Ausprobieren»); `EditorPage` («Ausprobieren» → `HeroChoice`, D3); `HeroChoice.Preselect`; `StartPage` übernimmt Vorwahl; `PlayPage` Knopf «Bearbeiten» bei eigenen Levels; Löschen eines gewählten Levels setzt `HeroChoice` auf das erste Standard-Level; resx DE/EN.
- **Test-Ids:** `confirm-dialog`, `confirm-yes`, `confirm-no`, `editor-template`, `editor-template-{id}`, `custom-level-delete-{id}`, `editor-clear`, `editor-try`, `play-edit`.
- **Tests:** xUnit `HeroChoiceTests` (Preselect), `LevelEditorStateTests` (Vorlage, Löschen des offenen Levels); bUnit `ConfirmDialogTests` (Tastatur, Escape), `LevelEditorTests` (Rückfrage erscheint, Abbrechen lässt alles), `PlayPageTests` («Bearbeiten» nur bei eigenen); E2E `EditorManageTests`: Ablauf 4 ohne Datei (Vorlage `easy-1` → ändern → speichern, Original unverändert; Löschen mit Rückfrage), Ablauf 5 («Ausprobieren» mit und ohne Held, «Bearbeiten» zurück, Rückfrage beim Wegklicken, Abbrechen bleibt) (Screenshots `editor-template`, `confirm-delete`, `confirm-leave`, `play-edit-button`).
- **Skills:** `blazor-ui`, `localization`, `e2e-playwright`, `architecture`.
- **Abnahme:** FR3, FR4, FR12, FR19, FR20, FR22, FR25, NFR8, Edge Case «gelöschtes Level gewählt».

### S5 – Export und Import
- **Dateien:** `Shared/Application/Contracts/IFileDownload.cs`; `Hosts/…/Services/BrowserFileDownload.cs`, `wwwroot/js/file-download.js`, Registrierung in `Program.cs`; `Levels/Domain/LevelEditing.ExportFileName(name)` (nur `[a-z0-9-]`, Umlaute ersetzt, leer → `level`, Endung `.heropath.json`); `LevelEditor`/`CustomLevelList` (Export-Knopf pro Level, `InputFile` nach D8, Meldungen); `HeroPath.Testing/FakeFileDownload`; resx DE/EN.
- **Test-Ids:** `custom-level-export-{id}`, `editor-import` (das `input type=file`), `editor-import-message`.
- **Tests:** xUnit `ExportFileNameTests`, `LevelRepositoryTests` (Import: ungültig, unlösbar, 11 Erdbeeren, Standard-Id, voll); bUnit `LevelEditorTests` (Export ruft `IFileDownload` mit JSON ohne Heldennamen, Import zeigt Meldung); E2E `LevelFileTests`: Export (`RunAndWaitForDownloadAsync`, Inhalt mit `LevelJson.Read` gültig), dieselbe Datei importieren (`SetInputFilesAsync`) → zweites Level mit gleichem Namen, ungültige Datei → Meldung (Screenshots `editor-import-ok`, `editor-import-error`).
- **Skills:** `architecture`, `blazor-ui`, `localization`, `e2e-playwright`, `release` (JS-Datei im publish-Ordner und unter GitHub-Pages-Pfad).
- **Abnahme:** FR26, FR27, NFR3, NFR4, Edge Cases Import.

## Befehle

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx
dotnet format HeroPath.slnx --verify-no-changes
HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E
```
