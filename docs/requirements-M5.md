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

## Umsetzungsstand und bekannte Einschränkungen

Stand nach M5 (Code in `src/` und `tests/`, HEAD `91cd2e4`). Die Abschnitte oben beschreiben die Anforderung, dieser Abschnitt den tatsächlichen Stand. Die Entscheidungen mit Alternativen stehen in `Architektur C4.md` §6 (Nr. 27 bis 40), der Plan in `plan.md` (D1 bis D12, alle wie empfohlen umgesetzt). Die Testläufe selbst (xUnit, bUnit, E2E) wurden beim Schreiben dieses Abschnitts nicht ausgeführt. Die Aussagen stützen sich auf den Code, die Skills in `.claude/skills` und die Messdateien in `artifacts/screenshots`.

### Abweichungen und Ergänzungen, im Verlauf entschieden

**Aufbau und Schnittstellen**

- **Editor-Seite (FR1, FR4, FR19):** `LevelEditor` ist eine Komponente in `Levels.UI`. Die routbare Seite `EditorPage` (`/editor` und `/editor/{LevelId}`) liegt in `Game.UI`, weil «Ausprobieren» `HeroChoice` braucht und `Levels` `Game` nicht kennen darf (D3). Der Knopf «Level bauen» (`start-build`) steht auf der Startseite unter «Los!».
- **Export-Schnittstelle (FR26):** Statt `ILevelFileService` gibt es `IFileDownload.SaveTextAsync(fileName, content, ct)` in `Shared/Application/Contracts`, Rückgabe `ValueTask<bool>`. `Shared` kennt keine Levels, deshalb ist die Schnittstelle allgemein. Die Web-Implementierung `BrowserFileDownload` lädt das JS-Modul `wwwroot/js/file-download.js` erst beim ersten Export. Das Modul gibt die Objekt-URL eine Sekunde nach dem Klick frei (`revokeObjectURL`), damit langsame Browser den Download noch starten können.
- **`IAppStorage` (FR18):** `SetAsync` und `RemoveAsync` liefern `ValueTask<bool>` (`true` = geschrieben). `LocalStorageAppStorage` gibt bei einem abgefangenen Fehler `false` zurück. `LanguageService` ignoriert das Ergebnis.
- **`LevelRepository`:** Der Konstruktor bekommt die Liste der Standard-Levels und die Lesefunktion (`LevelJson.Read`) von `AddLevelsModule`. `Application` darf `Infrastructure` nicht verwenden, deshalb greift der Repository nicht auf `StandardLevels` zu. Der Repository ist scoped und wird in `Program.cs` mit `LoadCustomLevelsAsync` vor dem ersten Render geladen (D12).
- **Serialisierung:** `SaveAsync`, `DeleteAsync`, `ImportAsync` und `LoadAsync` laufen hintereinander hinter einer `SemaphoreSlim`. Grenze, Duplikate und Namensnummer werden innerhalb der Sperre geprüft, sich überlappende Aufrufe können keine Änderung verlieren. Jede Änderung schreibt zuerst die ganze neue Liste in den Speicher und ersetzt erst danach die Liste im Arbeitsspeicher. Bei `StorageFailed` bleiben beide Stände unverändert und `Changed` wird nicht ausgelöst.
- **`LevelProgress`:** Es liest die Levels bei jedem Aufruf aus dem Repository. `NextAfter` und `AllSolved` gelten nur für die Standard-Level (FR23). Ein gelöstes eigenes Level merkt sich sein `Grid`. Wird es gelöscht oder mit einem anderen Spielfeld gespeichert, verliert es den Haken. Eine blosse Umbenennung behält ihn. Der Haken bleibt nur im Speicher (Sitzung).
- **`GameSession.Load`:** Dieselbe Id mit gleichem Grid, aber anderem Namen (umbenanntes eigenes Level) ersetzt nur `Level`. Programm und Lauf bleiben.
- **`HeroChoice.Preselect(id)`:** Setzt nur das Level, das die Startseite vorwählt. Held und Name bleiben, es gilt keine Wahl als bestätigt (`IsChosen` unverändert). «Ausprobieren» ohne gewählten Helden verwendet das (FR19): Die Seite navigiert auf die Startseite mit dem Fragment `#level-card-{id}`, die Karte ist vorgewählt und im sichtbaren Bereich. Mit Held ruft `EditorPage` `SelectLevel` und navigiert nach `play`. Wird ein eigenes Level gelöscht, das in `HeroChoice.LevelId` steht, ersetzen `EditorPage` und `StartPage` es durch das erste Standard-Level. Die `PlayPage` leitet auf die Startseite um, wenn `LevelProgress.Find` kein Level findet.
- **Sprung zu den eigenen Levels:** Die Startseite zeigt unter dem Einleitungstext den Link «Zu meinen Levels» (`start-jump-custom`, `href="#custom-levels"`), nur wenn eigene Levels existieren. Der Abschnitt bleibt nach den Standard-Levels (FR21).

**Speicherung und Validierung**

- **Ein Schlüssel (D1):** Alle eigenen Levels liegen als kompaktes JSON-Array unter `heropath.levels` (`StorageKeys.CustomLevels`). Jede Änderung ist ein einziger Schreibvorgang, es gibt keinen halben Stand.
- **Laden (NFR4, Edge Case «kaputter Eintrag»):** `StorageCustomLevelStore` liefert keine Levels bei leerem Wert, bei mehr als 21 × 64 KB Zeichen, bei kaputtem JSON, bei JSON-Tiefe über 16 oder wenn der Wert kein Array ist (diese Grenzen gelten für den ganzen Wert). Jedes Element läuft durch `LevelJson.Read` und `LevelCheck.CanSave`. Es wird übergangen, wenn es kaputt, zu gross, ungültig, unlösbar, über den Item-Grenzen, ohne Namen oder mit doppelter Id ist. Höchstens 20 bleiben erhalten. Übergangene Elemente bleiben im Speicher liegen und verschwinden beim nächsten Speichern, weil dann die gültige Liste geschrieben wird (so wurde die offene Frage der Edge Cases entschieden). Ein nicht lesbarer Speicher ergibt keine eigenen Levels, die App startet trotzdem.
- **Ids:** Eigene Ids sind `custom-` plus 12 Hex-Zeichen aus `RandomNumberGenerator` (`LevelIdGenerator`, bei Kollision neu gezogen). Gespeicherte Levels, deren Id einer Standard-Id gleicht, werden beim Laden verworfen. `SaveAsync` lehnt eine Standard-Id als `Invalid` ab, noch vor der Prüfung des Limits.
- **Name Pflicht (FR9):** Ein eigenes Level hat immer einen Namen, `Name` ist nie `null`. Der Editor hält den leeren Namen als `""`, die Prüfung meldet dann `Invalid` und sperrt Speichern. Der Validator lehnt Namen ab, die Format-, Bidi- oder Zeilentrenner-Zeichen enthalten (`TextCleaner.IsHidden`). Das gilt für Speicher und Import gleich. `LevelEditing.CleanName` liefert deshalb immer einen Namen, den der Validator annimmt. Die Länge 50 zählt UTF-16-Zeichen, der Editor schneidet, ohne ein Surrogatpaar zu trennen.
- **Item-Grenzen (D4):** `MaxStrawberries` und `MaxPizzas` (je 10) gelten im `LevelValidator` für alle Levels, auch für Import und Speicher. `LevelLimits.MaxSteps` (1000) spiegelt `ProgramLimits.MaxExecutedSteps`, weil `Levels` `Game` nicht referenzieren darf. `StepLimitMirrorTests` im Game-Testprojekt hält beide gleich.
- **Name bei Import:** Eine Datei ohne Namen bekommt den nächsten freien Standardnamen. Ein leerer oder nur aus Leerraum bestehender Name macht die Datei ungültig. Ist der Name unter den eigenen Levels schon vergeben, hängt `LevelEditing.UniqueName` « (2)», « (3)» … an (kleinste freie Zahl, ordinaler Vergleich, die Länge bleibt bei höchstens 50). Das gilt nur beim Import. Im Editor getippte Namen dürfen sich weiter wiederholen (Edge Case «Name gleich wie ein anderes Level»).
- **Dateiname des Exports (FR26):** `LevelEditing.ExportFileName` erzeugt Kleinbuchstaben und `[a-z0-9-]`. Umlaute werden ersetzt (ä/ö/ü/ß zu ae/oe/ue/ss), Akzente fallen weg, höchstens 40 Zeichen. Ein leerer Name ergibt `level`. Reservierte Windows-Gerätenamen (con, prn, aux, nul, com1 bis com9, lpt1 bis lpt9) bekommen das Präfix `level-`. Die Endung ist `.heropath.json`. Die Datei enthält nur Id, Name und Spielfeld, nie den Heldennamen. Neben den Export-Knöpfen steht ein Hinweis, dass der Name des Levels in der Datei steht.
- **Import-Lesen (FR27):** `InputFile` (`accept=".json,application/json"`, eine Datei) prüft zuerst `IBrowserFile.Size` gegen 256 KB und liest dann bis zu 64 K Zeichen (`LevelJson.MaxJsonLength`). Mehr ergibt «Die Datei ist zu gross.». Leseprobleme (`IOException`, `JSException` und weitere) ergeben «Die Datei konnte nicht gelesen werden.» Der Dateiname aus dem Browser wird nie angezeigt oder verwendet. Der Import fügt das Level der Liste hinzu und öffnet es nicht. Der offene Entwurf bleibt unberührt, deshalb gibt es dabei keine Rückfrage. Nach jedem Import wird das `InputFile` neu erzeugt, damit dieselbe Datei erneut wählbar ist.

**Löser und Leistung (D11, NFR2)**

- **`LevelSolver` neu geschrieben:** Der Plan sah eine BFS über (Feld, Erdbeer-Maske) vor. Umgesetzt ist eine BFS vom Start und von jeder Erdbeere aus, danach eine dynamische Programmierung über Menge und letzte Erdbeere (kürzeste Besuchsreihenfolge). Sind Ziel oder eine nötige Erdbeere unerreichbar, endet er sofort mit `null`. Ein Test vergleicht ihn auf 300 Zufallsfeldern mit einer Zustandsraum-BFS.
- **Messung:** `LevelPerformanceTests` (JIT, nicht WebAssembly) misst `LevelCheck.Evaluate` auf 20×20 mit 10 über das Feld verteilten Erdbeeren im Median von 5 Läufen mit etwa 0.6 ms. Das Laden von 20 solchen eigenen Levels dauert etwa 18 ms. Die erste Fassung (BFS über Feld und Maske) brauchte etwa 25 ms bzw. 480 ms. Im Browser misst der E2E-Test `EditorTests` (20×20, 10 Erdbeeren, alle nötig) die Zeit vom Klick auf die 10. Erdbeere, bis `data-min-steps` von 24 auf 64 wechselt. Die Messdateien `artifacts/screenshots/editor-20x20.timing.txt` (Laptop-Ansicht) und `editor-20x20.desktop.timing.txt` nennen 104 ms bzw. 103 ms, der Playwright-Klick ist darin enthalten. Der Test prüft höchstens 1 s. Vor dem Umbau des Lösers wurden nach Angabe aus dem Verlauf der Umsetzung im WebAssembly-Debug-Build 350 bis 690 ms gemessen (dazu liegt keine Messdatei vor, unverifiziert). Eine Entprellung der Prüfung war deshalb nicht nötig.
- **`LevelCheckCache` (intern):** Der Editor geht über einen Zwischenspeicher. Der Löser läuft nur neu, wenn sich das `Grid` vom letzten unterscheidet. Das Tippen des Namens validiert nur neu.

**Editor-Bedienung**

- **Rückfragen (NFR8, FR20):** `ConfirmDialog` (in `Shared.UI`) ist ein eigenes modales Fenster mit Fokus auf «Ja», Escape als Abbrechen und Tab-Falle. Ein Klick auf den Hintergrund tut nichts. «Neues Level», Vorlage und das Öffnen eines anderen Levels fragen nur bei ungespeicherten Änderungen, Löschen und «Alles leeren» fragen immer.
- **`NavigationLock` (D6):** Der Handler wartet nicht auf den Dialog. Bei ungespeicherten Änderungen ruft er `PreventNavigation()`, merkt sich das Ziel und zeigt den Dialog. «Weggehen» merkt das Ziel als erlaubt und navigiert erneut. Die Erlaubnis wird bei jeder Navigation gelöscht (auch wenn das Ziel nicht passt), eine spätere Navigation auf dieselbe Adresse fragt wieder. Eigene Navigationen des Editors und «Ausprobieren» sind ausgenommen. Neuladen und Tab-Schliessen bleiben ungeschützt (`ConfirmExternalNavigation` ist `false`).
- **«Ausprobieren» (FR19):** Der Knopf speichert mit den Regeln von FR15 bis FR18 und löst erst nach `SaveOutcome.Saved` den Wechsel aus.
- **Standardname (FR2, NFR9):** «Mein Level N» (EN «My level N») wird beim Öffnen gebildet und folgt einem Sprachwechsel, solange das Kind den Namen nicht getippt, das Level nicht gespeichert und kein gespeichertes Level geöffnet hat (`LevelEditorState.RenameDefault`). Danach bleibt der Name der Text des Kindes.
- **Zwei Spalten (NFR6):** Die rechte Seite des Editors besteht aus zwei Spalten, die getrennt scrollen. Ein Verlauf am oberen und unteren Rand (`mask-image` mit `animation-timeline: scroll()`, Rückfall auf «scroll shadows» ohne diese Unterstützung) zeigt, dass es weitergeht. Im Ausgangszustand (10×10, DE und EN, 1366×768) passt die zweite Spalte ohne internes Scrollen (`AssertEditorColumnsFitAsync`). Die Meldung eines Speicher- oder Importvorgangs steht neben dem auslösenden Knopf oder nimmt den Fokus.
- **Feldgrösse (NFR7):** Ein Feld im Editor ist höchstens 44 px gross. Bei 20×20 auf 1366×768 sind es etwa 29 px. Das ist die dokumentierte Ausnahme zur 44-px-Regel, sie gilt nur für die Felder (E2E prüft mindestens 28 px). Alle anderen Knöpfe bleiben bei 44 px.
- **`GameBoard` (D9):** Mit dem Parameter `CellClicked` ist jedes Feld ein `<button>` (`edit-cell-{x}-{y}`, `aria-label` mit Spalte, Zeile und Inhalt, beide ab 1), ohne Held. Vorschau und Spiel bleiben unverändert.
- **Weitere Test-Ids und Texte:** Siehe `.claude/skills/blazor-ui/references/level-editor.md` (Abschnitte «Level editor», «Managing levels», «Export and import»). Die resx-Schlüssel stehen im `localization`-Skill. `LevelsResourcesTests` prüft gleiche Schlüssel und Platzhalter in DE und EN und das Fehlen von «ß».
- **Behobene Punkte aus M4:** `LevelPicker` zeigt eigene Levels mit `LevelDefinition.Name` über `LevelDisplayExtensions.NameOf`. `LevelProgress` kennt eigene Levels über das Repository. `StandardLevels.Get` wirft weiter bei unbekannter Id, wird aber im Repository nicht mehr gebraucht.

### Offen (nicht umgesetzt)

- **Browser-Zurück-Knopf bei ungespeichertem Editor:** Dieser Fall ist nicht per E2E getestet. Der Schutz stützt sich auf `NavigationLock` (bUnit prüft `PreventNavigation`).
- **GitHub Pages mit Unterpfad:** Der Import des JS-Moduls (`./js/file-download.js`) ist auf einem Unterpfad nicht ausgeführt. Die Auflösung stützt sich auf `base href` und ist unverifiziert, bis der Export auf der veröffentlichten Seite geprüft ist.
- **`InputFile` im MAUI-WebView:** Ob der Import dort funktioniert, ist unverifiziert (M7). Der Export läuft hinter `IFileDownload`, eine MAUI-Implementierung gibt es nicht.
- **Andere Browser:** Firefox und WebKit sind nicht getestet, die E2E-Tests laufen in Chromium. Dazu gehört, ob die Rand-Verläufe (`animation-timeline: scroll()`) überall greifen. Ohne diese Unterstützung gilt der Rückfall.
- **NFR2 (200 ms):** Auf JIT ist die Prüfung mit etwa 0.6 ms gemessen. Im Browser sind es laut `editor-20x20.timing.txt` 103 bis 104 ms inklusive Klick. Auf dem veröffentlichten WebAssembly-Build (`dotnet publish -c Release`) ist es nicht gemessen. Der E2E-Test startet den Host mit `dotnet run`, ob dabei eine Debug- oder eine optimierte Konfiguration läuft, ist hier nicht geprüft.
- **Platz im Editor:** Der Zustand «gebaut» (viele Dinge, Speichermeldung) passt auf 1366×768 nur knapp. Bei vielen eigenen Levels scrollt die Liste in der Spalte.
- **Doppelte Prüfung:** `LevelCheck` validiert zweimal (einmal als Ergebnis, einmal als Schutznetz vor `ToGrid()`). Die Kosten sind nicht gemessen.
- **Startformular:** Der Zustand des Formulars der Startseite (Name, Wahl) bleibt beim Gang in den Editor und zurück nicht erhalten. Held, Name und Level kommen aus `HeroChoice`, was das Kind nicht bestätigt hat, ist weg.
- **Haken nur in der Sitzung:** Der Fortschritt der Levels (auch der eigenen) bleibt nur im Speicher und geht beim Neuladen verloren (Out of Scope, wie in M4).
- **Zwei Tabs:** Es gibt keine Sperre und keine Meldung. Das zuletzt gespeicherte Level gewinnt. Die eigenen Levels werden einmal beim Start geladen, ein anderer Tab sieht Änderungen daher erst nach Neuladen.
- **Aus den Reviews, [COULD], nicht behoben** (drei Runden, Befunde von `code-reviewer`, `security-reviewer` und `visual-checker`):
  - Löscht das Kind das offene Level, lösen `LevelEditorState.Open` und das folgende `OpenNew` `Changed` zweimal aus (ein zusätzlicher Render).
  - `GameSession.Load` ersetzt bei einer reinen Umbenennung `Level`, ohne `Changed` auszulösen. Die Spielseite zeigt den neuen Namen trotzdem, weil sie `LevelRepository.Changed` abonniert.
  - Der Level-Name eines Kindes steht im localStorage und in der exportierten Datei. Ausser den Hinweisen beim Namensfeld und beim Export gibt es keinen technischen Schutz (bewusster Kompromiss, NFR3).
  - Nach einem Import scrollt die zweite Spalte zur Meldung, oben ist das Namensfeld dann halb ausgeblendet (gewollter Scroll-Hinweis). Die Import-Meldung steht unter dem Import-Hinweis und nicht direkt am Knopf, sie bekommt aber den Fokus.
  - In DE bricht ein Listeneintrag wie «Mein Burgweg» neben «Exportieren» und «Löschen» auf zwei Zeilen um (lesbar, EN einzeilig).
  - Nach dem Sprung zur vorgewählten Karte liegt die Karte knapp am unteren Rand des sichtbaren Bereichs (Seitenende, weiter scrollen geht nicht).
  - Auf dem Desktop (1920×1080) wirkt ein kleines Spielfeld (z. B. 5×5 mit 44-px-Zellen) im Editor klein neben viel leerer Fläche.

### Verschoben (Out of Scope)

- Tipps oder Auflösung beim Spielen (z. B. Weg als gestrichelte Linie nach N Fehlversuchen). Der Löser in `Levels/Domain` ist die Grundlage. Aufgenommen in `Projekt_Kontext.md` §8, Entscheid nach dem Probelauf (M6).
- Undo/Redo im Editor.
- Fortschritt (Haken) oder Heldenwahl in `localStorage`.
- Neue Hindernis- oder Item-Arten, Themenwelten.
- Gespeicherte Programmlösung zum Level, Anzeige des Lösungswegs.
- Malen durch Ziehen über mehrere Felder, Drag & Drop im Editor.
- Teilen per Link oder QR-Code, Level in der URL.
- Bearbeiten der Standard-Level an Ort (nur als Vorlage).
- Export und Import im MAUI-Host.
- Schutz ungespeicherter Änderungen beim Neuladen oder Schliessen des Tabs.
