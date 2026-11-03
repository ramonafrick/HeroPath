# HeroPath

Browser learning app (Blazor WebAssembly, .NET 10) for children aged 11 to 13: they program a hero across a board to the castle using blocks.

## Read first

1. `docs/Architektur C4.md`: modules, components, MAUI rules (§4.3), decisions
2. `docs/Projektidee.md`: gameplay, blocks, messages, levels
3. `docs/Projekt_Kontext.md`: decisions, MoSCoW, limits, conventions, milestones

The docs describe the target architecture. Only the code in `src/` and `tests/` shows what is already implemented.

## Structure

Modular Clean Architecture, details in the `architecture` skill:

```
src/Shared/          HeroPath.Shared (contracts) + HeroPath.Shared.UI (layout, theme, graphics, language)
src/Modules/Levels/  HeroPath.Modules.Levels (Domain/Application/Infrastructure) + .UI
src/Modules/Game/    HeroPath.Modules.Game  (Domain/Application) + .UI
src/Hosts/           HeroPath.Web (today), HeroPath.Maui (later, do not create)
```

Dependencies point `Shared ← Levels ← Game`, the same for `.UI`. Pure projects have no Blazor. `tests/HeroPath.ArchitectureTests` enforces this.

## Commands

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx                       # unit, bUnit and architecture tests (E2E are skipped)
HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E    # Playwright E2E + screenshots in artifacts/screenshots
dotnet format HeroPath.slnx --verify-no-changes
dotnet watch --project src/Hosts/HeroPath.Web
dotnet publish src/Hosts/HeroPath.Web -c Release
```

## Key rules

- Game rules live only in the `Domain` folder of the owning module. Every logic change needs an xUnit test, every visible flow an E2E test.
- No platform access in components (`localStorage`, `window`), only through `IAppStorage`.
- User-facing texts only via resx, always DE (Swiss spelling, no «ß») and EN.
- Drag & drop always has a click alternative.
- The hero's name is never stored or logged.
- No CDNs, no third-party graphics, texts or code excerpts.
- Language: code, identifiers, `CLAUDE.md` and everything in `.claude/` (skills, agents) in English. Project documentation in `docs/` in German. German is fine inside these files only where it is product content, e.g. quoted UI texts or the German template for `docs/`.
- Skills and agents in `.claude/` are part of the code. If a commit changes something a skill or agent describes (types, rules, limits, commands, workflows), update the description in the same commit. `ClaudeWorkflowTests` checks that every public type of the Levels and Game modules is named in `game-rules`, and that skill names, agent names and the skill table below match.

## Working style: research, don't assume

- Do not make assumptions. If something is not settled by the request, the code or `docs/`, research it before acting: the code, `docs/`, the git history, and official sources (Microsoft Learn, .NET/Blazor/Playwright docs, Claude Code docs).
- Name the sources: file paths with line numbers, URLs, doc sections. Mark anything that could not be verified as unverified.
- Make proposals instead of silently deciding: for each design decision, show the options with pros and cons and a recommendation, then implement after approval.
- If research leaves the question open, say what was checked and ask, instead of guessing.

## Known pitfalls

- Windows Application Control can block freshly built DLLs on load («An Application Control policy has blocked this file»). Tests that inspect assemblies therefore read metadata instead of calling `Assembly.Load`.
- A test project without tests makes `dotnet test` fail (MTP exit code 8).
- Unnecessary `using`s are build errors (IDE0005).
- Blazor WebAssembly only loads the satellite assemblies of the startup culture, so a runtime language switch needs `loadAllSatelliteResources` in `index.html` (see `localization` skill). bUnit cannot catch this, `LanguageTests` (E2E) does.

## Skills

| Skill | Purpose |
|---|---|
| `architecture` | Modules, dependency rules, where new code belongs |
| `dotnet10` | C# conventions, solution, packages, tests, CLI |
| `game-rules` | Interpreter, movement, errors, level JSON |
| `blazor-ui` | Components, services, drag & drop, bUnit |
| `localization` | Texts, resx per module, language switching, tone |
| `level-design` | Standard levels, solvability test |
| `svg-assets` | Own SVG graphics |
| `e2e-playwright` | E2E tests, screenshots for the visual check |
| `release` | CI, GitHub Pages, ZIP for office laptops |
| `grill-me` / `orchestrate` | Clarify requirements / full feature workflow incl. visual check |
