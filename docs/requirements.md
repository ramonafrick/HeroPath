# Requirements: M4 – Level-Vorlagen, Start-Dialog, Items

**Date**: 2026-11-03 · Vorgänger: `requirements-M3.md` (Drag & Drop, Wiederhole, Meldungen, umgesetzt), `requirements-M2.md`, `requirements-M1.md`

## Problem

Nach M3 ist das Spiel mit Schleifen und Meldungen spielbar, aber nur auf einem einzigen Demo-Level, immer mit Max und ohne sichtbare Items. Es fehlen aus dem Must-Umfang (`Projekt_Kontext.md` §3) und dem Spielablauf (`Projektidee.md`, «Spielablauf» Schritte 1 und 2):

- **Held wählen und benennen** (Must): Max oder Lisa, Name vorausgefüllt und überschreibbar. Heute ist der Name fest «Max» (`MessageBox`, M3) und es gibt keine Grafik für Lisa.
- **Mindestens 4 Level-Vorlagen** (Must): Heute gibt es nur `00-demo.json`, `PlayPage` lädt es fest.
- **Items einsammeln** (Should): Die Regeln sind seit M1 im `Interpreter` umgesetzt (Einsammeln, `RequireAllItems`, nur Erdbeeren zählen). Das Spielfeld zeigt aber keine Items, es gibt keine Grafiken und keine Anzeige, was eingesammelt ist.

Bleibt M4 aus, gibt es am Zukunftstag nur eine Aufgabe und keine Wahl der Figur. Das Zusatzziel (Mädchen und Buben ansprechen, `Projektidee.md`) und die Lernziele 1, 2 und 4 lassen sich ohne abgestufte Level nicht erreichen.

## Users & Context

- Kinder 11 bis 13 Jahre ohne Vorkenntnisse, begleitet von Software Engineers, am Zukunftstag (ca. 4 Stunden).
- Laptop mit externem Bildschirm (1920×1080) oder ohne (1366×768), Maus und Tastatur. Kein Touch.
- Host: nur `HeroPath.Web` (Blazor WebAssembly). Kein MAUI.
- Betreuer springen zwischen Levels, je nach Tempo des Kindes.

## Functional Requirements

### Startseite (Held, Name, Level)
- FR1: Die Route `/` zeigt eine **eigene Startseite** (kein modaler Dialog auf der Spielseite). Die Spielseite zieht auf `/play` um.
- FR2: Die Startseite zeigt zwei grosse Karten **Max** und **Lisa** mit ihrer Grafik. Genau eine ist gewählt (Standard: Max). Die Wahl ist per Maus und Tastatur möglich und für Screenreader als Auswahl erkennbar.
- FR3: Ein Eingabefeld **Name** ist mit dem Standardnamen des gewählten Helden vorausgefüllt («Max» bzw. «Lisa»). Das Kind kann ihn überschreiben.
  - Höchstens **20 Zeichen** (Eingabe begrenzt).
  - Leerzeichen am Rand werden entfernt, Steuerzeichen werden entfernt.
  - Leer oder nur Leerzeichen → der Standardname des gewählten Helden wird verwendet.
  - Wechselt das Kind den Helden und hat den Namen nicht geändert (leer oder gleich dem bisherigen Standardnamen), wird der Name auf den neuen Standardnamen gesetzt. Ein selbst eingegebener Name bleibt.
- FR4: Die Startseite zeigt die **7 Level als Karten** in fester Reihenfolge, je mit Name und einer kleinen Vorschau des Spielfelds (Start, Ziel, Hindernisse, Items). Genau eines ist gewählt (Standard: das erste). Alle Level sind frei wählbar.
- FR5: Gelöste Level tragen auf ihrer Karte einen **Haken** (Form, nicht nur Farbe). «Gelöst» heisst: Ein Lauf in diesem Level endete mit `Succeeded`. Der Fortschritt liegt nur im Speicher der Sitzung (nicht in `localStorage`), Neuladen setzt ihn zurück.
- FR6: Ein Knopf **«Los!»** führt zur Spielseite mit Held, Name und Level.
- FR7: Kommt das Kind zurück auf die Startseite, sind Held, Name und Level der letzten Wahl vorausgefüllt.

### Spielseite
- FR8: Über dem Spielfeld stehen neben dem Level-Namen ein kleines Bild des Helden und sein **Name**, dazu ein Knopf **«Held wechseln»**, der zur Startseite führt.
- FR9: Auf dem Spielfeld läuft die **gewählte Figur** (Max oder Lisa).
- FR10: Alle Meldungen, die den Helden nennen (heute «Ziel nicht erreicht» und «Erfolg»), verwenden den gewählten Namen statt der Konstante «Max».
- FR11: Eine **Level-Auswahl** (LevelPicker) auf der Spielseite listet die 7 Level mit Haken für gelöste. Die Auswahl eines anderen Levels lädt es sofort.
- FR12: Nach dem Erfolg erscheint ein Knopf **«Nächstes Level»**, ausser im letzten Level. Er lädt das nächste Level in der Reihenfolge.
- FR13: Wechsel auf ein **anderes Level** (Startseite, LevelPicker, «Nächstes Level») leert das Programm und setzt den Lauf zurück (Status Bereit, Held auf Start, Items wieder da). Wird dasselbe Level erneut gewählt, bleiben Programm und Lauf wie bisher (`GameSession.Load` mit gleichem Grid).
- FR14: Wird `/play` ohne vorherige Wahl auf der Startseite geöffnet (direkter Aufruf, Neuladen), leitet die App auf die Startseite um.

### Level-Vorlagen
- FR15: Es gibt die **7 Standard-Level** aus dem `level-design`-Skill (in dieser Reihenfolge): `free-field`, `easy-1`, `easy-2`, `strawberries`, `small-maze`, `long-road`, `castle-maze`. Jedes hat einen Namen in DE und EN (`Level_<id>`).
- FR16: Das Demo-Level `demo` entfällt.
- FR17: Jedes Level erfüllt die Entwurfsregeln des `level-design`-Skills (u. a. erste Level mit ≤ 10 Bausteinen lösbar, `long-road` ohne Wiederhole mindestens 3-mal so viele Bausteine wie mit, alle Hindernisarten früh sichtbar). Vor dem Schreiben wird jedes Level als ASCII-Skizze mit Lösungsidee vorgelegt (im Plan).
- FR18: Mindestens ein Level enthält Pizza, `strawberries` verlangt alle Erdbeeren (`requireAllItems: true`).

### Items
- FR19: Erdbeeren und Pizza-Stücke werden auf dem Spielfeld mit **eigenen SVG-Grafiken** gezeigt (Form unterscheidet sie, nicht nur Farbe).
- FR20: Betritt der Held ein Item-Feld, **hüpft das Item kurz und verschwindet** (CSS-Animation). Bei `prefers-reduced-motion` verschwindet es ohne Animation. Reset und Levelwechsel zeigen alle Items wieder.
- FR21: Über dem Spielfeld steht ein **Zähler** je Item-Art, die im Level vorkommt: «Erdbeeren 1 von 3» und «Pizza 0 von 1», jeweils mit der kleinen Item-Grafik. Hat das Level eine Art nicht, fehlt ihr Zähler. Der Zähler folgt jedem Schritt (Play und Debug).
- FR22: Die **Erfolgsmeldung** nennt gefundene Pizza-Stücke: «Geschafft, Lisa! Du hast die Burg erreicht und 1 Pizza-Stück gefunden.» (Mehrzahl «2 Pizza-Stücke»). Ohne Pizza bleibt der bisherige Text.
- FR23: Sind nach diesem Erfolg **alle 7 Level gelöst**, ergänzt die Erfolgsmeldung «Du hast alle Level geschafft!».

## Non-Functional Requirements

- NFR1: Spielregeln (Items, Gewinnbedingung, Gelöst-Status, Level-Reihenfolge, Namensregeln) liegen nicht im Razor-Markup, sondern in Domain/Application des zuständigen Moduls und sind mit xUnit getestet.
- NFR2: Der Heldenname wird **nie gespeichert und nie geloggt**: nicht in `IAppStorage`, nicht in der URL, nicht in der Domain (`Person` bleibt ohne Name, Entscheid 3b), nicht in `GameMessage`.
- NFR3: Alle neuen Texte über resx in DE (Schweizer Rechtschreibung, ohne «ß») und EN, kindgerechter Ton.
- NFR4: Startseite auf 1366×768 und 1920×1080 ohne waagrechtes Scrollen; Spielseite behält das Laptop-Layout aus M3 (10 Zeilen ohne Seiten-Scrollen auf 1366×768) trotz der neuen Zeile mit Held und Zählern.
- NFR5: Alle Grafiken selbst gezeichnet (`svg-assets`-Skill), farbenblind-tauglich (Max/Lisa und Erdbeere/Pizza unterscheiden sich in Form oder Muster), mit lokalisiertem Titel.
- NFR6: Alle Bedienelemente per Tastatur erreichbar, Knöpfe mindestens 44 px.
- NFR7: Keine CDNs, keine fremden Grafiken. Ein neues NuGet-Paket nur nach Entscheid im Plan.
- NFR8: Ein Sprachwechsel auf Start- und Spielseite behält Held, Name, Level, Fortschritt und Programm (wie M2).

## Technical Constraints

- **Module:** `Levels` (Level-JSON, Item-Darstellung im `GameBoard`, Vorschau, LevelPicker, Item-Grafiken liegen nach `svg-assets` in `Shared.UI`), `Game` (Startseite, Heldenwahl und Name als UI- bzw. Application-Zustand, Zähler, Meldungen, Gelöst-Status). Abhängigkeiten nur `Shared ← Levels ← Game`. Kein neues Modul.
- **Host:** nur `HeroPath.Web`; Router lädt die Seiten aus den Modul-Assemblies wie heute.
- **Speicher:** keiner neu. Held, Name, Fortschritt nur im Speicher (scoped Dienst).
- **Grenzen:** unverändert (Raster 3 bis 20, 1000 Schritte, Wiederhole 1 bis 20, Tiefe 2, 200 Bausteine).
- **Fluent UI:** Ob Startseite und LevelPicker Fluent-UI-Komponenten verwenden (Entscheid Nr. 6) oder eigene, wird im Plan mit Optionen entschieden.
- **Lösbarkeit:** `StandardLevelsTests` prüft alle 7 Level (gültig, lösbar per BFS, mit `requireAllItems` über alle Erdbeeren).
- **E2E-Abläufe (Playwright, mit Screenshots):**
  1. Startseite: Lisa wählen, Namen eingeben, Level wählen, «Los!» → Spielseite zeigt Lisa, Namen und Level.
  2. Direkter Aufruf von `/play` → Startseite.
  3. Items: Level mit Erdbeeren und Pizza spielen, Zähler zählt, Items verschwinden, Erfolgsmeldung mit Pizza und Namen.
  4. Levelwechsel über LevelPicker und «Nächstes Level» leert das Programm; Haken auf gelöstem Level (Start- und Spielseite).
  5. Sprachwechsel auf der Startseite behält Eingaben.

## Acceptance Criteria

- [ ] `/` zeigt Heldenwahl (Max, Lisa), Namensfeld (max. 20 Zeichen) und 7 Level-Karten mit Vorschau; «Los!» öffnet `/play`.
- [ ] Lisa ist als eigene SVG-Grafik gezeichnet und läuft auf dem Feld, wenn sie gewählt ist.
- [ ] Leerer Name ergibt den Standardnamen; der Name erscheint über dem Feld und in den Meldungen; er steht nirgends in `IAppStorage` oder der URL.
- [ ] `/play` ohne Wahl leitet auf `/` um.
- [ ] Die 7 Level liegen als `01-…` bis `07-…` JSON vor, `demo` ist entfernt, `StandardLevelsTests` ist grün.
- [ ] Erdbeeren und Pizza sind auf dem Feld sichtbar, verschwinden mit kurzer Animation beim Einsammeln und erscheinen nach Reset wieder.
- [ ] Zähler «Erdbeeren x von y» / «Pizza x von y» erscheinen nur für vorhandene Arten und zählen bei Play und Debug mit.
- [ ] Erfolgsmeldung nennt Pizza-Stücke (Einzahl/Mehrzahl) und, wenn alle 7 gelöst sind, «Du hast alle Level geschafft!».
- [ ] Gelöste Level zeigen einen Haken auf Startseite und im LevelPicker.
- [ ] «Nächstes Level» erscheint nach Erfolg (nicht im letzten Level); Levelwechsel leert das Programm.
- [ ] Alle neuen Texte DE und EN; xUnit-, bUnit- und E2E-Tests grün; Screenshots visuell geprüft (normal und Graustufen, DE und EN).

## Edge Cases & Failure Modes

- Name nur aus Leerzeichen → Standardname, kein Fehler.
- Name mit 20+ Zeichen eingefügt (Paste) → auf 20 Zeichen gekürzt.
- Name mit HTML oder Skript-Text → wird als Text angezeigt (Blazor escaped), nie als Markup.
- Heldenwechsel nach eigenem Namen → eigener Name bleibt.
- Levelwechsel während Play → Lauf bricht ab (Runner-Abbruch wie bei `Load` heute), Programm leer, kein Schritt mehr danach.
- «Held wechseln» während Play → Play stoppt (`PlayPage.Dispose`, M2), Rückkehr mit gleichem Level setzt fort (Pause).
- Neuladen der Seite → Held, Name, Fortschritt sind weg, Startseite erscheint. Das ist gewollt (Name wird nicht gespeichert).
- Erfolg im gleichen Level mehrmals → Level bleibt einmal gelöst, «alle geschafft» erscheint erst, wenn alle 7 je einmal gelöst sind (und bei jedem weiteren Erfolg danach).
- Pizza nicht eingesammelt im Pflicht-Level → Erfolg trotzdem (M1-Regel), Meldung ohne Pizza-Teil.
- Ungültiges eingebettetes Level → Programmierfehler, `StandardLevelsTests` schlägt fehl (kein Laufzeit-Fallback nötig).
- Grosses Level (20×20) als kleine Vorschau → bleibt erkennbar, keine Layout-Sprünge.

## Out of Scope

- Konfetti oder andere Erfolgsanimation (später, M6).
- Level-Editor, eigene Levels, Speichern von Levels (M5).
- Fortschritt oder Heldenwahl in `localStorage`.
- Level in der URL (`/play/<id>`).
- Freischalten der Level nacheinander.
- Programm pro Level merken.
- Weitere Figuren, Items oder Themenwelten.
- MAUI-Host.

## Open Questions

- Keine. Technische Wahl (Fluent UI oder eigene Komponenten, Ort des Gelöst-Status, Startseite in `Game.UI`) wird im Plan mit Optionen vorgelegt.
