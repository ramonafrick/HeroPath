# Plan: M6 – Deutsch und Englisch, Feinschliff, Probelauf auf dem Ziel-Laptop

**Date**: 2026-11-04 · Grundlage: `docs/requirements.md` (freigegeben) · Vorgänger: `plan-M5.md`

## Überblick

Die Reihenfolge stellt die Auslieferung nach vorne, weil das Datum des Zukunftstags offen ist: Nach S3 ist die App einsetzbar, der Feinschliff lässt sich danach jederzeit abbrechen. Die Textdurchsicht (S7) kommt erst nach Konfetti, Sound und Tastatur, damit sie deren neue Texte mit abdeckt. Die Prüfliste (S8) kommt zuletzt, damit sie den fertigen Stand beschreibt.

| Slice | Name | Anforderungen | Betroffen | Skills |
|---|---|---|---|---|
| S1 | CI | FR1, NFR6 | `.github/workflows`, Repo-Dateien | `release`, `e2e-playwright` |
| S2 | GitHub Pages | FR2, NFR1, NFR6 | `.github/workflows`, `Hosts/HeroPath.Web` | `release` |
| S3 | ZIP für die Laptops | FR3, FR4, NFR1 | `deploy/`, `.github/workflows` | `release` |
| S4 | Erfolgsanimation | FR10 bis FR12, NFR2, NFR3 | `Game.UI`, `Levels.UI`, E2E | `blazor-ui`, `svg-assets`, `e2e-playwright` |
| S5 | Sound | FR13 bis FR16, NFR2 bis NFR5 | `Shared`, `Shared.UI`, `Game`, `Hosts/HeroPath.Web` | `blazor-ui`, `game-rules`, `localization` |
| S6 | Tastatur | FR17 bis FR21, NFR4 | `Shared`, `Game`, `Game.UI`, `Hosts/HeroPath.Web` | `blazor-ui`, `game-rules`, `localization` |
| S7 | Sprache | FR7 bis FR9 | `Shared`, `Shared.UI`, `Hosts/HeroPath.Web`, resx | `localization` |
| S8 | Prüfliste Probelauf | FR5, FR6, NFR7 | `docs/Probelauf.md` | `release` |

Abhängigkeiten: S2 und S3 bauen auf S1 auf. S4 bis S6 sind unabhängig voneinander. S7 folgt nach S4 bis S6, S8 nach allen anderen. Ein neues Modul ist nicht nötig. `Shared ← Levels ← Game` bleibt unverändert.

## Entscheide

Für jeden Entscheid steht die gewählte Option an erster Stelle. Der Plan wurde am 2026-11-04 freigegeben.

### E1: Lokaler Server im ZIP (freigegeben: Option A)

| Option | Vorteile | Nachteile |
|---|---|---|
| **A: PowerShell-Skript mit `HttpListener` (Empfehlung)** | Kein neues Projekt und keine Binärdatei. Die IT kann den Text lesen. ZIP bleibt klein. Teil von Windows | Eigener kleiner HTTP-Code, Pfadprüfung muss getestet werden. Scheitert unter Application Control, weil `HttpListener` dort kein erlaubter Typ ist |
| B: Selbstenthaltene EXE (ASP.NET Core) | Bewährter Webserver, keine eigene HTTP-Logik | Neues Projekt, Grösse (Test hier: eine Konsolen-EXE allein 70 MB, ASP.NET Core kommt dazu). Unsignierte EXE wird von Application Control und Smart App Control blockiert |
| C: Beides im ZIP | Zwei Chancen auf dem Laptop | Doppelter Aufwand. Eine strenge Richtlinie blockiert beide |

Ergebnis des Tests auf diesem Rechner (Windows 11, Smart App Control aktiv, kein Admin): Ein unsigniertes Skript läuft in `FullLanguage`, `HttpListener` auf `localhost` startet, die EXE läuft ebenfalls. Der Test unterscheidet die Varianten also nicht. **Für die Ziel-Laptops ist keine Variante bewiesen.** Unter einer strengen Richtlinie (AppLocker, App Control for Business) scheitern A und B, dann bleibt nur Pages. Das ist der Grund für die Empfehlung: A kostet am wenigsten, und die Rückfallebene ist bei beiden gleich.

Quellen: [about_Language_Modes](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_language_modes?view=powershell-5.1) (unter AppLocker oder WDAC läuft PowerShell in `ConstrainedLanguage`, nur erlaubte Typen), [Smart App Control](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/overview) («unknown, unsigned code are blocked by default»), `CLAUDE.md` «Known pitfalls». Unverifiziert: die tatsächliche Richtlinie der Ziel-Laptops.

### E2: Tastatur abfangen (freigegeben: Option A)

| Option | Vorteile | Nachteile |
|---|---|---|
| **A: Listener auf `document` in einem JS-Modul des Hosts, hinter einer Schnittstelle (Empfehlung)** | Wirkt unabhängig vom Fokus, auch wenn der fokussierte Knopf gerade gelöscht wurde. Entscheidet pro Taste, ob `preventDefault` nötig ist | Ein neues JS-Modul und eine neue Schnittstelle für einen späteren MAUI-Host |
| B: `@onkeydown` auf dem Seitenelement mit `tabindex="-1"` | Kein JS | Wirkt nur mit Fokus im Element. `preventDefault` steht als Direktive am Element und lässt sich nicht pro Taste im Handler entscheiden: Pfeiltasten scrollen, oder alle Tasten sind gesperrt |

Quelle: [Blazor event handling, «Prevent default actions»](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling?view=aspnetcore-10.0). Der Filter (Ziel, Wiederholung, Modifikatoren) läuft deshalb synchron in JS, die Zuordnung Taste zu Aktion in C#.

### E3: Tastenbelegung (freigegeben)

Belegung:

| Taste | Aktion |
|---|---|
| ← ↑ → ↓ | Baustein einfügen |
| Shift + ↑ / ↓ | Ausgewählten Baustein verschieben |
| Entf oder Rücktaste | Ausgewählten Baustein löschen |
| Esc | Auswahl aufheben |
| Leertaste | Play |
| D | Debug-Schritt |
| R | Reset |

Regeln: Eine gedrückt gehaltene Taste zählt einmal (`event.repeat` wird ignoriert), damit keine Serien entstehen. Mit Ctrl, Alt oder Meta wirkt kein Kürzel, damit Browser-Tasten wie Alt+← und Ctrl+R frei bleiben. Liegt der Fokus auf einem Knopf, gehören Leertaste und Enter diesem Knopf. Einen Wiederhole-Block fügt die Tastatur nicht direkt ein (nicht verlangt). Das geht über Tab und den Knopf der Palette.

### E4: Sound erzeugen (freigegeben mit dem Plan)

**A: Web Audio mit Oszillatoren in `js/sound.js` (Empfehlung):** keine Audiodateien, damit garantiert selbst erstellt, wenige Zeilen. B: WAV-Dateien im Repo wären grösser und bräuchten einen Erzeugungsschritt. `AudioContext` entsteht beim ersten Ton und wird mit `resume()` geweckt. Nach einem Neuladen mit eingeschaltetem Ton ist er vor der ersten Interaktion blockiert, aber erste Töne folgen ohnehin auf einen Klick oder Tastendruck. Quelle: [Chrome Autoplay Policy](https://developer.chrome.com/blog/autoplay).

### E5: Konfetti (freigegeben mit dem Plan)

**A: Nur CSS, sichtbar genau solange `Session.Status == Succeeded` (Empfehlung):** kein Timer und kein JS. Reset und Levelwechsel entfernen das Element. Ein Sprachwechsel startet die Animation nicht neu, weil dasselbe DOM-Element bleibt (FR12). Nach dem Ende bleibt es unsichtbar und ohne Klickfläche stehen. B: Ein C#-Timer bräuchte eigene Aufräumlogik.

### E6: Pages und Workflows (freigegeben mit dem Plan)

- `404.html` als Kopie von `index.html` wie im `release`-Skill. Das ist einfacher als das Redirect-Skript der [Microsoft-Anleitung](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/github-pages?view=aspnetcore-10.0). Der Direktlink wird nach der Aktivierung auf der echten Seite geprüft, sonst wird auf das Redirect-Skript gewechselt.
- `base href` per `sed` im Workflow statt mit der Drittanbieter-Action aus der Anleitung (NFR6).
- `.nojekyll` wird in den Publish-Ordner geschrieben. Die Microsoft-Anleitung nennt das Risiko von Integritätsfehlern durch Zeilenenden: Hier entstehen alle Dateien erst beim Publish im Workflow, und `.gitattributes` normalisiert schon auf LF. Es ist deshalb kein `*.js binary` nötig.
- Deploy nur nach grüner CI (`workflow_run`), zusätzlich `workflow_dispatch`. Bis Pages aktiviert ist, schlägt der Deploy-Schritt fehl. Das ist erwartet.
- Action-Versionen geprüft mit `git ls-remote` am 2026-11-04: `checkout@v7`, `setup-dotnet@v6`, `upload-artifact@v7`, `upload-pages-artifact@v5`, `deploy-pages@v5`.

## Slices

### S1: CI

- **Ziel:** Jeder Push auf `main` prüft Format, Build, alle Tests und E2E.
- **Dateien:** `.github/workflows/ci.yml`, `.claude/skills/release/SKILL.md` (Versionen, Tatsachen).
- **Vorgehen:** `ubuntu-latest` wie im `release`-Skill. Rechte `contents: read`. Screenshots als Artefakt, auch bei Fehlern. Falls Chromium auf Linux Systembibliotheken braucht, `playwright.ps1 install --with-deps chromium` ergänzen.
- **Tests:** Lauf auf GitHub. Pfade und Gross-/Kleinschreibung fallen erst unter Linux auf, Funde werden hier behoben.
- **Akzeptanz:** CI auf `main` grün, Artefakt `screenshots` abrufbar.
- **Abhängigkeiten:** keine. Braucht einen Push.

### S2: GitHub Pages

- **Ziel:** App läuft unter `https://ramonafrick.github.io/HeroPath/`.
- **Dateien:** `.github/workflows/pages.yml`, Skill `release`.
- **Vorgehen:** Publish, Base-Href, `404.html`, `.nojekyll`, Prüfschritt ohne externe URLs in HTML und CSS (NFR1), dass `js/file-download.js` vorhanden ist. Rechte `pages: write`, `id-token: write`.
- **Tests:** Prüfschritt im Workflow. Nach der Aktivierung: Aufruf der Seite, Direktlinks, Sprachwechsel, Export.
- **Akzeptanz:** Workflow baut das Artefakt. Nach der Aktivierung durch die Projektverantwortliche sind die Kriterien aus `requirements.md` erfüllt.
- **Abhängigkeiten:** S1. Das Repository ist öffentlich (Aufruf ohne Anmeldung: HTTP 200). Pages ist damit im Gratis-Plan möglich, die offene Frage aus `requirements.md` entfällt.

### S3: ZIP für die Laptops

- **Ziel:** ZIP entpacken, `Start.bat` doppelklicken, spielen, ohne Internet und ohne installiertes .NET.
- **Dateien (Option A):** `deploy/Start.bat`, `deploy/Start.ps1`, `deploy/LIESMICH.txt`, `deploy/build-zip.ps1`, `deploy/smoke-test.ps1`, `.github/workflows/package.yml`, `.gitattributes`, `docs/Architektur C4.md` §9 (Eintrag `HeroPath.Host` entfällt, `deploy/` neu), Skill `release`.
- **Vorgehen:**
  - `Start.ps1` bindet nur an `localhost`, sucht einen freien Port ab 5080, liefert die MIME-Typen für `.wasm`, `.dat`, `.json`, `.js`, `.css`, `.svg`, `.html`, fällt für Routen auf `index.html` zurück und öffnet den Standardbrowser. Pfade werden aufgelöst und müssen unter `wwwroot` liegen.
  - `Start.bat` ruft `powershell -NoProfile -ExecutionPolicy Bypass -File Start.ps1` auf und zeigt bei einem Fehler einen klaren deutschen Hinweis mit dem Pages-Weg.
  - `.gitattributes` bekommt `*.bat text eol=crlf`, weil `eol=lf` Batch-Dateien stören kann. PowerShell 5.1 liest Skripte ohne BOM nicht als UTF-8: `Start.ps1` bleibt ASCII, `LIESMICH.txt` wird mit BOM und CRLF geschrieben.
  - `package.yml` läuft auf `windows-latest`, bei `workflow_dispatch` und Tags `v*`. Bei einem Tag hängt es das ZIP mit dem vorinstallierten `gh` an den Release (`contents: write`).
- **Tests:** `smoke-test.ps1` startet den Server, prüft Statuscode und MIME-Typ für `index.html` und `.wasm`, den Fallback auf `index.html` für `/editor`, die Ablehnung von `..`-Pfaden, und dass nur `localhost` lauscht. Läuft lokal und im Workflow. Einmal manuell mit Playwright oder Browser gegen das entpackte ZIP spielen (E2E-Fixture bleibt unverändert).
- **Akzeptanz:** ZIP hier entpackt, offline gestartet, Level durchgespielt. Grösse festgehalten.
- **Abhängigkeiten:** S1. Option C oder B ändert die Dateiliste, nicht die Reihenfolge.

### S4: Erfolgsanimation

- **Ziel:** Etwa 2 Sekunden Konfetti im Spielfeld beim Erfolg.
- **Dateien:** `SuccessConfetti.razor(.cs/.css)` in `Game.UI`, `GameBoard.razor(.cs)` bekommt einen Parameter `OverlayContent` (Inhalt über dem Spielfeld, Spielseite füllt ihn), `PlayPage.razor`, Skill `blazor-ui`, `tests/HeroPath.E2E/Infrastructure/AppFixture.cs` (Option für normale Bewegung).
- **Vorgehen:** Fest berechnete Teile (kein Zufall in Tests), `aria-hidden`, `pointer-events: none`, `overflow: hidden`. Die Meldung sagt den Erfolg bereits an. Bei reduzierter Bewegung statt der Teile ein ruhiger Farbakzent.
- **Tests:**
  - bUnit: sichtbar genau bei `Succeeded`, weg nach Reset und Levelwechsel, gleiches Element nach Sprachwechsel.
  - E2E `SuccessAnimationTests`: Knopf «Nächstes Level» klickbar, während die Animation läuft.
  - Screenshots: `success-confetti` (normale Bewegung), `success-reduced-motion`, je DE und EN, normal und Graustufen.
- **Akzeptanz:** FR10 bis FR12.
- **Abhängigkeiten:** keine.

### S5: Sound

- **Ziel:** Abschaltbare, kurze Töne bei Erfolg, Fehler und Item.
- **Dateien:**
  - `Shared`: `ISoundPlayer`, `SoundEffect`, `SoundSettings`, `StorageKeys.Sound`.
  - `Shared.UI`: `SoundToggle` in `MainLayout`, Texte.
  - `Game`: `GameSession.StepApplied`, `RunSounds` (Zuordnung Schritt zu Ton, Priorität Erfolg vor Item).
  - `Hosts/HeroPath.Web`: `BrowserSoundPlayer`, `wwwroot/js/sound.js`, Registrierung.
  - `tests/HeroPath.Testing`: `FakeSoundPlayer`. Skills `game-rules`, `blazor-ui`, `localization`.
- **Vorgehen:** Der Schalter hat `aria-pressed`, ein eigenes Symbol pro Zustand (Lautsprecher mit Wellen, durchgestrichen) und einen lokalisierten Namen. `RunSounds` wartet nicht auf den Ton (NFR3). `BrowserSoundPlayer` meldet nie Fehler, wie die anderen Plattformdienste. Ohne Interaktion und bei fehlendem Audio bleibt es still. Der Interpreter ändert sich nicht.
- **Tests:**
  - xUnit: `RunSoundsTests` (alle Fehlerarten, Erfolg vor Item, Hinweise vor dem Start sind stumm, aus heisst stumm), `SoundSettingsTests` (Standard aus, speichern, kaputter Wert, Storage nicht verfügbar).
  - bUnit: `SoundToggleTests`. E2E `SoundToggleTests` mit Neuladen, Konsole ohne Fehler.
  - Screenshots: `sound-off`, `sound-on`, DE und EN, normal und Graustufen.
- **Akzeptanz:** FR13 bis FR16. Das Hören selbst prüft der Probelauf.
- **Abhängigkeiten:** keine.

### S6: Tastatur

- **Ziel:** Ein Level lässt sich nur mit der Tastatur lösen.
- **Dateien (Option A):**
  - `Shared`: `IKeyboardShortcuts` (Abo auf Tastenereignisse).
  - `Game`: `ShortcutKeys` (reine Zuordnung Taste zu `ShortcutAction`).
  - `Game.UI`: `PlayPage` abonniert beim Öffnen und löst beim Verlassen, `ShortcutHelp` als aufklappbare Liste.
  - `Hosts/HeroPath.Web`: `BrowserKeyboardShortcuts`, `wwwroot/js/keyboard.js`.
  - Skills `game-rules`, `blazor-ui`, `localization`.
- **Vorgehen:** Der JS-Filter wertet Ziel, `event.repeat` und Modifikatoren aus und ruft `preventDefault` nur für behandelte Tasten. Aktionen laufen über dieselben Wege wie Klicks (`Session.Add`, `MoveUp`, `MoveDown`, `Remove`, `Select`, `Runner.PlayAsync`, `StepOnce`, `Reset`). Damit gelten Sperren während eines Laufs und das Limit von 200 ohne eigene Regeln. Der Level-Editor abonniert nichts.
- **Tests:**
  - xUnit: `ShortcutKeysTests` (jede Taste, Shift, unbekannte Tasten).
  - bUnit: `ShortcutHelpTests`, `PlayPageShortcutTests` mit Fake (gesperrt während des Laufs, Programm voll, Abmelden beim Verlassen).
  - E2E `KeyboardTests`: Level vom ersten Baustein bis zum Erfolg, gedrückte Taste zählt einmal, Namensfeld (Start), Level-Select und Tempo-Regler lösen nichts aus, Leertaste auf fokussiertem Knopf löst nur den Knopf aus.
  - Screenshots: `shortcut-help`, DE und EN, normal und Graustufen.
- **Akzeptanz:** FR17 bis FR21.
- **Abhängigkeiten:** keine.

### S7: Sprache

- **Ziel:** Sprache vollständig und einheitlich.
- **Dateien:** `Shared`: `IDocumentLanguage`. `Shared.UI`: `LanguageService`. `Hosts/HeroPath.Web`: Implementierung (`IJSRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "lang", wert)`, ohne neue JS-Datei), `index.html` (`lang="de-CH"`), resx.
- **Vorgehen:**
  - `LanguageService` setzt das Attribut beim Start und bei jedem Wechsel, Fehler bleiben folgenlos. An 10 Stellen in Tests wird der Konstruktor angepasst.
  - Texte in beiden Sprachen durchgehen, Liste der Änderungen in der Übergabe festhalten.
  - Feste Texte suchen. Geprüft ist bereits: Kein `aria-label`, `title` oder `placeholder` im Razor hat feste Wörter. `NotFound` leitet zur Startseite um und zeigt keinen Text. Ladebildschirm und Fehleranzeige in `index.html` haben nur Symbole, sie erscheinen vor dem Laden der Sprache und bleiben ohne Text.
- **Tests:** xUnit `LanguageServiceTests` erweitern, E2E `LanguageTests` (`lang` nach Wechsel und Neuladen). Vorhandene Tests zu gleichen Schlüsseln und «ß» bleiben.
- **Akzeptanz:** FR7 bis FR9.
- **Abhängigkeiten:** S4 bis S6.

### S8: Prüfliste Probelauf

- **Ziel:** Die Projektverantwortliche kann den Probelauf ohne Rückfragen durchführen und die Ergebnisse festhalten.
- **Dateien:** `docs/Probelauf.md`, Skill `release` (Verweis).
- **Vorgehen:** Vorbereitung (Pages aktivieren, ZIP herunterladen), Schritte mit Feld für Ergebnis, Messung der Startzeit, Abschnitt «Funde». Dazu der Punkt, was bei blockierter Startdatei zu tun ist, und die Fragen an die IT.
- **Tests:** keine (Dokument).
- **Akzeptanz:** FR5. FR6 erfüllt sich erst durch den Probelauf selbst: M6 bleibt in `Projekt_Kontext.md` §7 bis dahin «in Arbeit».
- **Abhängigkeiten:** alle.

## Befehle

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx
HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E
dotnet format HeroPath.slnx --verify-no-changes
dotnet publish src/Hosts/HeroPath.Web -c Release -o artifacts/publish
```

## Vorgehen bei Commits

Ein Commit pro Slice auf `main`, nach grünem Build. Gepusht wird nach S1, danach nach jedem Slice, der einen Workflow ändert, und am Ende.

## Risiken

| Risiko | Gegenmassnahme |
|---|---|
| CI zeigt Linux-Unterschiede (Pfade, Schriften, Laufzeit der E2E-Tests) | Eigener Slice S1 zuerst, Screenshots auf Linux gelten nicht als Vergleich zu Windows |
| Beide Auslieferungswege scheitern auf dem Laptop | Früh testen, Rückfrage an die IT in der Prüfliste. Ohne Pages und ZIP bleibt nur eine Freigabe der IT |
| Kürzel stören bestehende Bedienung | Bestehende E2E-Tests bleiben unverändert grün, Filter per E2E abgesichert |
| Spielseite wird mit Tastenliste zu voll | Liste aufklappbar, Prüfung im visuellen Check auf den Laptop-Viewports |
