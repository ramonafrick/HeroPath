# Requirements: M2 – Blazor-Grundgerüst, Spielfeld, Figur, Play, Debug, Reset

**Date**: 2026-11-03 · Vorgänger: `requirements-M1.md` (Domain, umgesetzt)

## Problem

Die Spielregeln aus M1 (`Grid`, `Person`, `Interpreter`) sind getestet, aber noch nicht sichtbar. `PlayPage` ist leer. M2 macht das Spiel zum ersten Mal spielbar: Ein Kind sieht ein Spielfeld mit Held, Hindernissen, Start und Burg, stellt per Klick ein Programm aus Richtungs-Bausteinen zusammen und führt es mit Play (am Stück) oder Debug (Schritt für Schritt) aus. Reset setzt zurück. Damit werden die Lernziele Sequenz und Debugging erlebbar. M3 (Drag & Drop, Wiederhole, Meldungstexte) und M4 (Levels, Start-Dialog, Items) bauen auf dem Grundgerüst auf.

## Users & Context

- Kinder 11 bis 13 Jahre ohne Vorkenntnisse, begleitet von Software Engineers, am Zukunftstag (ca. 4 Stunden).
- Laptop mit externem Bildschirm (1920×1080), Maus und Tastatur. Zusätzlich Laptop-Bildschirm 1366×768 ohne externen Monitor. Kein Touch.
- Host: nur `HeroPath.Web` (Blazor WebAssembly im Browser). Kein MAUI.

## Functional Requirements

### Spielfeld und Figur
- FR1: Ein Demo-Level als eingebettete JSON-Ressource in `HeroPath.Modules.Levels` (10×10, 2 bis 3 Hindernisse, je mindestens ein Baum, Stein und Teich, keine Items, `requireAllItems = false`). Geladen über `LevelJson.Read` und einen kleinen Level-Dienst in `Modules.Levels/Application`, sodass M4 weitere Levels ergänzen kann. Das Level ist lösbar.
- FR2: `GameBoard` (Levels.UI) zeichnet das Raster mit CSS Grid: Startfeld (grün + Fussabdruck-Muster), Ziel (Burg auf goldenem Schachbrett), Hindernisse Baum/Stein/Teich.
- FR3: Der Held ist fest **Max** (Wahl Max/Lisa folgt in M4). Er steht zu Beginn auf dem Start und bewegt sich als eigene, absolut positionierte Ebene per CSS-Transition (keine Neuerzeugung pro Zelle).
- FR4: Alle Grafiken sind selbst gezeichnete Inline-SVG-Komponenten in `HeroPath.Shared.UI/Graphics` (Max, Start, Burg/Ziel, Baum, Stein, Teich) mit Palette in `theme.css` und lokalisiertem `<title>`.

### Programm per Klick
- FR5: `BlockPalette` mit den Bausteinen Hoch, Runter, Links, Rechts. Ein Klick hängt den Baustein ans Programmende an.
- FR6: `ProgramEditor` zeigt das Programm als nummerierte Liste (`@key` = `Command.Id`). Jeder Baustein hat einen Entfernen-Knopf, dazu gibt es «Alles löschen».
- FR7: Ab `ProgramLimits.MaxProgramCommands` (200) Bausteinen ist die Palette deaktiviert.

### Ausführung
- FR8: **Play** führt das Programm über den M1-`Interpreter` automatisch aus, mit Wartezeit gemäss Tempo zwischen den Schritten (`ExecutionRunner`, `TimeProvider`).
- FR9: **Debug** führt pro Klick genau einen `Step()` aus. Status danach «Pause» (sofern kein Endzustand).
- FR10: Nach Debug-Schritten macht **Play** ab der aktuellen Position weiter (gleicher Interpreter). Während Play ist Debug gesperrt. Es gibt keinen Pause-Knopf, nur Reset stoppt einen Lauf.
- FR11: **Reset** ist immer aktiv: bricht einen laufenden Play ab, erstellt einen neuen Interpreter, Figur zurück auf Start, Status «Bereit», Editor entsperrt. Das Programm bleibt erhalten.
- FR12: **Tempo** mit nativem `<input type="range">` in 3 Stufen: langsam 900 ms, mittel 500 ms (Standard), schnell 200 ms. Beschriftet in DE/EN. Die Bewegungsanimation passt zur Stufe. Tempo ist auch während Play änderbar.
- FR13: Palette, Entfernen und «Alles löschen» sind gesperrt, sobald ein Lauf begonnen hat (Läuft, Pause) und nach einem Endzustand (Fehler, Ziel erreicht), bis Reset gedrückt wird. Nach einem Endzustand sind Play und Debug gesperrt.
- FR14: Ist das Programm leer, sind Play und Debug gesperrt.

### Status und Hervorhebung
- FR15: Statusanzeige mit den Zuständen Bereit, Läuft, Pause, Fehler, Ziel erreicht (DE/EN). Sie trägt `data-testid="status"`, `data-state` (`ready`, `running`, `paused`, `failed`, `succeeded`) und bei Fehler `data-error` (Name der `ExecutionError`, z. B. `Obstacle`).
- FR16: Der Baustein der aktuellen `ExecutionStep.CommandId` wird im Programm hervorgehoben (bei Debug und Play). Bei Fehler werden Baustein und betroffenes Feld markiert (Hindernisfeld bzw. Randfeld, auf dem die Figur steht).
- FR17: Die kindgerechten Detailmeldungen pro Fehlerart («Autsch! …», «Hoppla! …», «Geschafft, Max!») folgen in M3. In M2 zeigt der Status nur den Zustand.

### Sprache und Grundgerüst
- FR18: `MainLayout` mit Kopfzeile: App-Titel («HeldenWeg» / «Hero's Path») und `LanguageSwitcher` (DE/EN) oben rechts.
- FR19: Umschalten der Sprache ohne Neuladen. Programm, Lauf-Zustand und Figur bleiben erhalten. Die Sprache wird über `IAppStorage` gespeichert (Web-Implementierung mit localStorage im Host) und beim Start wiederhergestellt. Standard und Rückfall bei unbekanntem Wert: `de-CH`.

## Non-Functional Requirements

- NFR1: Keine Spiellogik in Razor oder Code-Behind. `GameSession` und `ExecutionRunner` sind reines C# in `Modules.Game/Application`, ohne Blazor-Typen.
- NFR2: Alle sichtbaren Texte via resx (DE mit Schweizer Rechtschreibung, ohne «ß», und EN), im Modul, dem sie gehören.
- NFR3: Klickziele mindestens 44 px, echte `<button>`-Elemente, sichtbarer Fokus, `prefers-reduced-motion` schaltet Transitionen ab.
- NFR4: Responsives Layout ohne feste Pixelbreiten. Auf 1920×1080 und 1366×768 ist alles ohne Scrollen sichtbar (Spielfeld, Palette, Programm bis ca. 10 Bausteine, Steuerung, Status).
- NFR5: Start/Ziel und die drei Hindernisse sind auch in Graustufen unterscheidbar (Form/Muster).
- NFR6: Keine neuen NuGet-Pakete (Fluent UI kommt erst, wenn Dialog/Select gebraucht werden). Keine CDNs, keine fremden Grafiken.
- NFR7: Kein Heldenname im Speicher und in Logs (in M2 ohnehin fest «Max», nur angezeigt im `<title>` der Grafik).
- NFR8: `dotnet build`, `dotnet test --solution HeroPath.slnx`, `dotnet format --verify-no-changes` und die E2E-Tests (`HEROPATH_E2E=1`) sind grün.

## Technical Constraints

- Projekte:
  - `HeroPath.Modules.Levels`: Demo-Level-JSON als `EmbeddedResource` (Infrastructure), Level-Dienst (Application).
  - `HeroPath.Modules.Levels.UI`: `GameBoard`, Texte in `LevelsResources`.
  - `HeroPath.Modules.Game`: `Application/GameSession`, `Application/ExecutionRunner`.
  - `HeroPath.Modules.Game.UI`: `PlayPage`, `BlockPalette`, `ProgramEditor`, `ControlBar` (inkl. Tempo, Status), `GameResources`, Registrierung in `AddGameModule()`.
  - `HeroPath.Shared.UI`: SVG-Grafiken, `theme.css`, `MainLayout`-Kopfzeile, `LanguageSwitcher`, `LanguageService`, `SharedResources`.
  - `HeroPath.Shared`: ggf. Speicher-Schlüssel (`StorageKeys`).
  - `Hosts/HeroPath.Web`: `LocalStorageAppStorage`, Registrierung, Kultur beim Start setzen.
- Abhängigkeiten `Shared ← Levels ← Game` (gleich für `.UI`), `HeroPath.ArchitectureTests` und `ClaudeWorkflowTests` bleiben grün (neue öffentliche Typen der Module im `game-rules`-Skill nennen).
- Grenzwerte aus M1 gelten (`ProgramLimits`, `LevelLimits`). Ohne Wiederhole-Block ist das Schrittlimit (1000) mit 200 Bausteinen nicht erreichbar.
- Tests: xUnit für `GameSession`, `ExecutionRunner` (mit `FakeTimeProvider` bzw. eigenem Fake, keine echten Wartezeiten) und den Level-Dienst. bUnit für `BlockPalette`, `ProgramEditor`, `ControlBar`, `GameBoard`, `LanguageSwitcher`. Ein Test prüft, dass das Demo-Level gültig und lösbar ist.
- E2E-Flows (Playwright, Klick-Alternative, Screenshots normal + Graustufen, DE und EN, desktop 1920×1080 und laptop 1366×768):
  1. Play bis zur Burg → `data-state=succeeded`
  2. Debug Schritt für Schritt → Figur bewegt sich, Baustein hervorgehoben, `paused`, danach Play macht weiter bis zum Ende
  3. Fehler Hindernis und Rand → `failed` mit `data-error=Obstacle` bzw. `OutOfBounds`, Baustein und Feld markiert
  4. Reset + Bearbeiten → Figur zurück auf Start, Editor entsperrt, Baustein entfernen, «Alles löschen», Play/Debug gesperrt
  5. Sprachwechsel während Pause → Texte wechseln, Programm und Figurposition bleiben

## Acceptance Criteria

- [ ] Beim Öffnen von `/` erscheinen Spielfeld (Demo-Level), Max auf dem Start, Palette, leeres Programm, Steuerung (Play/Debug gesperrt), Tempo «mittel», Status «Bereit».
- [ ] Ein Klick auf einen Palette-Baustein hängt ihn an. Entfernen und «Alles löschen» funktionieren.
- [ ] Play mit einer Lösung führt zu «Ziel erreicht», Max steht auf der Burg.
- [ ] Debug bewegt Max pro Klick um genau ein Feld, der aktuelle Baustein ist hervorgehoben, Status «Pause». Play macht von dort weiter.
- [ ] Lauf in ein Hindernis bzw. aus dem Feld ergibt «Fehler» mit markiertem Baustein und Feld. Programm zu Ende ohne Burg ergibt «Fehler» (`GoalNotReached`).
- [ ] Während und nach einem Lauf sind die Bearbeitungsknöpfe gesperrt, nach einem Endzustand auch Play und Debug. Reset stellt den Anfangszustand mit unverändertem Programm her, auch mitten in Play.
- [ ] Tempo-Stufen wirken auf die Wartezeit (getestet mit Fake-TimeProvider).
- [ ] Sprachwechsel DE/EN ohne Reload, Programm und Lauf bleiben. Nach dem Neuladen ist die gewählte Sprache wieder aktiv.
- [ ] Alle 5 E2E-Flows grün, Screenshots in DE und EN, desktop und laptop, vom visual-checker ohne [MUST]-Befund geprüft.
- [ ] Build, Format, Unit-, bUnit- und Architekturtests grün.

## Edge Cases & Failure Modes

- Reset während Play → laufende Schleife bricht sofort ab (Abbruch-Token), kein weiterer Schritt wird danach noch angewendet.
- Mehrfaches schnelles Klicken auf Play → nur ein Lauf; Play ist ab dem ersten Klick gesperrt.
- Debug-Klicks nach Endzustand → nicht möglich (gesperrt).
- 200 Bausteine erreicht → Palette deaktiviert, keine Exception aus `ProgramDefinition`.
- localStorage blockiert oder voll → Sprache wird nicht gespeichert, das Spiel bleibt voll spielbar, keine Fehlermeldung an das Kind, kein Konsolenfehler, der den E2E-Test bricht.
- Gespeicherter Sprachwert ungültig/manipuliert → `de-CH`.
- Demo-Level-JSON ungültig (Programmierfehler) → Test schlägt fehl; zur Laufzeit klarer Fehler statt halb gerendertem Spielfeld.
- Darf nie passieren: Exception während eines Laufs erreicht die UI; Play und Debug liefern unterschiedliche Ergebnisse; Sprachwechsel verliert das Programm; Heldenname landet im Speicher.

## Out of Scope

- Drag & Drop, Wiederhole-Block, kindgerechte Meldungstexte pro Fehlerart (M3)
- Level-Auswahl, weitere Vorlagen, Start-Dialog (Held/Name), Lisa, Items Erdbeere/Pizza (M4)
- Level-Editor, Speichern eigener Levels (M5)
- Fluent UI Blazor (erst bei Dialog/Select, voraussichtlich M4)
- Pause-Knopf während Play, Schritt zurück, Konfetti/Erfolgsanimation, Sound, Tastaturkürzel
- Phone-Layout als Pflicht (Screenshot nur informativ), MAUI-Host

## Open Questions

- Keine offenen Fragen. Annahmen, die ich selbst getroffen habe (bitte bei der Freigabe korrigieren):
  - Layout: Kopfzeile oben, darunter links das Spielfeld, rechts Palette, Programm, Steuerung und Status.
  - Das Demo-Level hat die Id `demo` und ist keine der 7 Vorlagen. M4 darf es ersetzen oder als Vorlage übernehmen.
  - Bei `OutOfBounds` wird das Feld markiert, auf dem Max stehen bleibt (der Zug wird nicht ausgeführt).
  - Die M1-Dokumente liegen jetzt als `docs/requirements-M1.md` und `docs/plan-M1.md` vor. Der Verweis in `Projekt_Kontext.md` §7 ist nachgeführt.

## Umsetzungsstand und bekannte Einschränkungen

Stand nach M2 (Code in `src/` und `tests/`). Die Abschnitte oben beschreiben die Anforderung, dieser Abschnitt den tatsächlichen Stand. Die Reihenfolge der Entscheidungen mit Alternativen steht in `Architektur C4.md` §6 (Nr. 11 bis 14).

### Abweichungen, im Review entschieden

- **Sprachwechsel und Satellite-Assemblies (FR19):** Blazor WebAssembly lädt die Satellite-Assemblies (die `en`-resx) nur für die beim Start aktive Kultur. `Hosts/HeroPath.Web/wwwroot/index.html` startet Blazor deshalb manuell (`autostart="false"`, dann `Blazor.start({ configureRuntime: dotnet => dotnet.withConfig({ loadAllSatelliteResources: true }) })`), alle Sprachen werden beim Start geladen. Das Flag `loadAllSatelliteResources` liest der .NET-Runtime-Loader, es steht aber nicht in der öffentlichen `dotnet.d.ts`. Abgesichert ist es durch den E2E-Test `LanguageTests.Switch_ChangesTexts_AndPersists` (Wechsel ohne Neuladen). bUnit würde den Fehler nicht bemerken, weil es auf der .NET-Runtime läuft. Alternativen: `Architektur C4.md` §6, Nr. 11.
- **`GameSession.Load` (FR1, FR11):** `Load(Grid)` mit einem gleichen Grid (Wertgleichheit über `Grid.Equals`: Grösse, Start, Ziel, `RequireAllItems`, Hindernisse, Items) ändert nichts, Programm und Lauf bleiben. Ein anderes Grid verwirft den Lauf (Status `Ready`, Held auf dem neuen Start, entsperrt), das Programm bleibt. `PlayPage` ruft `Load` beim Öffnen auf.
- **Verlassen der Seite stoppt Play (nicht in den Anforderungen):** `PlayPage.Dispose` ruft `ExecutionRunner.Stop()`. Ein laufendes Play endet im Status `Paused` mit unverändertem Interpreter und Programm. Es wird nicht zurückgesetzt. Die UI hat weiterhin keinen Pause-Knopf (FR10), `Stop` wird nur hier verwendet. Alternativen: `Architektur C4.md` §6, Nr. 14.
- **Tempo in `GameSession`, Startregel im Runner:** Das Tempo (`Tempo`, Standard `Medium`) ist Zustand von `GameSession`; eine Änderung wirkt auf die nächste Wartezeit und löst `Changed` aus. Die Regel, ob Play oder Debug starten dürfen (Grid geladen, Programm nicht leer, Status `Ready` oder `Paused`), steht nur in `ExecutionRunner.CanStart`. `ControlBar` und `PlayAsync`/`StepOnce` verwenden dieselbe Eigenschaft. Reset ist immer aktiv (kein `disabled`).
- **Play-Schleife mit `try/finally`:** `PlayAsync` führt den ersten Schritt sofort aus und wartet danach `Tempo.Duration` (mit `TimeProvider`). Beendet eine Ausnahme die Schleife (z. B. aus einem `Changed`-Handler), wechselt der Status im `finally` von `Running` auf `Paused`, die Ausnahme wird weitergereicht. Bei Abbruch durch Reset oder Stop passiert das nicht doppelt (Vergleich der `CancellationTokenSource`).
- **Seitenleiste:** Reihenfolge Palette → Steuerung → Programm (`PlayPage.razor`), geändert gegenüber der Annahme «Palette, Programm, Steuerung». Der Entfernen-Knopf ist ein Papierkorb-Symbol (44-px-Ziel, `aria-label` «Baustein N entfernen»). Aktueller und fehlerhafter Baustein tragen ein Eck-Abzeichen (Kreis mit Pfeil bzw. Dreieck mit Ausrufezeichen) zusätzlich zu Rahmen und Muster, damit sie auch in Graustufen erkennbar sind.
- **`StandardLevels` statisch (FR1):** Der «kleine Level-Dienst» ist die statische Klasse `Modules.Levels/Infrastructure/StandardLevels` (`All`, `Get(id)`), die die eingebetteten `Infrastructure/Levels/*.json` über `LevelJson.Read` liest, nach Dateiname sortiert. Es gibt noch kein `ILevelSource` und kein `LevelRepository`; sie kommen mit M5. Das Demo-Level heisst `00-demo.json` (Id `demo`, Start (1,6), Ziel (6,3)). Ein ungültiges eingebettetes Level wirft beim ersten Zugriff. Alternativen: `Architektur C4.md` §6, Nr. 13.
- **Testinfrastruktur:**
  - Neues Testprojekt `tests/HeroPath.Shared.Tests` (Sprache, `LanguageSwitcher`, `LocalizedComponentBase`, Grafiken).
  - Neue Test-Hilfsbibliothek `tests/HeroPath.Testing` ohne Tests (`InMemoryAppStorage`, `ThrowingAppStorage`, `CultureScope`, `CultureBunitContext`), von den drei `*.Tests`-Projekten referenziert.
  - Neues Testpaket `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`), nur in `HeroPath.Modules.Game.Tests` referenziert. Das ist die einzige neue NuGet-Abhängigkeit. NFR6 bezieht sich damit auf Produktionscode: Die Projekte unter `src/` haben keine neuen Pakete. Alternativen: `Architektur C4.md` §6, Nr. 12.
- **Plattformzugriff:** `LocalStorageAppStorage` (Host) fängt `JSException`, `JSDisconnectedException`, `InvalidOperationException` und Zeitüberschreitungen ab: Lesen liefert `null`, Schreiben wird verworfen. Ein vom Aufrufer angeforderter Abbruch wird nicht verschluckt.

### Offen (nicht umgesetzt)

- **Visuell [COULD]** (Befunde des visuellen Checks, nicht behoben):
  - Max verdeckt die Burg beim Erfolg grösstenteils.
  - Der Erfolgsstatus hat keine Feier. Konfetti ist Should und kommt später.
  - Das Warn-Abzeichen ist in Graustufen klein und kontrastarm.
  - Max wird am Rand vom gestrichelten Feldrahmen leicht angeschnitten.
  - Die Statusbezeichnungen sind uneinheitlich: DE «Pause», EN «Paused». Zu prüfen ist «Pausiert».
  - Die Figuren wirken auf dem 10×10-Feld klein.
  - Das Tempo bleibt nach Reset erhalten. Das ist gewollt, FR11 setzt das Tempo nicht zurück.
- **Kein Screenshot mit mehr als 8 Bausteinen:** NFR4 nennt «bis ca. 10 Bausteine». Ob das Layout mit 10 Bausteinen ohne Scrollen passt, ist nicht verifiziert.
- **Kein Unit-Test für `LocalStorageAppStorage`:** Es gibt kein Testprojekt für den Host. Abgedeckt ist die Klasse nur indirekt durch den E2E-Test `LanguageTests`.
- **`ProgramEditor` rendert pro Klick zweimal:** einmal nach dem Klick-Handler, einmal durch das `Changed`-Ereignis (`SessionComponentBase`). Bewusst belassen, weil der zweite Render nur das identische Ergebnis neu zeichnet.
- **Sicherheit (Info):**
  - `index.html` enthält keine Content-Security-Policy als `<meta>`-Tag.
  - `dotnet list package --vulnerable` läuft nicht in der CI. Im Repository gibt es noch keinen `.github`-Ordner und damit keine CI.
- **Fehlermeldungen:** Kindgerechte Meldungen pro Fehlerart folgen in M3 (FR17). In M2 zeigt der Status nur den Zustand und `data-error` den Namen der `ExecutionError`.
