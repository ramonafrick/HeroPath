# HeroPath – Projektidee

> Projektname: **HeroPath** (deutscher Anzeigename: «HeldenWeg», englisch: «Hero's Path»). Die Figur steuert man über `Person.Move(...)`.

## Story

Max und Lisa sind die Helden. Sie machen sich auf den Weg zur Burg. Dabei müssen sie Bäumen, Steinen und Teichen ausweichen und unterwegs Erdbeeren und Pizza einsammeln. Wer ihnen den Weg programmiert, hilft ihnen, die Burg zu erreichen.

## Kurzbeschreibung

HeroPath ist eine Lern-Webanwendung für den Browser. Kinder wählen einen Helden (Max oder Lisa), können ihm einen eigenen Namen geben und programmieren ihn, indem sie Bausteine per Drag & Drop zu einem Programm zusammenstecken. Die Figur läuft damit über ein frei wählbares Spielfeld, weicht Hindernissen aus, sammelt Items ein und erreicht das Ziel. Läuft sie in ein Hindernis oder aus dem Spielfeld, gibt es eine kindgerechte Fehlermeldung.

## Anlass und Ziel

- **Anlass:** Zukunftstag. Kinder schnuppern einen halben Tag (ca. 4 Stunden) in der Software-Abteilung.
- **Ziel:** Ein erster, spielerischer Kontakt mit dem Programmieren. Die Kinder erleben, wie sich ein Programm aus einzelnen Befehlen zusammensetzt und wie man Fehler findet.
- **Zusatzziel:** Mädchen und Buben für den Entwicklerberuf begeistern. Darum gibt es zwei Figuren, eine Burg als Ziel und lustige Items.

### Lernziele

1. **Sequenz:** Die Reihenfolge der Befehle bestimmt das Ergebnis.
2. **Wiederholung:** Mit dem Wiederhole-Block spart man Arbeit.
3. **Debugging:** Mit dem Debug-Modus findet man den Fehler Schritt für Schritt.
4. **Problemzerlegung:** Ein langer Weg wird in kleine Schritte aufgeteilt.
5. **Kreativität:** Mit dem Level-Editor baut man eigene Aufgaben.

## Zielgruppe und Rahmen

| Punkt | Festlegung |
|---|---|
| Alter | 11 bis 13 Jahre, keine Vorkenntnisse |
| Geräte | Laptop, angeschlossen an Bildschirm, Maus und Tastatur (kein Touch nötig) |
| Dauer | ca. 4 Stunden, die App ist ein Teil davon |
| Betreuung | Software Engineers begleiten die Kinder |
| Sprachen | Deutsch (Schweizer Rechtschreibung) und Englisch, Umschalter oben rechts |

## Spielablauf

1. Das Kind wählt einen **Helden** (Max oder Lisa). Der Name ist vorausgefüllt und kann überschrieben werden, wenn das Kind seinem Helden einen eigenen Namen geben möchte.
2. Es wählt ein **Level** (Vorlage oder eigenes Level).
3. Es zieht **Bausteine** aus der Palette ins Programm.
4. Es startet mit **Play** (alles auf einmal) oder **Debug** (Schritt für Schritt).
5. Die Figur läuft. Bei einem Fehler zeigt die App, welcher Baustein das Problem verursacht.
6. **Reset** setzt alles zurück an den Start.

## Spielelemente

| Element | Darstellung |
|---|---|
| Held Max | Männlein mit gelbem T-Shirt und blauer Hose (Standardname «Max») |
| Heldin Lisa | Männlein mit rotem Kleid (Standardname «Lisa») |
| Startfeld | Grüne Fläche mit Fussabdruck-Muster |
| Zielfeld | Prinzessinnenburg auf goldenem Schachbrettmuster |
| Hindernisse | Baum, Stein, Teich (weitere möglich) |
| Items | Erdbeere (Standard), Pizza-Stück (Bonus) |

Start und Ziel unterscheiden sich durch **Farbe und Muster**, damit man sie auch ohne Farbwahrnehmung sicher erkennt. Alle Grafiken werden selbst als SVG gezeichnet.

Weitere Ideen für später: Themenwelten mit anderen Hindernissen und Items (z. B. Fussballplatz mit Bällen).

## Bausteine

| Baustein | Wirkung |
|---|---|
| Hoch | Figur geht ein Feld nach oben |
| Runter | Figur geht ein Feld nach unten |
| Links | Figur geht ein Feld nach links |
| Rechts | Figur geht ein Feld nach rechts |
| Wiederhole [N] mal | Führt die enthaltenen Bausteine N-mal aus (verschachtelbar, maximal 2 Ebenen) |

Die Steuerung ist **absolut** (Hoch, Runter, Links, Rechts). Das ist für den Einstieg am einfachsten, weil man sich nicht in die Figur hineindrehen muss.

## Steuerung

| Knopf | Funktion |
|---|---|
| **Play** | Führt das ganze Programm automatisch aus |
| **Debug** | Führt pro Klick einen Schritt aus, der aktuelle Baustein wird hervorgehoben |
| **Reset** | Stoppt und setzt Figur und Items zurück |
| **Tempo** | Regler für die Geschwindigkeit der Animation |

Eine Statusanzeige zeigt: Bereit, Läuft, Pause (Debug), Fehler, Ziel erreicht.

## Fehler und Meldungen (kindgerecht)

| Situation | Meldung (Deutsch) |
|---|---|
| Hindernis | «Autsch! Da steht ein Baum im Weg. Schau dir Schritt 6 noch einmal an.» (Baum, Stein oder Teich je nach Hindernis) |
| Spielfeldrand | «Hoppla! Hier ist die Welt zu Ende. Schritt 6 führt aus dem Spielfeld.» |
| Programm leer | «Zieh zuerst ein paar Bausteine ins Programm.» |
| Wiederhole-Block leer | «Leg zuerst Bausteine in den Wiederhole-Block.» (Block wird markiert, Lauf startet nicht) |
| Programm zu Ende, Ziel nicht erreicht | «Dein Programm ist fertig, aber Lisa ist noch nicht an der Burg. Probier es nochmal!» |
| Erdbeeren fehlen (falls Pflicht, Pizza zählt nicht) | «Fast geschafft! Es fehlen noch 2 Erdbeeren.» |
| Erfolg | «Geschafft, Lisa! Du hast die Burg erreicht.» |

Alle Texte gibt es auch auf Englisch.

## Level

### Vorlagen (Startvorschlag)

1. **Freies Feld:** 10×10 ohne Hindernisse, zum Ausprobieren.
2. **Einfach 1:** ein Hindernis zwischen Start und Ziel.
3. **Einfach 2:** zwei Hindernisse.
4. **Erdbeeren sammeln:** wenige Items auf dem Weg.
5. **Kleines Labyrinth:** drei Felder vor, zwei hoch, eins runter, man muss sich durchschlängeln.
6. **Lange Strasse:** ideal für den Wiederhole-Block.
7. **Burg-Labyrinth:** grösseres Feld für Geschickte.

### Level-Editor

- Spielfeldgrösse einstellbar (Breite und Höhe, z. B. 3 bis 20), Standard ist 10×10.
- Hindernisse setzen und entfernen, Start- und Zielfeld festlegen, Items platzieren.
- Eigene Levels im Browser speichern und wieder laden.

## Nicht-Ziele (Out of Scope)

- Backend, Benutzerkonten, Bestenlisten, Mehrspielerbetrieb
- Relative Steuerung (vorwärts, drehen) und Wenn-Blöcke (möglich als spätere Erweiterung)
- Schritt zurück im Debug-Modus
- Mobile App (.NET MAUI) und Touch-Optimierung. Beides ist später möglich, die Architektur ist darauf vorbereitet

## Name

**HeroPath** ist der Projektname (Repository, Namespaces). In der Oberfläche wird der Anzeigename mit der Sprache umgeschaltet:

| Sprache | Anzeigename |
|---|---|
| Deutsch | HeldenWeg |
| Englisch | Hero's Path |

Der Code-Begriff `Person.Move` bleibt bestehen.

## Veröffentlichung

- GitHub: `github.com/ramonafrick/HeroPath`
- Lizenz: **MIT**
- Alle Grafiken, Texte und Aufgaben sind selbst erstellt.
