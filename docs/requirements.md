# Requirements: M6 – Deutsch und Englisch, Feinschliff, Probelauf auf dem Ziel-Laptop

**Date**: 2026-11-04 · Vorgänger: `requirements-M5.md` (Level-Editor und Speicherung, umgesetzt), `requirements-M4.md`, `requirements-M3.md`, `requirements-M2.md`, `requirements-M1.md`

## Problem

Nach M5 ist das Spiel fachlich vollständig, aber nicht einsatzbereit. Die App läuft nur lokal aus dem Quellcode: `.github/` fehlt, es gibt also keine CI, keine GitHub-Pages-Seite und kein Paket für die Laptops. Sie wurde nie auf einem Ziel-Laptop gestartet (`Architektur C4.md` §8, «Manuell»).

Die Sprachumschaltung funktioniert, und alle drei Ressourcenpaare haben gleich viele Schlüssel (196 pro Sprache). Es bleiben aber Lücken: `<html lang>` bleibt beim Wechsel auf Englisch `de`, und die Texte wurden nie gesamthaft auf Ton und Schweizer Schreibweise durchgesehen. Vom Feinschliff fehlen der Should-Punkt «Kleine Erfolgsanimation» sowie die Could-Punkte «Sound» und «Tastaturkürzel» (`Projekt_Kontext.md` §3).

Bleibt M6 aus, lässt sich HeroPath am Zukunftstag nicht verlässlich einsetzen: Ob die App auf den Firmenlaptops startet, ist unbewiesen.

## Users & Context

- Kinder 11 bis 13 Jahre ohne Vorkenntnisse, begleitet von Software Engineers, einmalig am Zukunftstag (ca. 4 Stunden).
- Mehrere Laptops im selben Raum, mit externem Bildschirm, Maus und Tastatur (`Projekt_Kontext.md` §2).
- Browser auf den Ziel-Laptops: Edge oder Chrome (Interview vom 2026-11-04). Firefox ist optional.
- Die Betreuenden richten die Laptops ein. Den Probelauf führt die Projektverantwortliche auf einem Ziel-Laptop durch.

## Functional Requirements

### Auslieferung

- FR1: CI: Bei Push und Pull Request auf `main` laufen Formatprüfung, Build, Unit-, bUnit- und Architekturtests sowie die E2E-Tests. Die Screenshots sind als Artefakt abrufbar.
- FR2: GitHub Pages: Ein Workflow veröffentlicht die App unter `/HeroPath/` (Base-Href, `404.html` für Direktlinks, `.nojekyll`). Direktlinks wie `/HeroPath/editor` funktionieren. Das Aktivieren von Pages im Repository macht die Projektverantwortliche.
- FR3: ZIP für die Laptops: Ein Workflow erzeugt ein ZIP mit der App und einem lokalen Server, der ohne installiertes .NET und ohne Internet läuft. Ein Doppelklick auf eine Startdatei startet den Server und öffnet den Browser. Die Umsetzung (EXE oder andere Variante) wird im Plan entschieden, Application Control auf den Laptops ist dabei zu berücksichtigen.
- FR4: Eine kurze deutsche Anleitung im ZIP erklärt Start, Beenden und das Vorgehen, wenn die Startdatei blockiert wird.

### Probelauf

- FR5: `docs/Probelauf.md` enthält eine Prüfliste für den Ziel-Laptop: beide Auslieferungswege, alle sieben Level, Play/Debug/Reset, Sprachwechsel, Drag & Drop und Klick-Alternative, Editor, Export und Import, Sound, Tastatur, Bildschirmgrösse, Startzeit, sowie die offenen manuellen Punkte aus `Architektur C4.md` §8 (Animationen, Zurück-Knopf im Editor, NFR2 aus M5).
- FR6: Funde aus dem Probelauf werden in M6 behoben oder bewusst als bekannte Einschränkung festgehalten. M6 gilt erst nach dem Probelauf als abgeschlossen.

### Sprache

- FR7: `<html lang>` folgt der gewählten Sprache: nach dem Lesen der gespeicherten Sprache beim Start und bei jedem Wechsel (`de-CH` / `en`).
- FR8: Alle Texte in `de` und `en` werden durchgesehen: «du»-Form, einfache Wörter, Schweizer Schreibweise, Guillemets, keine zusammengesetzten Satzbausteine. Korrekturen laufen über die resx-Dateien.
- FR9: Sichtbare, feste Texte ausserhalb der resx-Dateien werden gesucht und behoben. Der Ladebildschirm und die Blazor-Fehleranzeige werden dabei mitgeprüft.

### Erfolgsanimation

- FR10: Beim Erfolg (Play und Debug) erscheint etwa 2 Sekunden Konfetti, begrenzt auf das Spielfeld. Es ist nicht klickbar und verschwindet von selbst.
- FR11: Bei `prefers-reduced-motion: reduce` gibt es statt Konfetti einen ruhigen Farbakzent.
- FR12: Reset, Levelwechsel oder ein neuer Lauf entfernen die Animation sofort. Ein Sprachwechsel startet sie nicht neu.

### Sound

- FR13: Ein Schalter in der Kopfzeile (neben der Sprachwahl) schaltet den Ton ein und aus. Sein Zustand ist auch ohne Farbe erkennbar, und er hat einen lokalisierten Namen.
- FR14: Beim ersten Start ist der Ton aus. Die Wahl wird über `IAppStorage` gespeichert und bleibt beim Neuladen erhalten.
- FR15: Eingeschaltet klingt je ein kurzer, selbst erzeugter Ton bei Erfolg, bei Fehler (Hindernis, Spielfeldrand, Schrittlimit, Ziel nicht erreicht) und beim Einsammeln eines Items. Pro Schritt gibt es keinen Ton.
- FR16: Ist Ton nicht verfügbar, läuft das Spiel stumm und ohne Fehlermeldung weiter.

### Tastatur

- FR17: Auf der Spielseite fügen die Pfeiltasten den passenden Bewegungsbaustein ein, an derselben Stelle wie ein Klick auf den Baustein in der Palette.
- FR18: Der ausgewählte Baustein lässt sich per Tastatur verschieben und löschen.
- FR19: Tasten für Play, Debug-Schritt und Reset. Die Belegung wird im Plan festgelegt.
- FR20: Die Spielseite zeigt eine kurze, lokalisierte Liste der Tasten.
- FR21: Die Kürzel wirken nicht, solange ein Eingabefeld, das Level-Select, der Tempo-Regler oder ein Dialog den Fokus hat. Eine Taste auf einem fokussierten Knopf löst nur diesen Knopf aus, nicht zusätzlich ein Kürzel. Der Level-Editor bekommt keine Kürzel.

## Non-Functional Requirements

- NFR1: Keine CDNs und keine externen Aufrufe zur Laufzeit. Der Publish-Ordner lädt nichts von fremden Servern.
- NFR2: Konfetti, Töne und Grafiken sind selbst erstellt.
- NFR3: Animation und Töne verzögern den Ablauf nicht. Play-Tempo und Schrittlimit bleiben unverändert.
- NFR4: Farbe ist nie alleiniger Träger von Information, auch nicht beim Ton-Schalter und in der Tastenliste.
- NFR5: Keine neuen Daten über das Kind. Neu gespeichert wird nur die Ton-Einstellung, der Heldenname weiterhin nie.
- NFR6: Die Workflows brauchen keine Geheimnisse ausser dem von GitHub bereitgestellten Token und laufen mit minimalen Rechten.
- NFR7: Die Startzeit bis zur nutzbaren Startseite wird auf dem Ziel-Laptop gemessen und in `docs/Probelauf.md` festgehalten (Schwelle siehe Open Questions).

## Technical Constraints

- Betroffene Projekte: `HeroPath.Shared` und `HeroPath.Shared.UI` (Sprache, Ton-Schalter, Plattformschnittstellen), `HeroPath.Modules.Game` und `.UI` (Kürzel, Auslöser für Konfetti und Ton), `HeroPath.Modules.Levels.UI` (Konfetti auf dem Spielfeld, falls dort am sinnvollsten), `Hosts/HeroPath.Web` (Implementierungen, `index.html`). Neue Workflows unter `.github/workflows`. Ein MAUI-Host wird nicht gebaut.
- Kein Plattformzugriff in Komponenten. `document.documentElement.lang` und Audio laufen über Schnittstellen in `Shared`, die der Host implementiert (`Architektur C4.md` §4.3). Das gilt auch für neue JS-Module.
- Spielregeln bleiben im `Domain`-Ordner. Für Kürzel und Ton werden keine Spielregeln dupliziert. Einfügen, Verschieben und Löschen nutzen die vorhandenen Abläufe von `GameSession` und `ProgramEditing`.
- Grenzen bleiben: 1000 Schritte, 200 Bausteine, Wiederhole 1 bis 20, Tiefe 2, Spielfeld 3 bis 20.
- Neue Texte in `de` und `en` über resx. Neue öffentliche Typen der Module in `game-rules` beschreiben (`ClaudeWorkflowTests`).
- Commits und Push auf `origin/main` sind erlaubt. Pages aktivieren und einen Release auslösen macht die Projektverantwortliche im GitHub-Web. `gh` ist auf diesem Rechner nicht installiert.
- E2E-Abläufe mit Screenshots (normal und Graustufen, DE und EN): Konfetti nach Erfolg, Ton-Schalter mit Neuladen, Bedienung nur mit Tastatur bis zum Erfolg, `<html lang>` nach dem Wechsel, Tastenliste.

## Acceptance Criteria

- [ ] CI läuft auf `main` grün, Screenshots sind als Artefakt abrufbar.
- [ ] Die Pages-URL startet die App. Direktlinks auf `/HeroPath/editor` und `/HeroPath/play` funktionieren, Sprachwechsel und Export auch unter dem Unterpfad.
- [ ] Das ZIP lässt sich auf diesem Rechner ohne Internet und ohne .NET-SDK-Aufruf starten, und ein Level lässt sich durchspielen.
- [ ] `docs/Probelauf.md` ist vorhanden, die Projektverantwortliche hat sie auf einem Ziel-Laptop ausgeführt, und die Ergebnisse stehen in der Datei.
- [ ] `<html lang>` ist nach jedem Wechsel und nach dem Neuladen korrekt (E2E).
- [ ] Beim Erfolg läuft Konfetti, ohne die Bedienung zu blockieren. Bei reduzierter Bewegung erscheint nur der Farbakzent (E2E).
- [ ] Der Ton ist beim ersten Start aus, die Wahl überlebt ein Neuladen, und die drei Ereignisse lösen den richtigen Ton aus (Unit- oder bUnit-Test mit Fake, E2E für den Schalter). Das Hören selbst wird im Probelauf geprüft.
- [ ] Ein Level lässt sich vom ersten Baustein bis zum Erfolg nur mit der Tastatur lösen (E2E).
- [ ] Im Namensfeld, im Level-Select, im Tempo-Regler und im Dialog lösen Tasten keine Kürzel aus (bUnit oder E2E).
- [ ] Die Textdurchsicht ist abgeschlossen. Die Tests prüfen weiter gleiche Schlüssel in DE und EN sowie kein «ß».
- [ ] `dotnet build`, `dotnet test`, `dotnet format` und die E2E-Tests sind grün.

## Edge Cases & Failure Modes

- Storage nicht verfügbar → Ton bleibt aus, Sprache läuft weiter, keine Fehlermeldung.
- Audio blockiert (Autoplay-Regel des Browsers, kein Gerät) → stumm weiter, keine Meldung, kein Browserfehler in der Konsole.
- Taste wird gedrückt gehalten (Tastenwiederholung) → erzeugt keine versehentlichen Serien. Die Regel legt der Plan fest, die Grenze von 200 Bausteinen gilt ohnehin.
- Kürzel während eines Laufs → Bearbeiten ist gesperrt wie beim Klick, Reset wirkt.
- Programm voll oder keine Auswahl → gleiche Meldung wie beim Klick, oder es passiert nichts.
- Mehrere Erfolge in Folge → Animation startet neu, keine Reste stapeln sich.
- Sprachwechsel während der Animation → Animation läuft zu Ende.
- Pages unter Unterpfad → JS-Module und `_content` laden mit relativen Pfaden.
- Laptop blockiert die Startdatei oder den ZIP-Inhalt → Anleitung nennt den Pages-Weg. Scheitern beide, entscheidet die Projektverantwortliche mit der IT.
- Port des lokalen Servers belegt → Startdatei oder Anleitung behandelt den Fall (Entscheid im Plan).
- Kein Internet am Event → ZIP-Weg, ohne externe Aufrufe.

## Out of Scope

- Undo/Redo und Tipps: Entscheid nach dem Probelauf (`Projekt_Kontext.md` §8).
- Tastaturkürzel im Level-Editor.
- Ton pro Schritt, Hintergrundmusik, Lautstärkeregler.
- Konfetti über die ganze Seite.
- Firefox als Pflicht und Firefox-E2E. Firefox bleibt ein optionaler Punkt der Prüfliste.
- MAUI-Host und Touch (M7).
- Tags und Releases durch den Agenten, Pages-Aktivierung im Repository.
- Code-Signierung der EXE.
- Weitere Themenwelten, Hindernisse oder Items.

## Open Questions

- Das Datum des Zukunftstags ist unbekannt. Annahme: Der Plan ordnet die Auslieferung zuerst, der Feinschliff lässt sich jederzeit abbrechen.
- Die IT-/Security-Freigabe für lokales Ausführen und Downloads ist offen (`Projekt_Kontext.md` §8) und liegt ausserhalb dieses Repositories.
- Das Repository ist öffentlich (Aufruf ohne Anmeldung am 2026-11-04: HTTP 200), Pages ist damit im Gratis-Plan möglich.
- Annahme NFR7: Vorschlag ist eine Schwelle von 15 Sekunden beim Kaltstart. Sie wird nach der ersten Messung bestätigt oder angepasst.
- Annahme FR15: «Fehler» umfasst alle Lauf-Fehler, nicht aber Hinweise vor dem Start wie ein leeres Programm oder einen leeren Wiederhole-Block.
