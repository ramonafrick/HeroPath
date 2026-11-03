# Requirements: M5 – Level-Editor und Speicherung

**Date**: 2026-11-03 · Vorgänger: `requirements-M4.md` (Level-Vorlagen, Start-Dialog, Items, umgesetzt), `requirements-M3.md`, `requirements-M2.md`, `requirements-M1.md`

## Problem

Nach M4 gibt es 7 feste Level. Das Lernziel 5 «Kreativität: Mit dem Level-Editor baut man eigene Aufgaben» (`Projektidee.md`, Lernziele) ist noch offen, ebenso die Should-Punkte «Level-Editor: Hindernisse setzen, Start und Ziel festlegen, Grösse ändern» und «Eigene Levels im Browser speichern» sowie der Could-Punkt «JSON-Export und -Import von Levels» (`Projekt_Kontext.md` §3). Im Spielablauf (`Projektidee.md`, Schritt 2) ist «Vorlage oder eigenes Level» vorgesehen.

Bleibt M5 aus, ist das Spiel trotzdem vollständig (M1 bis M4, `Projekt_Kontext.md` §7). Schnelle Kinder haben dann aber nach dem Burg-Labyrinth nichts mehr zu tun, und sie können keine Aufgaben für andere Kinder bauen.

## Users & Context

- Kinder 11 bis 13 Jahre ohne Vorkenntnisse, begleitet von Software Engineers, am Zukunftstag (ca. 4 Stunden). Der Editor ist für Kinder gedacht, die die Standard-Level geschafft haben, oder für eine Bau-Phase am Ende.
- Mehrere Kinder können nacheinander am gleichen Laptop arbeiten, die eigenen Levels bleiben im Browser liegen.
- Laptop mit externem Bildschirm (1920×1080) oder ohne (1366×768), Maus und Tastatur. Kein Touch.
- Host: nur `HeroPath.Web` (Blazor WebAssembly). Kein MAUI. Die Schnittstellen bleiben MAUI-tauglich (`Architektur C4.md` §4.3).

## Functional Requirements

### Editor-Seite
- FR1: Eine eigene Seite **`/editor`** («Level bauen»). Sie ist von der Startseite aus über einen Knopf «Level bauen» erreichbar. Sie zeigt links das Spielfeld zum Bearbeiten, rechts die Werkzeuge und die Liste «Meine Levels».
- FR2: **Neues Level:** 10×10, leer, Start links unten (0,9), Ziel rechts oben (9,0), Name vorausgefüllt mit «Mein Level N» (N = kleinste Nummer, die noch kein eigenes Level trägt).
- FR3: **Standard-Level als Vorlage:** Das Kind wählt eines der 7 Standard-Level und bekommt eine Kopie als neues eigenes Level (neue Id, Name «Mein Level N»). Das Standard-Level selbst bleibt unverändert.
- FR4: **Eigenes Level bearbeiten:** aus der Liste «Meine Levels» oder über den Knopf «Bearbeiten» auf der Spielseite (FR22).

### Bearbeiten
- FR5: **Werkzeugleiste** mit genau einem aktiven Werkzeug: Start, Ziel, Baum, Stein, Teich, Erdbeere, Pizza, Radiergummi. Jedes Werkzeug hat ein Bild (die bestehenden SVG-Grafiken) und einen Text. Das aktive Werkzeug ist mit Form markiert, nicht nur mit Farbe.
- FR6: **Klick auf ein Feld** wendet das aktive Werkzeug an. Jedes Feld ist ein Knopf und per Tastatur erreichbar. Es gibt kein Drag & Drop im Editor.
  - Hindernis oder Item auf leeres Feld → gesetzt. Auf ein Feld mit anderem Hindernis oder Item → ersetzt. Mit gleichem Werkzeug auf gleichem Inhalt → entfernt.
  - Start oder Ziel → wird auf das Feld verschoben. Liegt dort ein Hindernis oder Item, wird es entfernt. Start und Ziel tauschen nie stillschweigend: Klick mit «Start» auf das Ziel (oder umgekehrt) tut nichts und zeigt einen Hinweis.
  - Hindernis auf Start oder Ziel, Item auf Start → nicht möglich, Hinweis. Item auf Ziel ist erlaubt (Regel aus M1, `Grid`).
  - Radiergummi → entfernt Hindernis oder Item, Start und Ziel bleiben.
- FR7: **Grösse** einstellbar, Breite und Höhe je 3 bis 20 (`LevelLimits`), mit − und + Knöpfen und Zahlenanzeige.
- FR8: **Verkleinern:** Hindernisse und Items ausserhalb werden entfernt. Start und Ziel ausserhalb rücken auf das nächste Feld am neuen Rand. Fallen Start und Ziel dann zusammen oder läge das Ziel auf einem Hindernis, rückt das Ziel auf das nächste freie Feld. Ein Hinweis nennt die Anzahl der entfernten Dinge («3 Dinge wurden entfernt»). Die Regel liegt in `Levels/Domain`.
- FR9: **Name:** Pflichtfeld, vorausgefüllt (FR2), höchstens 50 Zeichen (`LevelLimits.MaxNameLength`). Bereinigung wie beim Heldennamen: Steuer- und Formatzeichen entfernt, Leerraum am Rand entfernt. Leer → Speichern nicht möglich, Hinweis. Unter dem Feld steht der Hinweis «Bitte verwende nicht deinen echten Namen.» Die Level-Id wird intern erzeugt und nie angezeigt.
- FR10: **«Alle Erdbeeren nötig»** (`RequireAllItems`) als Schalter. Er ist nur aktiv, wenn mindestens eine Erdbeere liegt. Wird die letzte Erdbeere entfernt, schaltet er sich aus.
- FR11: **Höchstens 10 Erdbeeren und 10 Pizza-Stücke** pro Level. Ist die Grenze erreicht, setzt das Werkzeug nichts und zeigt einen Hinweis.
- FR12: **«Alles leeren»** entfernt nach einer Rückfrage alle Hindernisse und Items. Start, Ziel, Grösse und Name bleiben.

### Prüfen und Speichern
- FR13: Der Editor prüft nach jeder Änderung, ob das Level **lösbar** ist (kürzester Weg vom Start zum Ziel, mit `RequireAllItems` über alle Erdbeeren). Der Löser liegt in `Levels/Domain` und ist mit xUnit getestet.
- FR14: **Mindestschritte:** Ist das Level lösbar, zeigt der Editor «Dein Level braucht mindestens N Schritte.» Ist es nicht lösbar, zeigt er einen kindgerechten Hinweis (z. B. «Der Held kommt nicht zur Burg. Mach einen Weg frei.»), mit `RequireAllItems` einen eigenen Hinweis zu nicht erreichbaren Erdbeeren. Die Lösung oder der Weg wird nie angezeigt.
- FR15: **Speichern** ist nur möglich, wenn das Level gültig (`LevelValidator`), lösbar und der kürzeste Weg höchstens 1000 Schritte lang ist (Schrittlimit). Sonst ist der Knopf gesperrt und der Grund steht daneben.
- FR16: Speichern **überschreibt** ein bereits gespeichertes eigenes Level (gleiche Id). Ein neues Level oder eine Vorlage bekommt beim ersten Speichern einen Platz in «Meine Levels».
- FR17: **Höchstens 20 eigene Levels.** Beim 21. erscheint «Du hast schon 20 Levels. Lösche zuerst eines.», nichts wird gespeichert.
- FR18: **Speicherfehler sichtbar:** Klappt das Speichern im Browser nicht (blockiert, voll), zeigt der Editor «Speichern hat nicht geklappt. Du kannst dein Level exportieren.» Das Level bleibt im Editor offen. Dafür meldet `IAppStorage` beim Schreiben, ob es geklappt hat.
- FR19: **«Ausprobieren»** speichert (wie FR15 bis FR18) und öffnet das Level sofort auf der Spielseite. Ohne gewählten Helden geht es über die Startseite mit diesem Level vorgewählt.
- FR20: **Ungespeicherte Änderungen:** Verlässt das Kind den Editor über einen Knopf oder Link der App, erscheint eine Rückfrage («Deine Änderungen sind noch nicht gespeichert. Trotzdem weggehen?»). Neuladen oder Schliessen des Tabs ist nicht geschützt.

### Eigene Levels spielen
- FR21: Eigene Levels erscheinen **nach den Standard-Levels**: auf der Startseite in einem eigenen Abschnitt «Meine Levels» mit Vorschau-Karten (wie FR4 von M4) und im LevelPicker als eigene Gruppe. Angezeigt wird `LevelDefinition.Name` (statt `Level_<id>` aus resx). Ohne eigene Levels fehlen Abschnitt und Gruppe.
- FR22: Auf der Spielseite gibt es bei einem eigenen Level einen Knopf **«Bearbeiten»**, der es im Editor öffnet.
- FR23: Gelöste eigene Levels tragen den **Haken** wie Standard-Levels (nur im Speicher der Sitzung). «Nächstes Level» und «Du hast alle Level geschafft!» beziehen sich weiter nur auf die 7 Standard-Level.
- FR24: Eigene Levels bleiben nach **Neuladen** erhalten (localStorage über `IAppStorage`).

### Löschen, Export, Import
- FR25: **Löschen** eines eigenen Levels aus «Meine Levels» nach Rückfrage («Level ‹Name› wirklich löschen?»).
- FR26: **Export:** Ein eigenes Level lässt sich als JSON-Datei herunterladen (Format wie `LevelJson.Write`). Der Dateiname ist aus dem Level-Namen abgeleitet und bereinigt. Der Zugriff auf den Download läuft über eine Plattform-Schnittstelle (`ILevelFileService` laut `Architektur C4.md` §4.3), die Web-Implementierung liegt im Host.
- FR27: **Import:** Das Kind wählt eine JSON-Datei aus. Sie wird geprüft wie beim Speichern (`LevelJson.Read` mit 64-KB- und Tiefen-Grenze, `LevelValidator`, Lösbarkeit, 10 Erdbeeren, 20 Levels). Gültig → neues eigenes Level mit neuer Id (nie Überschreiben, keine Kollision mit Standard-Ids), Name aus der Datei (fehlt er, «Mein Level N»). Ungültig → kindgerechte Meldung, nichts wird gespeichert.

## Non-Functional Requirements

- NFR1: Regeln (Werkzeug anwenden, Verkleinern, Grenzen, Lösbarkeit, Mindestschritte, Namensbereinigung, Nummer für «Mein Level N», Speicher-Liste) liegen in Domain/Application des Moduls `Levels` und sind mit xUnit getestet, nicht im Razor-Markup.
- NFR2: Die Lösbarkeitsprüfung eines 20×20-Levels mit 10 Erdbeeren dauert im Browser höchstens 200 ms (Messung im Plan festlegen). Der Editor bleibt dabei bedienbar.
- NFR3: Der **Heldenname** gelangt nie in ein Level, den Speicher oder den Export (Entscheid 3b, NFR2 aus M4). Der Level-Name ist der einzige frei eingegebene Text, der gespeichert und exportiert wird, deshalb der Hinweis in FR9.
- NFR4: Gespeicherte oder importierte Daten sind **nicht vertrauenswürdig**: Jedes Level aus dem Speicher und aus einer Datei wird mit `LevelJson.Read` und `LevelValidator` geprüft. Kaputte oder manipulierte Einträge führen nie zu einem Absturz.
- NFR5: Alle neuen Texte über resx in DE (Schweizer Rechtschreibung, ohne «ß») und EN, kindgerechter Ton. Der Level-Name eines Kindes wird nicht übersetzt.
- NFR6: Editor auf 1366×768 und 1920×1080 ohne waagrechtes Scrollen; ein 10×10-Feld ist auf 1366×768 ohne Seiten-Scrollen ganz sichtbar.
- NFR7: Alle Bedienelemente per Tastatur erreichbar, Knöpfe und Felder mindestens 44 px (bei 20×20 auf 1366×768 darf das Feld kleiner werden, es bleibt aber klickbar; Grenze im Plan festlegen).
- NFR8: Rückfragen (Löschen, Alles leeren, Verlassen) sind eigene Komponenten ohne `window.confirm` und ohne Fluent UI (Entscheid Nr. 24), per Tastatur bedienbar.
- NFR9: Ein Sprachwechsel im Editor behält das offene Level und die ungespeicherten Änderungen.
- NFR10: Keine CDNs, keine fremden Grafiken. Ein neues NuGet-Paket nur nach Entscheid im Plan.

## Technical Constraints

- **Module:**
  - `Levels` (Hauptteil): Editor-Regeln, Löser und eigene Level-Quelle in `HeroPath.Modules.Levels` (Domain/Application/Infrastructure); `LevelEditor`-Seite und -Komponenten, Anpassung `LevelPicker` (Gruppe, Name) in `HeroPath.Modules.Levels.UI` (`Architektur C4.md` §3, Komponenten `LevelEditor`, `LevelRepository`).
  - `Game`: `LevelProgress` bekommt die Levels aus der neuen Quelle (Standard + eigene), Startseite «Meine Levels» und «Level bauen», Spielseite «Bearbeiten».
  - `Shared`: `IAppStorage` meldet beim Schreiben Erfolg oder Fehler (FR18), neue `StorageKeys` für eigene Levels, Schnittstelle für Datei-Export (FR26).
  - Abhängigkeiten nur `Shared ← Levels ← Game`. Kein neues Modul.
- **Host:** nur `HeroPath.Web`. `LocalStorageAppStorage` an den geänderten Vertrag anpassen, Web-Implementierung des Datei-Exports (JS-Download als eigene Datei im Projekt, keine CDN). Der Import nutzt nach Möglichkeit Blazors `InputFile` ohne eigenes JavaScript (im Plan prüfen).
- **Speicher:** localStorage über `IAppStorage`. Aufbau der Schlüssel (ein Eintrag pro Level oder eine Liste) entscheidet der Plan.
- **Grenzen:** Raster 3 bis 20, 1000 Schritte, Wiederhole 1 bis 20, Tiefe 2, 200 Bausteine (unverändert). Neu: höchstens 10 Erdbeeren und 10 Pizza-Stücke pro Level, höchstens 20 eigene Levels. Ob die Item-Grenze im `LevelValidator` für alle Levels gilt, entscheidet der Plan (die Standard-Level haben heute höchstens 3 Erdbeeren).
- **Löser:** Der BFS-Löser aus den Tests (`level-design`-Skill) zieht in `Levels/Domain`, die Tests der Standard-Level verwenden danach denselben Löser.
- **Navigation:** Rückfrage beim Verlassen über Blazors `NavigationLock` (im Plan mit Quelle bestätigen).
- **E2E-Abläufe (Playwright, mit Screenshots DE und EN):**
  1. Bauen → Speichern → Spielen: Startseite «Level bauen», Werkzeuge setzen, Mindestschritte sichtbar, speichern, Karte unter «Meine Levels», Level spielen und lösen, Haken.
  2. Neuladen behält das eigene Level.
  3. Unlösbar und Fehler: Ziel eingemauert → Speichern gesperrt mit Hinweis; Verkleinern mit Hinweis; Grenze 10 Erdbeeren.
  4. Vorlage, Löschen, Export/Import: Standard-Level als Vorlage, eigenes Level löschen (Rückfrage), exportieren und die Datei wieder importieren.
  5. «Ausprobieren» und «Bearbeiten» zwischen Editor und Spielseite, Rückfrage bei ungespeicherten Änderungen.

## Acceptance Criteria

- [ ] Die Startseite hat einen Knopf «Level bauen», der `/editor` öffnet.
- [ ] Im Editor lassen sich Grösse (3 bis 20), Start, Ziel, Baum, Stein, Teich, Erdbeere, Pizza per Werkzeug und Klick setzen und entfernen, Name und «Alle Erdbeeren nötig» einstellen.
- [ ] Verkleinern entfernt Dinge ausserhalb mit Hinweis, Start und Ziel bleiben im Feld und verschieden.
- [ ] Der Editor zeigt laufend «mindestens N Schritte» oder einen Hinweis, warum das Level nicht lösbar ist; unlösbare oder ungültige Levels lassen sich nicht speichern.
- [ ] Höchstens 10 Erdbeeren, 10 Pizza-Stücke, 20 eigene Levels; Überschreiten zeigt einen Hinweis.
- [ ] Gespeicherte Levels erscheinen auf der Startseite unter «Meine Levels» und im LevelPicker mit ihrem Namen, sind spielbar, bekommen den Haken und sind nach Neuladen noch da.
- [ ] Ein Standard-Level lässt sich als Vorlage kopieren, ohne dass sich das Original ändert.
- [ ] Eigene Levels lassen sich nach Rückfrage löschen, als JSON exportieren und wieder importieren (immer als neues Level); eine ungültige Datei ergibt eine Meldung und ändert nichts.
- [ ] Ein Speicherfehler wird angezeigt, das Level bleibt offen.
- [ ] «Ausprobieren», «Bearbeiten», «Alles leeren» und die Rückfrage beim Verlassen funktionieren.
- [ ] Ein kaputter Eintrag im localStorage führt zu keinem Absturz, die übrigen Levels erscheinen.
- [ ] Der Heldenname steht in keinem gespeicherten oder exportierten Level.
- [ ] Alle neuen Texte DE und EN; xUnit-, bUnit-, Architektur- und E2E-Tests grün; Screenshots visuell geprüft (normal und Graustufen, DE und EN).

## Edge Cases & Failure Modes

- Kaputter, manipulierter oder zu grosser Eintrag im Speicher → Eintrag wird übergangen, die übrigen Levels erscheinen, kein Absturz. Ob der Eintrag gelöscht oder liegen gelassen wird, entscheidet der Plan.
- Speicher nicht verfügbar beim Start → keine eigenen Levels, Standard-Level spielbar, Editor nutzbar; Speichern zeigt den Fehler (FR18).
- Speicher voll beim Speichern → Fehler sichtbar (FR18), bisheriger Stand des Levels im Speicher bleibt unverändert.
- Zwei Tabs bearbeiten gleichzeitig → das zuletzt gespeicherte gewinnt. Keine Sperre, kein Absturz.
- Eigenes Level wird gelöscht, während es auf der Startseite gewählt ist oder in `HeroChoice.LevelId` steht → die Wahl fällt auf das erste Standard-Level zurück.
- Eigenes Level wird nach dem Spielen bearbeitet und gespeichert → beim nächsten Öffnen der Spielseite gilt die neue Fassung; das Programm wird geleert, wenn sich das Spielfeld geändert hat (Regel `GameSession.Load` aus M4).
- Name mit HTML oder Skript-Text → wird als Text angezeigt (Blazor escaped), im Dateinamen des Exports bereinigt.
- Name mit 50+ Zeichen eingefügt → gekürzt. Name gleich wie ein anderes eigenes Level → erlaubt (die Id unterscheidet).
- Import einer Datei > 64 KB, kein JSON, falsches Schema, ungültig, unlösbar, > 10 Erdbeeren → Meldung, nichts gespeichert.
- Import einer Datei mit Id eines Standard-Levels oder eines eigenen Levels → neue Id, kein Überschreiben.
- Import bei 20 eigenen Levels → Meldung wie FR17.
- Verkleinern auf 3×3 mit Start und Ziel am gleichen Randfeld → Ziel rückt auf ein freies Feld; ein 3×3-Feld hat immer genug freie Felder.
- Lösbar, aber kürzester Weg über 1000 Schritte (theoretisch bei 20×20-Labyrinth mit 10 Erdbeeren) → nicht speicherbar, eigener Hinweis.
- Darf nie passieren: Absturz durch Daten aus Speicher oder Datei; ein Standard-Level wird verändert; ein unlösbares Level landet in «Meine Levels»; der Heldenname gelangt in Speicher oder Export.

## Out of Scope

- Tipps oder Auflösung beim Spielen (z. B. Weg als gestrichelte Linie nach N Fehlversuchen). Wird als offener Punkt in `Projekt_Kontext.md` §8 aufgenommen, möglicher Kandidat für M6 nach dem Probelauf. Der Löser in `Levels/Domain` schafft die Grundlage.
- Undo/Redo im Editor.
- Fortschritt (Haken) oder Heldenwahl in `localStorage`.
- Neue Hindernis- oder Item-Arten, Themenwelten.
- Gespeicherte Programmlösung zum Level, Anzeige des Lösungswegs.
- Malen durch Ziehen über mehrere Felder, Drag & Drop im Editor.
- Teilen per Link oder QR-Code, Level in der URL.
- Bearbeiten der Standard-Level an Ort (nur als Vorlage).
- Export und Import im MAUI-Host (nur die Web-Implementierung hinter der Schnittstelle).
- Schutz ungespeicherter Änderungen beim Neuladen oder Schliessen des Tabs.

## Open Questions

- Keine fachlichen. Technische Wahl wird im Plan mit Optionen und Quellen vorgelegt: Aufbau der Speicherschlüssel, Änderung des `IAppStorage`-Vertrags, Ort der neuen Level-Quelle (`ILevelSource`/`LevelRepository`), Gültigkeit der Item-Grenze für alle Levels, JS für den Download, `NavigationLock`, Umgang mit kaputten Einträgen, Mindestgrösse der Felder bei 20×20.
