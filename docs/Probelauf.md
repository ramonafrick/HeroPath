# Probelauf auf dem Ziel-Laptop

Prüfliste für M6. Die Liste ist für einen Durchgang von etwa 45 Minuten auf **einem** Firmenlaptop gedacht, wenn möglich mit externem Bildschirm, Maus und Tastatur wie am Zukunftstag. Bitte die Spalte «Ergebnis» ausfüllen (ok oder Beschreibung) und diese Datei danach committen. M6 ist erst abgeschlossen, wenn alle Funde behoben oder bewusst als Einschränkung festgehalten sind.

## Angaben zum Laptop

| Angabe | Wert |
|---|---|
| Datum, Prüfperson | |
| Modell, Windows-Version | |
| Browser und Version | |
| Auflösung und Skalierung (Einstellungen > Anzeige) | |
| Externer Bildschirm (Auflösung) | |
| Netz am Zukunftstag vorhanden? | |

## Vorbereitung

1. Prüfen, ob auf `main` die CI grün ist.
2. ZIP holen: GitHub > Actions > «Package» > «Run workflow». Nach dem Lauf steht unten beim Lauf das Artefakt mit dem ZIP. Das ZIP auf den Laptop kopieren.

## Weg 1: Online (GitHub Pages)

| Nr. | Schritt | Erwartet | Ergebnis |
|---|---|---|---|
| 1.1 | `https://ramonafrick.github.io/HeroPath/` öffnen | Startseite «HeldenWeg» erscheint | |
| 1.2 | Beim ersten Laden Sekunden stoppen, vom Eingeben der Adresse bis zur Startseite | Zeit notieren, Ziel höchstens 15 s | |
| 1.3 | Seite neu laden | Deutlich schneller | |
| 1.4 | `https://ramonafrick.github.io/HeroPath/editor` direkt öffnen | Editor erscheint | |
| 1.5 | Sprache auf EN, Seite neu laden | Bleibt Englisch | |

## Weg 2: Offline (ZIP)

| Nr. | Schritt | Erwartet | Ergebnis |
|---|---|---|---|
| 2.1 | ZIP entpacken, `LIESMICH.txt` lesen | Umlaute korrekt, Anleitung verständlich | |
| 2.2 | WLAN und Kabel trennen | Kein Netz | |
| 2.3 | `Start.bat` doppelklicken | Warnung von Windows? Text notieren. Schwarzes Fenster, danach Browser mit dem Spiel | |
| 2.4 | Falls blockiert: Meldung notieren, Hinweise aus `LIESMICH.txt` befolgen | Weg zum Start oder Hinweis auf Weg 1 | |
| 2.5 | Ein Level durchspielen | Funktioniert ohne Netz | |
| 2.6 | Schwarzes Fenster schliessen, im Browser neu laden | Spiel ist nicht mehr erreichbar | |
| 2.7 | Nochmals starten | Gleicher Anschluss, eigene Levels noch da. Bei einem anderen Anschluss sind sie nicht sichtbar, siehe unten | |

## Spiel

Den Rest bitte auf dem Weg ausführen, der am Zukunftstag gilt.

| Nr. | Schritt | Erwartet | Ergebnis |
|---|---|---|---|
| 3.1 | Max und Lisa wählen, Namen ändern | Name erscheint in der Meldung beim Erfolg | |
| 3.2 | Alle sieben Level per Maus lösen | Alle lösbar, am Ende «alle Level geschafft» | |
| 3.3 | Bausteine per Drag & Drop einfügen, verschieben, löschen, verschachteln | Funktioniert flüssig | |
| 3.4 | Dasselbe nur mit Klicks | Funktioniert | |
| 3.5 | Play, Debug, Reset, Tempo langsam und schnell | Bewegung flüssig, Hervorhebung stimmt | |
| 3.6 | Gegen Hindernis laufen, Spielfeld verlassen, Ziel nicht erreichen | Kindgerechte Meldung, Feld markiert | |
| 3.7 | Erdbeere und Pizza einsammeln | Items verschwinden mit kleiner Animation, Zähler stimmt | |
| 3.8 | Level lösen | Konfetti etwa 2 s, «Nächstes Level» klickbar | |
| 3.9 | In Windows Animationen ausschalten (Barrierefreiheit), Level lösen | Kein Konfetti, ruhiger goldener Rahmen | |
| 3.10 | Sprache während des Spiels wechseln | Name, Programm und Level bleiben | |

## Sound

| Nr. | Schritt | Erwartet | Ergebnis |
|---|---|---|---|
| 4.1 | Beim ersten Start | Ton ist aus | |
| 4.2 | Ton einschalten, Level lösen | Kurzer, angenehmer Erfolgston | |
| 4.3 | Fehler auslösen, Item einsammeln | Je ein eigener Ton | |
| 4.4 | Lautstärke im Raum beurteilen | Stört nicht, oder Standard «aus» reicht | |
| 4.5 | Neu laden, dann Level lösen | Einstellung bleibt, Ton klingt | |

## Tastatur

| Nr. | Schritt | Erwartet | Ergebnis |
|---|---|---|---|
| 5.1 | Level nur mit Pfeilen, Leertaste, D und R lösen | Funktioniert | |
| 5.2 | Baustein per Tab und Enter wählen, Shift+Pfeil hoch/runter, Entf, Esc | Verschieben, Löschen, Auswahl aufheben | |
| 5.3 | Knopf «Tasten» öffnen | Liste verständlich und vollständig, in DE und EN | |
| 5.4 | Am Laptop ohne externen Bildschirm prüfen | Funktioniert auch mit Laptop-Tastatur | |

## Level-Editor

| Nr. | Schritt | Erwartet | Ergebnis |
|---|---|---|---|
| 6.1 | Level bauen, speichern, spielen | Erscheint unter «Meine Levels» | |
| 6.2 | Neu laden | Level ist noch da, Held und Name sind zurückgesetzt | |
| 6.3 | Level exportieren | Download erlaubt? Datei liegt im Download-Ordner | |
| 6.4 | Level löschen, Datei importieren | Level ist wieder da | |
| 6.5 | Im Editor mit ungespeicherten Änderungen den Zurück-Knopf des Browsers drücken | Rückfrage oder bekanntes Verhalten notieren | |
| 6.6 | 20×20-Level, schnell auf Felder klicken | Kein spürbares Warten (Ziel: Anzeige in 200 ms) | |

## Darstellung

| Nr. | Schritt | Erwartet | Ergebnis |
|---|---|---|---|
| 7.1 | Am externen Bildschirm prüfen | Spielfeld gut lesbar, Seite scrollt nicht | |
| 7.2 | Am Laptop-Bildschirm prüfen, besonders bei Skalierung 125 % oder 150 % | Alles erreichbar. Kleine Darstellung und Scrollen notieren | |
| 7.3 | Im Editor auf dem Laptop-Bildschirm speichern | Spalten laufen nicht über | |

## Optional: Firefox

| Nr. | Schritt | Erwartet | Ergebnis |
|---|---|---|---|
| 8.1 | Falls Firefox vorhanden: Drag & Drop testen | Funktioniert, oder Klick-Alternative reicht | |

## Klärung mit der IT

| Frage | Antwort |
|---|---|
| Darf `Start.bat` laufen (PowerShell mit lokalem Server auf `localhost`)? | |
| Ist `ramonafrick.github.io` aus dem Firmennetz erreichbar? | |
| Sind Downloads für den Export erlaubt? | |

## Bekannte Punkte

- Eigene Levels liegen im Browser pro Adresse. Wählt der lokale Server einen anderen Anschluss, weil 5080 belegt ist, sind sie dort nicht sichtbar. Online und offline sind ebenfalls getrennt.
- Die Schwelle von 15 Sekunden in 1.2 ist ein Vorschlag und wird nach der ersten Messung bestätigt oder angepasst.

## Funde

| Nr. | Beschreibung | Schwere (muss / soll / kann) | Entscheid |
|---|---|---|---|
| | | | |

## Entscheide nach dem Probelauf

| Thema | Entscheid |
|---|---|
| Rückgängig/Wiederherstellen im Programm-Editor | |
| Tipps oder Auflösung beim Spielen | |
