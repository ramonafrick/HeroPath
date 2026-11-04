# HeroPath – Projektkontext

Diese Datei fasst Entscheidungen, Anforderungen und Konventionen zusammen. Sie dient als Einstieg für alle, die am Projekt arbeiten, auch für KI-Assistenten (z. B. Claude Code).

## 1. Worum geht es?

Browser-Lern-App «HeroPath» (HeldenWeg / Hero's Path) für den Zukunftstag: Kinder (11 bis 13 Jahre) programmieren eine Figur mit Drag-&-Drop-Bausteinen über ein Spielfeld mit Hindernissen, Items, Start und Ziel. Details siehe `Projektidee.md`, Aufbau siehe `Architektur C4.md`.

## 2. Getroffene Entscheidungen

| Thema | Entscheidung |
|---|---|
| Zielgruppe | Kinder 11 bis 13 Jahre, keine Vorkenntnisse |
| Gerät | Laptop mit externem Bildschirm, Maus und Tastatur (Touch nicht nötig) |
| Dauer | Ca. 4 Stunden Halbtag, danach ist der Einsatz vorbei |
| Technik | .NET 10, Blazor WebAssembly, rein clientseitig |
| Plattformen | Heute Browser. Die Architektur ist für .NET MAUI Blazor Hybrid (Mobile) als zweiten Host vorbereitet, Umsetzung später |
| UI-Bibliothek | Fluent UI Blazor für Dialoge, Auswahl, Slider und Buttons, eigenes verspieltes Styling. Stand nach M5 nicht verwendet: Startseite, Level-Auswahl und Rückfragen (`ConfirmDialog`) sind eigene Komponenten bzw. ein natives `select` (`Architektur C4.md` §6, Nr. 24) |
| Steuerung | Absolut: Hoch, Runter, Links, Rechts. Dazu Wiederhole-Block (verschachtelbar, 2 Ebenen) |
| Spielfeld | Frei einstellbar, Standard 10×10, nicht zwingend quadratisch |
| Start und Ziel | Eigene Felder mit anderer Farbe und Muster (Fussabdrücke, Burg auf Schachbrett) |
| Figuren | Max (gelbes T-Shirt, blaue Hose) und Lisa (rotes Kleid). Der Name ist vorausgefüllt und kann vom Kind überschrieben werden |
| Hindernisse | Baum, Stein, Teich |
| Items | Erdbeere (Standard), Pizza-Stück (Bonus). Austauschbar über Level-Daten. Für `RequireAllItems` zählen nur Erdbeeren, die Pizza ist freiwillig |
| Fehler | Kollision mit Hindernis oder Verlassen des Spielfelds, kindgerechte Meldung. Ein leerer Wiederhole-Block verhindert den Start mit einem Hinweis |
| Ziel | Sobald der Held das Ziel erreicht (und ggf. alle Erdbeeren hat), endet der Lauf sofort mit Erfolg. Übrige Bausteine werden nicht ausgeführt |
| Debug | Schritt für Schritt, aktueller Baustein wird hervorgehoben, kein «Schritt zurück» (später möglich) |
| Speicherung | localStorage für eigene Levels und Sprache. Heldenname wird nicht gespeichert |
| Sprachen | Deutsch (Schweizer Rechtschreibung) und Englisch, Umschalter oben rechts |
| Tests | xUnit (Core) und bUnit (UI) |
| Lizenz | MIT |
| Veröffentlichung | GitHub unter `ramonafrick`, lokale Installation auf den Büro-Laptops |
| Projektname | HeroPath. Anzeigename in der Oberfläche: «HeldenWeg» (Deutsch) und «Hero's Path» (Englisch) |
| Story | Max und Lisa sind die Helden und machen sich auf den Weg zur Burg |

## 3. Anforderungen (MoSCoW)

### Must
- Spielfeld mit einstellbarer Grösse, Standard 10×10
- Held Max oder Lisa wählen, Name vorausgefüllt und überschreibbar
- Bausteine Hoch, Runter, Links, Rechts und Wiederhole per Drag & Drop zusammenstellen
- Play, Debug und Reset
- Fehler bei Hindernis und bei Verlassen des Spielfelds, kindgerechte Texte
- Start- und Zielfeld klar erkennbar
- Mindestens 4 Level-Vorlagen
- Deutsch und Englisch mit Umschalter
- Unit-Tests für die Spielregeln

### Should
- Items einsammeln (Erdbeere, Pizza)
- Level-Editor: Hindernisse setzen, Start und Ziel festlegen, Grösse ändern
- Eigene Levels im Browser speichern
- Tempo-Regler und Hervorhebung des aktuellen Bausteins
- bUnit-Tests für die wichtigsten Komponenten
- Kleine Erfolgsanimation (z. B. Konfetti)

### Could
- JSON-Export und -Import von Levels
- Weitere Hindernisse und Themenwelten (z. B. Fussballplatz)
- Sound (seit M6 umgesetzt)
- Tastaturkürzel (seit M6 umgesetzt)

### Won't (vorerst)
- Backend, Benutzerkonten, Bestenlisten
- Relative Steuerung, Wenn-Blöcke
- Schritt zurück im Debug-Modus (nach dem Zukunftstag möglich, der Interpreter blockiert es nicht)
- MAUI-App und Touch-Optimierung (Architektur ist vorbereitet, Umsetzung später)

## 4. Technische Eckpunkte

- **Sprache und Framework:** C# (aktueller Sprachstand von .NET 10), Blazor WebAssembly.
- **Architektur:** Modular nach Clean Architecture. Module `Levels` (Spielfeld, Level, Editor) und `Game` (Held, Bausteine, Interpreter) plus `Shared`. Jedes Modul hat ein reines C#-Projekt (`Domain`, `Application`, `Infrastructure`, ohne UI-Abhängigkeit) und ein `.UI`-Projekt (Razor Class Library). Abhängigkeitsrichtung `Shared ← Levels ← Game`, automatisch geprüft. Hosts unter `src/Hosts`: heute `HeroPath.Web`, `HeroPath.Maui` kann später folgen. Details in `Architektur C4.md` §3.
- **Plattformdienste:** Speicher, Datei-Export und Ähnliches laufen über Schnittstellen in `Shared`, die jeder Host implementiert: `IAppStorage` (seit M5 melden `SetAsync` und `RemoveAsync` per `bool`, ob geschrieben wurde) und `IFileDownload` (Export, seit M5).
- **Standard-Levels:** Eingebettete JSON-Ressourcen in `HeroPath.Modules.Levels`, damit sie in jedem Host funktionieren. Seit M4 sieben Level (`free-field`, `easy-1`, `easy-2`, `strawberries`, `small-maze`, `long-road`, `castle-maze`), das Demo-Level entfällt. Sie werden weiter über die statische Klasse `StandardLevels` gelesen. Seit M5 reicht `AddLevelsModule` die Liste an den `LevelRepository` weiter, der Standard-Levels und eigene Levels vereint. `LevelProgress` (Game) liest die Levels aus dem Repository. Ein `ILevelSource` gibt es nicht (`Architektur C4.md` §6, Nr. 13).
- **Heldenwahl, Name und Fortschritt (seit M4):** `HeroChoice` (Held, normalisierter Name, gewähltes Level) und `LevelProgress` (Reihenfolge, gelöste Level) sind scoped Dienste in `Modules.Game/Application` und halten ihre Daten nur im Speicher. Nichts davon geht in `localStorage`, die URL oder die Domain. Ein Neuladen der Seite setzt Held, Name und Fortschritt zurück, `/play` ohne Wahl leitet auf `/` um. Namensregeln: höchstens 20 Zeichen, Steuer- und Formatzeichen entfernt, leer ergibt den Standardnamen. Details in `Architektur C4.md` §4.2 («Held, Level und Fortschritt») und §6 (Nr. 21 bis 26).
- **Eigene Levels und Editor (seit M5):** Der Level-Editor (`/editor`, Komponente `LevelEditor` in `Levels.UI`, Seite `EditorPage` in `Game.UI`) baut Levels mit Werkzeug und Klick. Die Regeln liegen in `Modules.Levels` (`LevelEditing`, `LevelCheck`, `LevelEditorState`). Eigene Levels liegen als eine JSON-Liste unter dem localStorage-Schlüssel `heropath.levels` (über `IAppStorage` und `ICustomLevelStore`), jedes Element wird beim Laden und Importieren wie nicht vertrauenswürdige Eingabe geprüft. Export als `.heropath.json`-Datei über `IFileDownload`, Import über Blazors `InputFile`. Der Level-Name ist der einzige frei eingegebene Text, der gespeichert und exportiert wird. Details in `Architektur C4.md` §4.2 («Editor und eigene Levels») und §6 (Nr. 27 bis 40), Abweichungen in `requirements-M5.md`, Abschnitt «Umsetzungsstand und bekannte Einschränkungen».
- **Interpreter:** Ein Interpreter für Play und Debug. Er liefert pro `Step()` einen `ExecutionStep` mit Position, Status, ggf. Fehler und (seit M3) den Runden der umschliessenden Wiederhole-Blöcke.
- **Ablaufsteuerung (seit M2):** `GameSession` hält Programm, Lauf-Status (`RunStatus`) und Tempo, `ExecutionRunner` führt Play, Debug und Reset über den Interpreter aus. Beide sind reines C# in `Modules.Game/Application`. Details in `Architektur C4.md` §4.2.
- **Programm als Baum (seit M3):** Das Programm ist eine unveränderliche `ProgramDefinition` mit verschachtelten Wiederhole-Blöcken. Alle Bearbeitungen (Einfügen, Verschieben, Entfernen, Anzahl ändern) sind reine Funktionen in `ProgramEditing` (Domain). `GameSession` hält zusätzlich Auswahl und Meldung (`GameMessage`). Details in `Architektur C4.md` §4.2 und §6 (Nr. 15 bis 20).
- **Schrittlimit:** Maximal 1000 ausgeführte Schritte pro Lauf, Wiederhole-Anzahl 1 bis 20, Verschachtelung maximal 2 Ebenen. Seit M5 gilt dieselbe Zahl als Grenze für den kürzesten Weg eines eigenen Levels (`LevelLimits.MaxSteps`), sonst ist das Level nicht speicherbar.
- **Programmgrösse:** Maximal 200 Bausteine pro Programm (Bewegungen und Wiederhole-Blöcke rekursiv gezählt, `ProgramLimits.MaxProgramCommands`).
- **Spielfeldgrenzen:** Breite und Höhe je 3 bis 20.
- **Grenzen für eigene Levels (seit M5):** höchstens 10 Erdbeeren und 10 Pizza-Stücke pro Level (der `LevelValidator` prüft sie für alle Levels), höchstens 20 eigene Levels. Beim Import wird eine Datei bis 256 KB gelesen, als Level-JSON gelten höchstens 64 KB (65 536 Zeichen). Ein eigenes Level hat einen Namen (höchstens 50 Zeichen).
- **Löser (seit M5):** `LevelSolver` in `Modules.Levels/Domain` berechnet den kürzesten Weg zum Ziel, mit `RequireAllItems` über alle Erdbeeren (BFS vom Start und von jeder Erdbeere, danach dynamische Programmierung über die Reihenfolge). Der Editor zeigt damit «mindestens N Schritte» und sperrt unlösbare Levels. Der Löser zeigt dem Kind nie den Weg. Die Tests der Standard-Level verwenden denselben Löser.
- **Level-JSON:** Maximal 64 KB (65 536 Zeichen, längere Eingaben werden vor dem Parsen abgelehnt) und maximale JSON-Tiefe 8. Level-Id nach `^[a-z0-9-]{1,40}$` (eigene Levels: `custom-` plus 12 Hex-Zeichen), Level-Name (optional, bei eigenen Levels Pflicht) höchstens 50 Zeichen, nicht nur Leerraum und ohne Steuer- und Formatzeichen.
- **Gewinnbedingung:** Ziel erreicht. Pro Level optional zusätzlich alle Items eingesammelt (`RequireAllItems`).
- **Rendering:** CSS Grid für das Raster, SVG für Figuren, Hindernisse und Items. Bewegung mit CSS-Transitions.
- **Drag & Drop (seit M3):** Native HTML5-Ereignisse über Blazor (`ProgramEditor` mit `BlockList`, `ProgramBlock`, `DropZone`, dazu `BlockPalette`), kein eigenes JavaScript. Immer mit Alternative per Klick (Auswählen, Einfügeziel, ▲▼🗑). Die Technik bleibt austauschbar. Ob Firefox den Drag ohne `dataTransfer.setData` startet, ist unverifiziert (siehe §8). Entscheid: `Architektur C4.md` §6, Nr. 20. Im Level-Editor gibt es kein Drag & Drop, nur Klick.
- **Lokalisierung:** `IStringLocalizer` mit resx-Dateien (`de`, `en`), Sprache im localStorage. Auch der Anzeigename der App wird lokalisiert (HeldenWeg / Hero's Path). Die Sprache wird ohne Neuladen umgeschaltet. Damit die `en`-Texte dann vorhanden sind, lädt `index.html` beim Start alle Satellite-Assemblies (`Blazor.start` mit `loadAllSatelliteResources`), siehe `Architektur C4.md` §6, Nr. 11.
- **Sound (seit M6):** Beim ersten Start aus, die Wahl liegt unter `heropath.sound` in `localStorage`. Töne gibt es nur bei Erfolg, Fehler und beim Einsammeln eines Items, erzeugt mit Web-Audio-Oszillatoren (`wwwroot/js/sound.js`), ohne Audiodateien. `RunSounds` (Game) hört auf `GameSession`, der Host implementiert `ISoundPlayer`.
- **Tastatur (seit M6):** Nur auf der Spielseite. Pfeile fügen Bausteine ein, Umschalt+↑/↓ verschiebt, Entf oder Rücktaste löscht, Esc hebt die Auswahl auf, Leertaste startet Play, D macht einen Debug-Schritt, R setzt zurück. Eine gehaltene Taste zählt einmal, Ctrl, Alt und Meta bleiben dem Browser. Die Tabelle steht einmal in `ShortcutKeys` (Game), der Listener auf `document` liegt hinter `IKeyboardShortcuts` im Host. Der Knopf «Tasten» öffnet die Liste (`ShortcutHelp`), während eines Laufs bleibt sie zu.
- **Erfolgsanimation (seit M6):** Nur CSS (`SuccessConfetti`), etwa 2 Sekunden, auf das Spielfeld begrenzt. Bei `prefers-reduced-motion` erscheint nur ein ruhiger goldener Rahmen.
- **Keine externen Ressourcen:** Keine CDNs, Schriften und Grafiken liegen lokal im Projekt. Die JS-Module (`wwwroot/js/`) und der Service Worker sind eigene Dateien im Host.

## 5. Konventionen

- Code, Kommentare, Variablen- und Methodennamen: **Englisch**.
- Dokumentation und Benutzertexte: **Deutsch** (Schweizer Rechtschreibung, «ss» statt «ß»), dazu Englisch über Ressourcendateien.
- Moderne C#-Syntax: Primary Constructors, Collection Expressions (`[]`), Records, Pattern Matching.
- SOLID, Dependency Injection, kleine Komponenten, Logik nicht im Razor-Markup.
- Interfaces nur, wo sie echten Nutzen haben (z. B. Speicher und Level-Quelle für Tests).
- Nullable Reference Types aktiv, Warnungen als Fehler.
- Ein Commit pro sinnvolle Änderung, kurze aussagekräftige Meldungen.
- Regeln für MAUI-Bereitschaft: siehe `Architektur C4.md`, Abschnitt 4.3 (alles in den Modulen bzw. `Shared.UI`, kein direkter Plattformzugriff in Komponenten, responsives Layout, Klick-Alternative zu Drag & Drop).
- Sichtbare Abläufe bekommen einen E2E-Test mit Screenshots (Playwright), die vor dem Review visuell geprüft werden.

## 6. Bereitstellung

1. Quellcode auf GitHub (MIT-Lizenz).
2. `dotnet publish -c Release` erzeugt die statischen Dateien. Danach ändert nur `deploy/finish-site.ps1` die `index.html` (Base-Href, Content-Security-Policy, PWA-Kennzeichen, Hash im Manifest des Service Workers).
3. Auf den Laptops gibt es drei Wege:
   - **Online:** `https://ramonafrick.github.io/HeroPath/` öffnen. Der Workflow `Pages` veröffentlicht nach jeder grünen CI auf `main`.
   - **Installierte App:** Die Pages-Version in Edge oder Chrome als App installieren. Nach einem ersten Besuch mit Internet läuft sie offline. Eine neue Version wird erst wirksam, wenn alle Fenster der App geschlossen waren.
   - **ZIP:** Das ZIP aus dem Workflow `Package` entpacken und `Start.bat` doppelklicken (PowerShell-Skript als lokaler Server auf `localhost`, kein .NET nötig, ohne Service Worker). `index.html` per Doppelklick funktioniert bei Blazor WebAssembly nicht. Unter einer strengen Richtlinie (AppLocker, App Control) kann der Server nicht starten, dann bleibt die Online-Version.
4. Vor dem Einsatz auf den Firmenlaptops mit der IT oder Security klären, ob die Wege erlaubt sind (`Probelauf.md`, «Klärung mit der IT»).

## 7. Meilensteine (Vorschlag)

| Nr. | Inhalt | Stand |
|---|---|---|
| M1 | Domain der Module `Levels` (Grid, Level) und `Game` (Person, Befehle, Interpreter) mit Tests | ✅ erledigt |
| M2 | Blazor-Grundgerüst, Spielfeld, Figur, Play, Debug, Reset (Programm zunächst per Klick) | ✅ erledigt |
| M3 | Drag & Drop, Wiederhole-Block, Block-Hervorhebung, Fehlermeldungen | ✅ erledigt |
| M4 | Level-Vorlagen, Start-Dialog, Items | ✅ erledigt |
| M5 | Level-Editor und Speicherung | ✅ erledigt |
| M6 | Deutsch und Englisch, Feinschliff, Probelauf auf dem Ziel-Laptop | in Arbeit: umgesetzt, Probelauf (`Probelauf.md`) steht aus |
| M7 | (später, nach dem Zukunftstag) `Hosts/HeroPath.Maui` als Host, responsives Layout und Touch-Bedienung | offen |

M1 bis M5 sind umgesetzt. Abweichungen und offene Punkte aus M1: `requirements-M1.md`, Abschnitt «Umsetzungsstand und bekannte Einschränkungen». Aus M2: `requirements-M2.md`, Abschnitt «Umsetzungsstand und bekannte Einschränkungen». Aus M3: `requirements-M3.md`, Abschnitt «Umsetzungsstand und bekannte Einschränkungen». Aus M4: `requirements-M4.md`, Abschnitt «Umsetzungsstand und bekannte Einschränkungen». Aus M5: `requirements-M5.md`, Abschnitt «Umsetzungsstand und bekannte Einschränkungen». Stand von M6: `requirements.md`, gleichnamiger Abschnitt.

## 8. Offene Punkte

- [ ] Datum des Zukunftstags und verfügbare Entwicklungszeit
- [x] Hindernisse und Items final festlegen: Baum, Stein, Teich; Erdbeere, Pizza (seit M4 in den sieben Standard-Levels umgesetzt, weitere Arten wären Could)
- [ ] Freigabe der IT/Security für die lokale Installation auf den Laptops
- [ ] Ablauf des Halbtags (App als Einstieg, danach weitere Aufgaben?)
- [x] Soll jedes Kind seine Levels am Ende mitnehmen können (Export)? Beantwortet durch M5: Eigene Levels lassen sich als `.heropath.json`-Datei herunterladen und wieder importieren. Ob die Downloads auf den Firmenlaptops erlaubt sind, ist nicht geprüft (bei der IT-Freigabe mitklären)
- [ ] Zielplattformen für Mobile (Android, iOS)? Der iOS-Build braucht einen Mac
- [ ] Probelauf auf dem Ziel-Laptop ausführen (`Probelauf.md`), Funde beheben oder als Einschränkung festhalten. Erst danach ist M6 abgeschlossen
- [ ] Workflow `Package` nach den Korrekturen erneut starten und das ZIP prüfen. Der letzte Lauf auf GitHub schlug fehl, die Korrektur lief bisher nur lokal
- [ ] Layout für kleine Höhen und sehr schmale Spielfelder (`requirements.md`, «Bekannte Einschränkungen»)
- [ ] Rückgängig/Wiederherstellen (Undo/Redo) im Programm-Editor: nach dem Probelauf (M6) entscheiden
- [ ] Tipps oder Auflösung beim Spielen, z. B. Weg als gestrichelte Linie nach N Fehlversuchen. Der Löser in `Levels/Domain` ist vorhanden (M5). Nach dem Probelauf (M6) entscheiden
- [ ] Drag & Drop im Firefox auf dem Firmenbrowser prüfen: Blazor kann `dataTransfer.setData` nicht aufrufen, ob Firefox den Drag dann startet, ist unverifiziert (`Architektur C4.md` §6, Nr. 20). Die Klick-Alternative funktioniert in jedem Fall

## 9. Risiken

| Risiko | Gegenmassnahme |
|---|---|
| Drag & Drop mit verschachtelten Blöcken wird aufwendig | Zuerst nur flache Liste, Wiederhole danach. Notfalls Blockly oder SortableJS einsetzen |
| Wenig Entwicklungszeit | MoSCoW einhalten, M1 bis M4 zuerst |
| Installation auf Firmenlaptops nicht erlaubt | GitHub Pages als Alternative, Test mit Firmenbrowser vorab |
| Drag & Drop funktioniert auf Touch-Geräten im MAUI-WebView nicht zuverlässig | Klick-zum-Hinzufügen von Anfang an, später Pointer Events oder eine Bibliothek |
| Kinder bleiben bei Fehlern stecken | Debug-Modus, freundliche Meldungen, einfache Einstiegslevel |

## 10. Glossar

| Begriff | Bedeutung |
|---|---|
| Programm | Liste von Bausteinen, die nacheinander ausgeführt werden |
| Baustein (Block) | Ein Befehl, z. B. «Rechts» oder «Wiederhole 3 Mal» |
| Level | Ein Spielfeld mit Start, Ziel, Hindernissen und ggf. Items |
| Eigenes Level | Ein vom Kind gebautes oder importiertes Level, im Browser gespeichert (localStorage), mit eigenem Namen |
| Debug | Programm Schritt für Schritt ausführen, um Fehler zu finden |
| Step | Ein einzelner Bewegungsschritt der Figur |

## 11. Hinweise für KI-Assistenten

- Zuerst `Architektur C4.md` lesen, dann `Projektidee.md`.
- Spielregeln gehören in den `Domain`-Ordner des zuständigen Moduls, nie ins Razor-Markup.
- Neue Komponenten gehören in das `.UI`-Projekt ihres Moduls, modulübergreifende in `Shared.UI`. Plattformzugriffe (Speicher, Dateien, `window`) nur über Schnittstellen, damit später ein MAUI-Host möglich bleibt.
- Abhängigkeiten nur in Richtung `Shared ← Levels ← Game`. `HeroPath.ArchitectureTests` muss grün bleiben.
- Zu jeder Logikänderung gehört ein xUnit-Test, zu jedem sichtbaren Ablauf ein E2E-Test.
- Zu begründende Entwurfsentscheidungen mit Alternativen kurz erklären, bevor sie umgesetzt werden.
- Keine fremden Grafiken, Texte oder Code-Auszüge übernehmen. Alles wird selbst erstellt.
- Benutzertexte nur über Ressourcendateien (DE und EN), nie fest im Code.
