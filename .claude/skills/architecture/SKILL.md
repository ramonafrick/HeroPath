---
name: architecture
description: HeroPath's modular Clean Architecture — Shared, Modules (Levels, Game) and Hosts, the two-project-per-module pattern (pure C# library with Domain/Application/Infrastructure folders + Razor UI library), dependency rules enforced by ArchitectureTests, where a new class/component/resource belongs, module registration (Add<X>Module), platform abstractions for a future MAUI host, and how to add a new module. Use before creating any new file or project, when deciding where code goes, or when a dependency between modules seems necessary.
---

# Architecture — modular, clean, MAUI-ready

Docs: `docs/Architektur C4.md` (§3 containers, §4 components, §6 decisions, §9 folders). MiniMate inspired the module layout. HeroPath deliberately keeps domain logic free of Blazor.

## Layout

```
src/
  Shared/
    HeroPath.Shared/                 pure C#: cross-module contracts (IAppStorage, IFileDownload, …), TextCleaner
    HeroPath.Shared.UI/              RCL: MainLayout, NotFound, theme, SVG graphics, localization, LanguageSwitcher, ConfirmDialog
  Modules/
    Levels/
      HeroPath.Modules.Levels/       pure C#: Domain/ (Position, Grid, ObstacleKind, ItemKind, LevelDefinition, LevelLimits, LevelSolver, EditorTool, LevelEditing)
                                               Application/ (LevelValidator, LevelReadResult, LevelCheck (+ internal LevelCheckCache), LevelIdGenerator, LevelRepository, SaveOutcome, LevelEditorState, ImportResult, Contracts/ICustomLevelStore)
                                               Infrastructure/ (embedded Levels/*.json, JSON context, StorageCustomLevelStore)
      HeroPath.Modules.Levels.UI/    RCL: board view (GameBoard, also the clickable edit board), LevelPicker, LevelEditor (+ EditorToolbar, EditorSizeControl, EditorStatus, CustomLevelList, EditorLevelFiles for import and export), LevelsModuleExtensions (AddLevelsModule, LoadCustomLevelsAsync), LevelDisplayExtensions (NameOf), Resources/
    Game/
      HeroPath.Modules.Game/         pure C#: Domain/ (Direction, Person, MoveResult, commands, ProgramDefinition, BlockSlot, ProgramEditing,
                                               Interpreter, ExecutionStep, ProgramLimits)
                                               Application/ (GameSession, ExecutionRunner, HeroChoice, LevelProgress)
      HeroPath.Modules.Game.UI/      RCL: StartPage, PlayPage, EditorPage (routes /editor and /editor/{LevelId}, hosts LevelEditor from Levels.UI, decision D3 of M5), HeroBadge, HeroFigure, BlockPalette, ProgramEditor, BlockList, ProgramBlock, DropZone, ControlBar, MessageBox, ItemCounter, SuccessConfetti, GameModuleExtensions, Services/ (ProgramDragState), Resources/
  Hosts/
    HeroPath.Web/                    Blazor WASM host: Program.cs, App.razor (router), index.html, web platform services
    HeroPath.Maui/                   (later, do not create)
tests/
  HeroPath.Shared.Tests/             xUnit + bUnit for Shared and Shared.UI (LanguageService, LanguageSwitcher, ConfirmDialog)
  HeroPath.Modules.Levels.Tests/     xUnit + bUnit for Levels and Levels.UI
  HeroPath.Modules.Game.Tests/       xUnit + bUnit for Game and Game.UI
  HeroPath.Testing/                  plain class library, no tests: shared fakes (InMemoryAppStorage, ThrowingAppStorage,
                                     CultureScope, CultureBunitContext), referenced by the three *.Tests projects
  HeroPath.ArchitectureTests/        dependency rules (metadata-based)
  HeroPath.E2E/                      Playwright, opt-in via HEROPATH_E2E=1 (see e2e-playwright skill)
```

## Dependency rules (enforced by `tests/HeroPath.ArchitectureTests`)

```
Shared      ← Levels      ← Game          (pure C#)
Shared.UI   ← Levels.UI   ← Game.UI       (Razor)
X.UI → X ;  Hosts → all UI projects
```

- **Pure projects** (`Shared`, `Modules.Levels`, `Modules.Game`) never reference `Microsoft.AspNetCore.*`, `Microsoft.JSInterop` or any `.UI` project. They have no NuGet dependencies unless justified.
- `Levels` never knows `Game`. If Levels needs something from Game, the model is wrong. Move the concept down into Levels or Shared.
- `Shared*` never references a module.
- Nothing references a host.
- Inside a pure project: `Domain` uses nothing from `Application`/`Infrastructure`. `Application` may use `Domain`. `Infrastructure` implements `Application` contracts. Folder namespaces match (`HeroPath.Modules.Game.Domain`, …). The code-reviewer checks this. There is no test for it.

When you add a project-level rule, add it to `DependencyRuleTests.Rules`.

## Where does it go?

| Thing | Location |
|---|---|
| Game rule, value object, entity | `Modules.<X>/Domain` |
| Use case / stateful service without Blazor (GameSession, ExecutionRunner, LevelRepository) | `Modules.<X>/Application` |
| Contract needed by Application, implemented elsewhere (`ICustomLevelStore`, implemented by `StorageCustomLevelStore` in Infrastructure) | `Modules.<X>/Application/Contracts` |
| JSON, embedded resources, storage-backed implementation | `Modules.<X>/Infrastructure` |
| Razor page/component of one feature | `Modules.<X>.UI/Pages` or `/Components` |
| Texts of one module | `Modules.<X>.UI/Resources/<X>Resources.resx` (+ `.en.resx`) |
| Component base classes (language / session / drag subscription) | `Shared.UI/Components` (`LocalizedComponentBase`), `Modules.Game.UI/Components` (`SessionComponentBase`, `DragAwareComponentBase`) |
| UI-only state shared by components (drag state) | `Modules.<X>.UI/Services` (`ProgramDragState`) |
| Routable page that needs both modules (EditorPage hosts the Levels.UI `LevelEditor`; «Try it» needs `HeroChoice`) | `Modules.Game.UI/Pages`; the reusable component stays in `Modules.Levels.UI/Components` |
| Layout, theme, SVG graphics, app title, LanguageSwitcher, ConfirmDialog | `Shared.UI` |
| Platform contract (storage, file download: `IAppStorage`, `IFileDownload`; replaces the planned `ILevelFileService`) | `Shared/Application/Contracts` |
| Platform implementation (localStorage via JS interop, `BrowserFileDownload` plus `wwwroot/js/file-download.js`) | `Hosts/HeroPath.Web/Services`, JS in the host `wwwroot/js` |

## Module registration

- Each module exposes `Add<X>Module(this IServiceCollection)` in its **UI project** (it registers both its Application services and UI services). This keeps the pure project free of DI packages.
- `Shared.UI` exposes `AddSharedUI()`.
- Hosts compose: `services.AddSharedUI().AddLevelsModule().AddGameModule();` plus their platform services (`IAppStorage`).
- Routable pages: the web host's `App.razor` lists module UI assemblies in `AdditionalAssemblies`. A new module with pages must be added there.

## MAUI readiness

- Everything except startup and platform services lives in Shared/Modules. A future `Hosts/HeroPath.Maui` only adds `MauiProgram.cs`, a `BlazorWebView` and MAUI implementations of the `Shared` contracts.
- No `window`, `localStorage`, `HttpClient` with relative URLs or file system access outside hosts.

## Adding a new module

Only when a feature has its own domain language and would otherwise blur an existing module (e.g. later «Themenwelten»). Don't create a module for a single component.

1. `src/Modules/<X>/HeroPath.Modules.<X>` (classlib) + `HeroPath.Modules.<X>.UI` (razorclasslib), `SupportedPlatform browser` in the UI csproj.
2. References according to the rules above. Add both to `HeroPath.slnx` under `src/Modules/<X>`.
3. `Add<X>Module()`, register it in every host, add the assembly to `App.razor` if it has pages.
4. `tests/HeroPath.Modules.<X>.Tests` with at least one real test (an empty test project fails `dotnet test`).
5. Extend `DependencyRuleTests.Rules` and the module table in `docs/Architektur C4.md`.
