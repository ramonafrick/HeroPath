# Requirements: M3 – Drag & Drop, Wiederhole-Block, Block-Hervorhebung, Fehlermeldungen

**Date**: 2026-11-03 · Vorgänger: `requirements-M2.md` (Grundgerüst, umgesetzt), `requirements-M1.md` (Domain, umgesetzt)

## Problem

Nach M2 ist das Spiel spielbar, aber nur mit einer flachen Liste von Richtungs-Bausteinen, die per Klick angehängt werden. Es fehlen drei Dinge aus dem Must-Umfang (`Projekt_Kontext.md` §3) und den Lernzielen (`Projektidee.md`):

- **Wiederholung** (Lernziel 2): Ohne Wiederhole-Block gibt es keine Schleifen. Die Domain kann sie seit M1 (`RepeatCommand`, verschachtelt bis 2 Ebenen), die UI noch nicht.
- **Drag & Drop** (Must): Bausteine werden laut Projektidee ins Programm «gezogen». Dazu gehört Einfügen an beliebiger Stelle und Umsortieren.
- **Kindgerechte Meldungen** (Must, M2 FR17): Der Status zeigt heute nur «Fehler». Kinder erfahren nicht, was passiert ist und wo sie suchen sollen (Lernziel 3, Debugging).

Bleibt M3 aus, fehlen am Zukunftstag Schleifen und verständliche Fehler. Die Kinder bleiben bei Fehlern stecken (Risiko in `Projekt_Kontext.md` §9).

## Users & Context

- Kinder 11 bis 13 Jahre ohne Vorkenntnisse, begleitet von Software Engineers, am Zukunftstag (ca. 4 Stunden).
- Laptop mit externem Bildschirm (1920×1080) oder ohne (1366×768), Maus und Tastatur. Kein Touch.
- Host: nur `HeroPath.Web` (Blazor WebAssembly). Kein MAUI.

## Functional Requirements

### Programm als Baum, Wiederhole-Block
- FR1: Die Palette erhält einen fünften Baustein **«Wiederhole»**. Ein neuer Wiederhole-Block hat die Anzahl **3** und einen leeren Rumpf.
- FR2: Der Wiederhole-Block zeigt im Kopf «Wiederhole [−] N [+] mal». − und + sind 44-px-Knöpfe, N bleibt zwischen 1 und 20 (`ProgramLimits`). An der Grenze ist der jeweilige Knopf gesperrt.
- FR3: Der Rumpf eines Wiederhole-Blocks ist eingerückt und enthält Bausteine, auch einen weiteren Wiederhole-Block (maximal 2 Ebenen, `ProgramLimits.MaxRepeatNestingDepth`).
- FR4: Das Programm wird **einspaltig** dargestellt (heute zwei Spalten). Jeder Baustein trägt eine hierarchische Nummer: oben 1, 2, 3 …, im Rumpf von Baustein 3 dann 3.1, 3.2, verschachtelt 3.2.1.
- FR5: Die Gesamtgrenze von 200 Bausteinen (`ProgramLimits.MaxProgramCommands`, rekursiv gezählt) gilt weiter. Ist sie erreicht, ist die Palette gesperrt und ein Hinweis erklärt den Grund (FR22).

### Drag & Drop (Haupt-Bedienung)
- FR6: **Palette → Programm:** Ein Palette-Baustein lässt sich an jede Stelle ziehen: zwischen zwei Bausteine, ans Ende, in einen (auch leeren) Wiederhole-Rumpf. Eine Einfüge-Linie zeigt die Zielstelle.
- FR7: **Umsortieren:** Ein Programm-Baustein lässt sich an eine andere Stelle ziehen, auch in einen Wiederhole-Rumpf hinein oder aus ihm heraus. Ein Wiederhole-Block wird mit seinem ganzen Rumpf verschoben.
- FR8: **Herausziehen zum Löschen:** Ein Programm-Baustein, der auf die Palette gezogen wird, wird gelöscht (mit Rumpf).
- FR9: **Ungültige Ziele:** Ein Wiederhole-Block darf nicht in die 3. Ebene und nicht in seinen eigenen Rumpf. Solche Ziele zeigen beim Ziehen ein «Nein»-Zeichen, das Ablegen bewirkt nichts. Kein Fehler, keine Exception.
- FR10: Während eines Laufs (Läuft, Pause) und nach einem Endzustand bis Reset ist Drag & Drop gesperrt, wie die übrigen Bearbeitungen (M2 FR13).

### Klick-Alternative
- FR11: **Auswählen:** Ein Klick auf einen Programm-Baustein wählt ihn aus (dicker Rahmen, `aria-pressed` bzw. gleichwertiger Zustand). Nur der ausgewählte Baustein zeigt **▲ ▼ 🗑** (je 44 px). Ein zweiter Klick oder ein Klick auf einen anderen Baustein ändert die Auswahl.
- FR12: **Einfügeziel:** Ist ein Wiederhole-Block oder ein Baustein in dessen Rumpf ausgewählt, hängt ein Klick auf die Palette in den Rumpf dieses Wiederhole-Blocks an, sonst ans Programmende. Eine kurze Zeile zeigt, wohin der nächste Baustein kommt («Neue Bausteine kommen in: Wiederhole 3» bzw. «… ans Ende»).
- FR13: **▲ / ▼** verschieben den ausgewählten Baustein um eine Stelle unter seinen Geschwistern. Am Anfang bzw. Ende eines Wiederhole-Rumpfs verlassen sie den Rumpf (direkt vor bzw. nach den Wiederhole-Block). Ganz oben bzw. unten im Programm ist der Knopf gesperrt. Die Auswahl bleibt auf dem verschobenen Baustein.
- FR14: **🗑** löscht den ausgewählten Baustein (Wiederhole mit Rumpf). «Alles löschen» bleibt.
- FR15: Ist ein Einfügeziel gewählt, in das kein Wiederhole-Block mehr passt (2. Ebene), ist der Palette-Knopf «Wiederhole» gesperrt und ein Hinweis erklärt es (FR22).

### Hervorhebung während des Laufs
- FR16: Wie in M2 wird der aktuelle Bewegungs-Baustein hervorgehoben (`data-current`), bei Fehler zusätzlich markiert (`data-error`). Das gilt auch für Bausteine im Wiederhole-Rumpf.
- FR17: Jeder Wiederhole-Block, in dem der aktuelle Baustein liegt, erhält einen leichteren Rahmen und zeigt die Runde: **«Runde 2 von 3»**. Bei Verschachtelung zeigen beide Blöcke ihre eigene Runde. Die Information kommt aus dem `Interpreter` (Domain), nicht aus der UI.
- FR18: Mit Beginn eines Laufs wird die Auswahl (FR11) aufgehoben.

### Meldungen
- FR19: Unter dem Status in der Seitenleiste erscheint ein **Meldungsfeld** mit Symbol und Text, gefärbt nach Art (Fehler, Erfolg, Hinweis) und zusätzlich mit unterschiedlicher Form, damit es auch in Graustufen erkennbar ist. `aria-live="polite"`.
- FR20: Meldungen nach Art, mit der Nummer aus FR4 («Baustein N») und dem Heldennamen (in M3 fest «Max», als Platzhalter im Text):

  | Situation | Deutsch | Englisch (Vorschlag) |
  |---|---|---|
  | Hindernis (Baum/Stein/Teich) | «Autsch! Da steht ein Baum im Weg. Schau dir Baustein 6 noch einmal an.» | «Ouch! There's a tree in the way. Take another look at block 6.» |
  | Spielfeldrand | «Hoppla! Hier ist die Welt zu Ende. Baustein 6 führt aus dem Spielfeld.» | «Oops! The world ends here. Block 6 leads off the board.» |
  | Ziel nicht erreicht | «Dein Programm ist fertig, aber Max ist noch nicht bei der Burg. Probier es nochmal!» | «Your program is done, but Max hasn't reached the castle yet. Try again!» |
  | Erdbeeren fehlen | «Fast geschafft! Es fehlen noch 2 Erdbeeren.» (Einzahl: «Es fehlt noch 1 Erdbeere.») | «Almost there! 2 strawberries are still missing.» / «1 strawberry is still missing.» |
  | Schrittlimit | «Puh! Dein Programm macht mehr als 1000 Schritte. Brauchst du wirklich so viele Wiederholungen?» | «Phew! Your program takes more than 1000 steps. Do you really need that many repeats?» |
  | Erfolg | «Geschafft, Max! Du hast die Burg erreicht.» | «You did it, Max! You reached the castle.» |

- FR21: **Leerer Wiederhole-Block:** Play und Debug starten keinen Lauf. Hinweis «Leg zuerst Bausteine in den Wiederhole-Block.», der erste leere Wiederhole-Block wird markiert, der Status bleibt «Bereit», das Programm bleibt bearbeitbar. Der Hinweis verschwindet bei der nächsten Bearbeitung.
- FR22: **Hinweise ohne Lauf:**
  - Programm leer: «Zieh zuerst ein paar Bausteine ins Programm.» (ersetzt den heutigen Leer-Text; Play/Debug bleiben gesperrt).
  - Programm voll (200): «Dein Programm ist voll. Lösch zuerst einen Baustein.»
  - Verschachtelung (FR15): «Hier passt kein weiterer Wiederhole-Block hinein.»
- FR23: Reset blendet Fehler- und Erfolgsmeldungen aus. Ein Sprachwechsel übersetzt eine sichtbare Meldung sofort, ohne den Lauf zu verändern.

## Non-Functional Requirements

- NFR1: Keine Spiellogik in Razor oder Code-Behind. Baum-Operationen (Einfügen, Verschieben, Entfernen, Anzahl ändern, Gültigkeit eines Ziels, Nummerierung, leere Wiederhole-Blöcke, Rundenzähler) liegen im Modul `Game` (Domain bzw. Application) und sind mit xUnit getestet.
- NFR2: Alle Texte via resx im Modul `Game.UI`, DE (Schweizer Rechtschreibung, ohne «ß») und EN, kindgerechter Ton (`localization`-Skill).
- NFR3: Klickziele mindestens 44 px, echte `<button>`-Elemente, sichtbarer Fokus. Alle Klick-Alternativen sind per Tab erreichbar. `prefers-reduced-motion` schaltet Transitionen ab.
- NFR4: Auf 1366×768 sind Palette, Steuerung, Status, Meldung und etwa **8** Programmzeilen ohne Seiten-Scrollen sichtbar. Längere Programme scrollen innerhalb der Liste, nie die Seite. 1920×1080 entsprechend mehr.
- NFR5: Hervorhebungen (aktuell, Fehler, Wiederhole-Rahmen, Auswahl, Einfüge-Linie, ungültiges Ziel) und Meldungsarten sind auch in Graustufen unterscheidbar (Form, Rahmenstil, Symbol).
- NFR6: Native HTML5 Drag & Drop ohne neue NuGet-Pakete oder JS-Bibliotheken (Entscheid Nr. 4, `Architektur C4.md` §6). Keine CDNs, keine fremden Grafiken.
- NFR7: Der Heldenname wird weder gespeichert noch geloggt.
- NFR8: Build, Format, Unit-, bUnit-, Architektur- und E2E-Tests (`HEROPATH_E2E=1`) sind grün.

## Technical Constraints

- Projekte:
  - `HeroPath.Modules.Game` (Domain/Application): Programm als Baum statt `List<MoveCommand>` in `GameSession`, Bearbeitungs-Operationen, Rundenzähler im `Interpreter`/`ExecutionStep`, Vorprüfung auf leere Wiederhole-Blöcke vor dem Lauf, Nummerierung. Die genaue Aufteilung zwischen Domain und Application wird in Phase 2 geplant.
  - `HeroPath.Modules.Game.UI`: `BlockPalette` (Wiederhole, Ziehen, Hinweise), `ProgramEditor` (Baum, Auswahl, Drag & Drop, ▲▼🗑, −/+), neue Meldungskomponente, `GameResources` (DE/EN).
  - `HeroPath.Shared.UI`: ggf. neue Symbole (Wiederhole, Hinweis), `theme.css`.
  - Keine Änderung an `Modules.Levels`, am Host oder an `IAppStorage` erwartet.
- Abhängigkeiten `Shared ← Levels ← Game` bleiben. `ArchitectureTests` und `ClaudeWorkflowTests` bleiben grün (neue öffentliche Typen im `game-rules`-Skill nennen).
- Grenzwerte aus `ProgramLimits` gelten unverändert (1000 Schritte, Anzahl 1 bis 20, Tiefe 2, 200 Bausteine).
- `Command`-Hierarchie bleibt geschlossen (`Architektur C4.md` §6, Nr. 3a). `RepeatCommand` und `ProgramDefinition` bleiben unveränderlich.
- Interpreter: Play und Debug nutzen weiter denselben `Interpreter` (Must aus M1).
- Tests:
  - xUnit: alle Baum-Operationen inkl. ungültiger Ziele (Tiefe 3, eigener Rumpf, gesperrt, voll), ▲▼ an Rumpfgrenzen, Nummerierung, Rundenzähler (auch verschachtelt), Vorprüfung leerer Wiederhole-Block.
  - bUnit: `ProgramEditor` (Auswahl, ▲▼🗑, −/+, Einfügeziel, Sperren), `BlockPalette` (Wiederhole, Sperren, Hinweise), Meldungskomponente (alle Meldungsarten DE/EN, Einzahl/Mehrzahl).
- E2E-Flows (Playwright, DE und EN, desktop 1920×1080 und laptop 1366×768, Screenshots normal + Graustufen):
  1. **Drag & Drop:** Palette → Position im Programm, Umsortieren (auch in und aus Wiederhole), Herausziehen löscht.
  2. **Nur Klick mit Wiederhole:** Wiederhole anhängen, auswählen, Palette-Klicks landen im Rumpf, −/+, ▲▼ (auch aus dem Rumpf heraus), 🗑.
  3. **Lauf mit Wiederhole:** Debug durch einen Wiederhole-Block (Baustein + Wiederhole hervorgehoben, «Runde 2 von 3»), danach Play bis zur Burg mit Erfolgsmeldung.
  4. **Meldungen:** Hindernis (Baum), Rand, Ziel nicht erreicht, leerer Wiederhole-Block (bleibt «Bereit» und bearbeitbar), Verschachtelung abgelehnt. Text in DE und EN geprüft.

## Acceptance Criteria

- [ ] Ein Wiederhole-Block lässt sich per Ziehen und per Klick einfügen, startet mit 3, −/+ ändern die Anzahl zwischen 1 und 20.
- [ ] Bausteine lassen sich per Ziehen an jede gültige Stelle einfügen, umsortieren (auch in und aus Wiederhole-Rümpfen) und durch Ziehen auf die Palette löschen.
- [ ] Jede Drag-&-Drop-Aktion ist auch per Klick möglich (Auswahl, Einfügeziel, ▲▼, 🗑).
- [ ] Ein Wiederhole-Block in der 3. Ebene oder im eigenen Rumpf ist weder per Ziehen noch per Klick möglich, das Kind sieht warum.
- [ ] Bei einem Lauf mit Wiederhole stimmen Hervorhebung und «Runde X von N» in jedem Schritt, bei Play und Debug gleich.
- [ ] Jede Fehlerart und der Erfolg zeigen die Meldung aus FR20 in DE und EN, mit der Baustein-Nummer aus der Liste.
- [ ] Ein leerer Wiederhole-Block verhindert den Start, Status bleibt «Bereit», das Programm bleibt bearbeitbar.
- [ ] Auf 1366×768 sind etwa 8 Programmzeilen samt Palette, Steuerung, Status und Meldung ohne Seiten-Scrollen sichtbar.
- [ ] Alle 4 E2E-Flows grün, Screenshots vom visual-checker ohne [MUST]-Befund geprüft.
- [ ] Build, Format, Unit-, bUnit- und Architekturtests grün.

## Edge Cases & Failure Modes

- Wiederhole in die 3. Ebene oder in den eigenen Rumpf ziehen → «Nein»-Zeichen, Ablegen ohne Wirkung.
- Ablegen ausserhalb jeder Zielzone → nichts passiert, Baustein bleibt, wo er war.
- Ziehen eines Bausteins an seine eigene Position → keine Änderung, kein `Changed`.
- Drag beginnt, dann startet ein Lauf oder Reset (z. B. Doppelklick) → Ablegen hat keine Wirkung, wenn das Programm inzwischen gesperrt ist.
- 200 Bausteine erreicht → Palette gesperrt, Ziehen aus der Palette ohne Wirkung, Hinweis sichtbar. Verschieben bleibt möglich.
- Ausgewählter Baustein wird gelöscht → Auswahl aufgehoben, Einfügeziel «ans Ende».
- ▲ auf dem ersten Baustein des Programms bzw. ▼ auf dem letzten → Knopf gesperrt.
- ▲/▼ eines Bausteins aus einem Rumpf heraus würde einen Wiederhole-Block in die 3. Ebene bringen → nicht möglich (Knopf gesperrt).
- −/+ an den Grenzen 1 bzw. 20 → Knopf gesperrt.
- Mehrere leere Wiederhole-Blöcke → der erste in Programmreihenfolge wird markiert.
- Schrittlimit (z. B. 20 × 20 × 3 Bausteine) → Fehler mit Schrittlimit-Meldung, Baustein markiert, kein Hängen der UI.
- Sprachwechsel bei sichtbarer Meldung → Meldung in der neuen Sprache, Lauf und Programm unverändert.
- Darf nie passieren: Exception aus einer Bearbeitung oder einem Drop erreicht die UI; Play und Debug zeigen unterschiedliche Runden oder Ergebnisse; Programm geht beim Ziehen verloren; Heldenname wird gespeichert.

## Out of Scope

- Tastatur-Drag-&-Drop (Pfeiltasten). Die Klick-Knöpfe sind per Tab erreichbar. → M6 / Could
- Konfetti bzw. Erfolgsanimation → M6 (Feinschliff, Should)
- Rückgängig/Wiederherstellen → offener Punkt in `Projekt_Kontext.md` §8, Entscheid nach dem Probelauf in M6
- Lisa und eigener Heldenname in Meldungen → M4 (Start-Dialog). In M3 fest «Max» über Platzhalter.
- Level-Auswahl, Items auf dem Spielfeld (M4), Level-Editor (M5)
- Touch-Bedienung, MAUI-Host (M7)

## Open Questions

- Keine offenen Fragen. Die englischen Meldungstexte in FR20 sind Vorschläge und werden bei der Freigabe bestätigt oder korrigiert.

## Umsetzungsstand und bekannte Einschränkungen

Stand nach M3 (Code in `src/` und `tests/`). Die Abschnitte oben beschreiben die Anforderung, dieser Abschnitt den tatsächlichen Stand. Die Entscheidungen mit Alternativen stehen in `Architektur C4.md` §6 (Nr. 15 bis 20), der Plan in `plan.md`.

### Abweichungen und Ergänzungen, im Verlauf entschieden

- **Laptop-Layout (NFR4):** Die Seitenleiste ist einspaltig. Unter `@media (max-height: 56rem) and (min-width: 82rem)` (`PlayPage.razor.css`) wird sie zweispaltig (Spalten `0.9fr` / `1.1fr`): links die Werkzeuge (Palette, Steuerung, Status, Meldung), rechts das Programm. Auf 1366×768 zeigt das Layout 10 Zeilen ohne Seiten-Scrollen (Zielwert in NFR4 war «etwa 8»).
- **Kopf eines Wiederhole-Blocks:** Der Kopf bleibt einzeilig («Wiederhole [−] N [+] mal»). ▲ ▼ 🗑 und die Runden-Anzeige («Runde 2 von 3») stehen in einer eigenen Fusszeile (`block-foot`) unter dem Kopf. Das Wiederhole-Symbol zeigt nur ein Wiederhole-Block der obersten Ebene (`Path.Count == 1`), kein verschachtelter. Bei einem Bewegungs-Baustein stehen ▲ ▼ 🗑 direkt in seiner Zeile.
- **Hinweis bei leerem Programm (FR22):** Der Text «Zieh Bausteine ins Programm oder klick sie an.» (`Message_EmptyProgram`) steht nur im Meldungsfeld (`MessageBox`). Er nennt beide Wege und weicht damit vom Vorschlag «Zieh zuerst ein paar Bausteine ins Programm.» ab. Die Programmspalte zeigt bei leerem Programm eine Platzhalter-Zeichnung ohne Text (`program-placeholder`), damit kein Text doppelt steht. Der Schlüssel `Program_Empty` und das Test-Id `program-empty` aus M2 sind entfernt.
- **Rangfolge der Meldungen (`GameSession.Message`):** Meldung des Laufs (Fehler oder Erfolg) vor dem Hinweis zum leeren Wiederhole-Block vor den abgeleiteten Hinweisen. Abgeleitet werden, nur solange das Programm bearbeitbar ist: `EmptyProgram`, `ProgramFull`, `NestingTooDeep` (in dieser Reihenfolge). Die abgeleiteten Hinweise werden bei jedem Lesen neu berechnet und müssen nicht gelöscht werden. Der Hinweis zum leeren Wiederhole-Block wird durch die nächste Bearbeitung und durch Reset gelöscht. `Load` mit einem anderen Grid und Reset löschen Hinweis und Meldung des Laufs.
- **Leerer Wiederhole-Block (FR21):** Die Vorprüfung liegt im `ExecutionRunner` (`PlayAsync` und `StepOnce`, nur im Status `Ready`). Bei einem Treffer wird kein `Interpreter` erstellt, der Status bleibt `Ready`, `GameSession.HintedId` markiert den ersten leeren Block (`data-hint`). Play und Debug bleiben dafür aktiv (`CanStart` unverändert). `ExecutionError.EmptyRepeat` und `EmptyProgram` des Interpreters sind damit im normalen Ablauf kaum erreichbar. `GameMessage.FromStep` bildet sie trotzdem als Hinweis ab (Schutznetz).
- **Drag & Drop (FR6 bis FR10, NFR6):**
  - Es werden nur Blazor-Ereignisse verwendet (`@ondragstart`, `@ondragover:preventDefault="…"`, `@ondrop`), kein eigenes JavaScript (Entscheid D6, Variante A, `Architektur C4.md` §6).
  - **Firefox ist unverifiziert:** Firefox startet einen Drag eventuell nur, wenn `dataTransfer.setData` aufgerufen wird, und Blazor kann das ohne JS nicht. Weder im Code noch in `docs/` ist das geprüft. Zu prüfen beim Probelauf (M6) auf dem Firmenbrowser. Die Klick-Alternative funktioniert unabhängig davon. E2E läuft nur in Chromium.
  - Ungültige Ziele: Die eigene Position (keine Änderung), der eigene Rumpf und die 3. Ebene sind ungültig und zeigen beim Darüberfahren das «Nein»-Zeichen mit Schraffur (`zone-no`, `zone-hatch`). Das Ablegen bewirkt nichts.
  - Ziehen ändert die Auswahl nie.
  - Die Palette ist das Löschziel. Ein Feld «Hierher ziehen zum Löschen» (`palette-delete`, `Palette_DropDelete`) erscheint nur, solange ein bestehender Baustein gezogen wird.
  - Die Drop-Zonen (`DropZone`) sind `aria-hidden` und nicht fokussierbar. Die Tastaturbedienung deckt die Klick-Alternative ab (Auswahl, ▲▼🗑).
  - `ProgramDragState` hält nur die Quelle. Jede Zielfrage geht an `GameSession` (`CanAddAt`, `CanAddRepeatAt`, `CanMove`, `CanRemove`), jedes Ablegen wird erneut geprüft. Ein Drag bei gesperrtem Programm startet nicht (`Start` bricht ab).
  - `PlayPage.Dispose` beendet einen laufenden Drag (`Drag.End()`), zusätzlich zu `Runner.Stop()` aus M2.
- **E2E-Tests:** Die Drag-Tests (`DragDropTests`) verwenden eine manuelle Mausfolge und warten, bis die Zonen-Boxen stabil sind, weil die Zonen nach `dragstart` wachsen. Der E2E-Test für das Schrittlimit nutzt die Playwright-Browser-Uhr statt echter Wartezeit. `MessageTests` dauert trotzdem etwa 1.5 Minuten.
- **`ExecutionStep.Repeats`:** Die Liste der umschliessenden Wiederhole-Blöcke (`RepeatProgress(RepeatId, Round, Count)`, aussen nach innen) ist eine `init`-Eigenschaft. Die `record`-Gleichheit von `ExecutionStep` vergleicht sie per Referenz, nicht nach Inhalt. Im Doc-Kommentar vermerkt.
- **Meldungstexte:** `MessageBox` setzt den Heldennamen als feste Konstante «Max» ein (`HeroName`). Er wird nicht gespeichert und nicht geloggt (NFR7). `GameMessage` selbst enthält nur Werte, keine Texte und keinen Namen.

### Offen (nicht umgesetzt)

- **Aus dem Code-Review, [COULD], nicht behoben:**
  - Der Snapshot eines Schritts alloziert pro Schritt.
  - `DropZone.ParentNumber` verwendet den Zeichenwert `"root"` als Magic String.
  - Redundantes `ArgumentNullException.ThrowIfNull` in `ProgramEditing.UpTarget` und `DownTarget`.
  - `MessageBox` fällt bei `Obstacle` ohne Hindernisart auf `ObstacleKind.Tree` zurück (`?? ObstacleKind.Tree`).
  - Unnötiges `foreach`-Capture (`var captured = direction`) in `BlockPalette`.
  - `ProgramBlock` prüft die Grenzen 1 bis 20 im Kopf selbst (`MinRepeatCount`/`MaxRepeatCount`) und dupliziert damit die Regel aus `ProgramEditing.SetRepeatCount`.
  - `GameSession.Message` und `DerivedHint` werden bei jedem Lesen neu berechnet.
  - `GameMessage.FromStep` für `EmptyRepeat` und `EmptyProgram` ist im normalen Ablauf kaum erreichbar (Schutznetz des Interpreters).
  - Einige UI-Klassen haben keinen XML-Doc-Kommentar.
- **Visuell [COULD], nicht behoben:**
  - Am linken Ende des schraffierten ungültigen Streifens steht ein kleiner Balkenstummel.
  - Läuft die Programmliste über, gibt es keinen Hinweis zum Scrollen.
  - Der überfahrene ungültige Streifen überlappt die Unterkante der Zeile darüber. Das ist beabsichtigt.
- **Verschoben (Out of Scope):**
  - Tastatur-Drag-&-Drop (M6, Could).
  - Konfetti (M6).
  - Rückgängig/Wiederherstellen (offener Punkt in `Projekt_Kontext.md` §8, Entscheid nach dem Probelauf).
  - Heldenname und Lisa in den Meldungen (M4). Bis dahin fest «Max».
- **Firefox-Drag:** Siehe oben, unverifiziert, Prüfung beim Probelauf (M6).
- **Sicherheit:** Der Sicherheits-Review ergab keine Befunde. Info: Sobald M4 den Heldennamen in die Meldungen einsetzt, ist er erneut zu prüfen (nicht speichern, nicht loggen).
