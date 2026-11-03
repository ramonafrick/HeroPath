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
| UI-Bibliothek | Fluent UI Blazor für Dialoge, Auswahl, Slider und Buttons, eigenes verspieltes Styling |
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
- Sound
- Tastaturkürzel

### Won't (vorerst)
- Backend, Benutzerkonten, Bestenlisten
- Relative Steuerung, Wenn-Blöcke
- Schritt zurück im Debug-Modus (nach dem Zukunftstag möglich, der Interpreter blockiert es nicht)
- MAUI-App und Touch-Optimierung (Architektur ist vorbereitet, Umsetzung später)

## 4. Technische Eckpunkte

- **Sprache und Framework:** C# (aktueller Sprachstand von .NET 10), Blazor WebAssembly.
- **Architektur:** Modular nach Clean Architecture. Module `Levels` (Spielfeld, Level, Editor) und `Game` (Held, Bausteine, Interpreter) plus `Shared`. Jedes Modul hat ein reines C#-Projekt (`Domain`, `Application`, `Infrastructure`, ohne UI-Abhängigkeit) und ein `.UI`-Projekt (Razor Class Library). Abhängigkeitsrichtung `Shared ← Levels ← Game`, automatisch geprüft. Hosts unter `src/Hosts`: heute `HeroPath.Web`, `HeroPath.Maui` kann später folgen. Details in `Architektur C4.md` §3.
- **Plattformdienste:** Speicher, Datei-Export und Ähnliches laufen über Schnittstellen in `Shared` (z. B. `IAppStorage`), die jeder Host implementiert.
- **Standard-Levels:** Eingebettete JSON-Ressourcen in `HeroPath.Modules.Levels`, damit sie in jedem Host funktionieren.
- **Interpreter:** Ein Interpreter für Play und Debug. Er liefert pro `Step()` einen `ExecutionStep` mit Position, Status und ggf. Fehler.
- **Schrittlimit:** Maximal 1000 ausgeführte Schritte pro Lauf, Wiederhole-Anzahl 1 bis 20, Verschachtelung maximal 2 Ebenen.
- **Programmgrösse:** Maximal 200 Bausteine pro Programm (Bewegungen und Wiederhole-Blöcke rekursiv gezählt, `ProgramLimits.MaxProgramCommands`).
- **Spielfeldgrenzen:** Breite und Höhe je 3 bis 20.
- **Level-JSON:** Maximal 64 KB (65 536 Zeichen, längere Eingaben werden vor dem Parsen abgelehnt) und maximale JSON-Tiefe 8. Level-Id nach `^[a-z0-9-]{1,40}$`, Level-Name (optional) höchstens 50 Zeichen, nicht nur Leerraum und ohne Steuerzeichen.
- **Gewinnbedingung:** Ziel erreicht. Pro Level optional zusätzlich alle Items eingesammelt (`RequireAllItems`).
- **Rendering:** CSS Grid für das Raster, SVG für Figuren, Hindernisse und Items. Bewegung mit CSS-Transitions.
- **Drag & Drop:** Native HTML5-Ereignisse in einer eigenen Komponente `ProgramEditor`, dazu immer eine Alternative per Klick. Die Technik bleibt austauschbar.
- **Lokalisierung:** `IStringLocalizer` mit resx-Dateien (`de`, `en`), Sprache im localStorage. Auch der Anzeigename der App wird lokalisiert (HeldenWeg / Hero's Path).
- **Keine externen Ressourcen:** Keine CDNs, Schriften und Grafiken liegen lokal im Projekt.

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
2. `dotnet publish -c Release` erzeugt die statischen Dateien.
3. Auf den Laptops entweder die GitHub-Pages-URL öffnen oder den publish-Ordner mit einem lokalen Mini-Server ausliefern. `index.html` per Doppelklick funktioniert bei Blazor WebAssembly nicht.
4. Vor dem Einsatz auf den Firmenlaptops mit der IT oder Security klären, ob die Installation und das Ausführen erlaubt sind.

## 7. Meilensteine (Vorschlag)

| Nr. | Inhalt | Stand |
|---|---|---|
| M1 | Domain der Module `Levels` (Grid, Level) und `Game` (Person, Befehle, Interpreter) mit Tests | ✅ erledigt |
| M2 | Blazor-Grundgerüst, Spielfeld, Figur, Play, Debug, Reset (Programm zunächst per Klick) | offen |
| M3 | Drag & Drop, Wiederhole-Block, Block-Hervorhebung, Fehlermeldungen | offen |
| M4 | Level-Vorlagen, Start-Dialog, Items | offen |
| M5 | Level-Editor und Speicherung | offen |
| M6 | Deutsch und Englisch, Feinschliff, Probelauf auf dem Ziel-Laptop | offen |
| M7 | (später, nach dem Zukunftstag) `Hosts/HeroPath.Maui` als Host, responsives Layout und Touch-Bedienung | offen |

Wenn die Zeit knapp wird, entfallen zuerst M5 und die Items. M1 bis M4 ergeben bereits ein vollwertiges Spiel. Abweichungen und offene Punkte aus M1: `requirements-M1.md`, Abschnitt «Umsetzungsstand und bekannte Einschränkungen».

## 8. Offene Punkte

- [ ] Datum des Zukunftstags und verfügbare Entwicklungszeit
- [ ] Hindernisse und Items final festlegen (Vorschlag: Baum, Stein, Teich; Erdbeere, Pizza)
- [ ] Freigabe der IT/Security für die lokale Installation auf den Laptops
- [ ] Ablauf des Halbtags (App als Einstieg, danach weitere Aufgaben?)
- [ ] Soll jedes Kind seine Levels am Ende mitnehmen können (Export)?
- [ ] Zielplattformen für Mobile (Android, iOS)? Der iOS-Build braucht einen Mac

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
| Baustein (Block) | Ein Befehl, z. B. «Rechts» oder «Wiederhole 3 mal» |
| Level | Ein Spielfeld mit Start, Ziel, Hindernissen und ggf. Items |
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
