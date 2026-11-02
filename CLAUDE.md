# HeroPath

Browser-Lern-App (Blazor WebAssembly, .NET 10) für Kinder von 11 bis 13 Jahren: Mit Bausteinen wird ein Held über ein Spielfeld zur Burg programmiert.

## Zuerst lesen

1. `docs/Architektur C4.md`: Module, Komponenten, MAUI-Regeln (§4.3), Entscheidungen
2. `docs/Projektidee.md`: Spielablauf, Bausteine, Meldungen, Level
3. `docs/Projekt_Kontext.md`: Entscheidungen, MoSCoW, Grenzwerte, Konventionen, Meilensteine

Die Docs beschreiben die Zielarchitektur. Was davon schon umgesetzt ist, zeigt nur der Code in `src/` und `tests/`.

## Aufbau

Modulare Clean Architecture, Details im Skill `architecture`:

```
src/Shared/          HeroPath.Shared (Verträge) + HeroPath.Shared.UI (Layout, Theme, Grafiken, Sprache)
src/Modules/Levels/  HeroPath.Modules.Levels (Domain/Application/Infrastructure) + .UI
src/Modules/Game/    HeroPath.Modules.Game  (Domain/Application) + .UI
src/Hosts/           HeroPath.Web (heute), HeroPath.Maui (später, nicht anlegen)
```

Die Abhängigkeitsrichtung ist `Shared ← Levels ← Game`, für `.UI` gleich. Reine Projekte haben kein Blazor. `tests/HeroPath.ArchitectureTests` prüft das.

## Befehle

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx                       # Unit-, bUnit- und Architekturtests (E2E werden übersprungen)
HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E    # Playwright E2E + Screenshots in artifacts/screenshots
dotnet format HeroPath.slnx --verify-no-changes
dotnet watch --project src/Hosts/HeroPath.Web
dotnet publish src/Hosts/HeroPath.Web -c Release
```

## Wichtigste Regeln

- Spielregeln nur im `Domain`-Ordner des zuständigen Moduls. Zu jeder Logikänderung gehört ein xUnit-Test, zu jedem sichtbaren Ablauf ein E2E-Test.
- Keine Plattformzugriffe in Komponenten (`localStorage`, `window`), nur über `IAppStorage`.
- Benutzertexte nur über resx, immer DE (Schweizer Rechtschreibung, kein «ß») und EN.
- Drag & Drop immer mit Klick-Alternative.
- Der Heldenname wird nie gespeichert oder geloggt.
- Keine CDNs, keine fremden Grafiken, Texte oder Code-Auszüge.
- Code und Bezeichner auf Englisch, Doku auf Deutsch.
- Entwurfsentscheidungen mit Alternativen kurz erklären, bevor sie umgesetzt werden.

## Bekannte Stolpersteine

- Windows Application Control kann frisch gebaute DLLs beim Laden blockieren («An Application Control policy has blocked this file»). Tests, die Assemblies untersuchen, lesen deshalb Metadaten statt `Assembly.Load`.
- Ein Testprojekt ohne Test lässt `dotnet test` fehlschlagen (MTP Exit-Code 8).
- Überflüssige `using`s sind Build-Fehler (IDE0005).

## Skills

| Skill | Wofür |
|---|---|
| `architecture` | Module, Abhängigkeitsregeln, wo neuer Code hingehört |
| `dotnet10` | C#-Konventionen, Solution, Pakete, Tests, CLI |
| `game-rules` | Interpreter, Bewegung, Fehler, Level-JSON |
| `blazor-ui` | Komponenten, Services, Drag & Drop, bUnit |
| `localization` | Texte, resx pro Modul, Sprachwechsel, Tonalität |
| `level-design` | Standard-Levels, Lösbarkeits-Test |
| `svg-assets` | Eigene SVG-Grafiken |
| `e2e-playwright` | E2E-Tests, Screenshots für den visuellen Check |
| `release` | CI, GitHub Pages, ZIP für Büro-Laptops |
| `grill-me` / `orchestrate` | Anforderungen klären / kompletter Feature-Workflow inkl. visuellem Check |
