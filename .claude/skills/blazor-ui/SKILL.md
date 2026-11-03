---
name: blazor-ui
description: Blazor WebAssembly and Razor Class Library conventions for the module UI projects (HeroPath.Shared.UI, HeroPath.Modules.*.UI) and the web host — MAUI-ready component rules (everything in RCLs, no direct localStorage/window access, _content paths), component structure (code-behind, CSS isolation, parameters, EventCallback), GameSession state and re-rendering, ExecutionRunner play loop, Fluent UI Blazor usage, HTML5 drag & drop with click alternative, CSS Grid board and animations, accessibility, IAppStorage, and bUnit testing patterns. Use when creating or changing Razor components, UI services, styles, the web host, or bUnit tests.
---

# Blazor UI — module UI projects and web host

Docs: `docs/Architektur C4.md` §4.1 (components), §4.3 (MAUI rules), §5 (Play/Debug sequence), §6 (decisions). Placement comes from the `architecture` skill, game behaviour from `game-rules`, texts from `localization`, graphics from `svg-assets`, E2E from `e2e-playwright`.

## Placement rules (non-negotiable, Architektur §4.3)

1. Components live in the UI project of their module: `HeroPath.Modules.Game.UI` (PlayPage, StartDialog, BlockPalette, ProgramEditor, ControlBar), `HeroPath.Modules.Levels.UI` (board view, LevelPicker, LevelEditor). Cross-module UI (MainLayout, NotFound, theme, SVG graphics, LanguageSwitcher) lives in `HeroPath.Shared.UI`. `Hosts/HeroPath.Web` contains only `Program.cs`, `wwwroot/index.html` + `css/app.css` (loading/error UI), `App.razor` (router with module assemblies in `AdditionalAssemblies`) and **web implementations** of `Shared` contracts.
2. Components never touch `localStorage`, `window`, files or relative `HttpClient` URLs. They use `IAppStorage` (and later `ILevelFileService`).
3. Every drag & drop action has a **click alternative** (click on a palette block appends it; buttons to remove/move).
4. Responsive layout: CSS Grid/Flexbox, relative units, container queries. No fixed pixel widths. Click/tap targets ≥ 44 px.
5. Static assets from an RCL: `_content/<AssemblyName>/...` (e.g. `_content/HeroPath.Shared.UI/css/theme.css`).
6. Interactive elements and state carriers get a `data-testid` (`palette-right`, `control-play`, `status` with `data-state`) for E2E tests. Shell ids: `app-title` (the single `<h1>` in `MainLayout`, which `FocusOnNavigate Selector="h1"` relies on, so pages must not add another h1), `language-de` / `language-en` (`aria-pressed` marks the active one).
7. No CDN links. Fonts, icons and graphics are local.
8. MAUI is **not** in scope. Don't create a MAUI project, but don't block one either.

## Component structure

```
Components/ProgramEditor.razor        markup only
Components/ProgramEditor.razor.cs     partial class: parameters, handlers, state
Components/ProgramEditor.razor.css    CSS isolation
```

- No game logic in markup or code-behind: call the module's `Domain` (`LevelValidator`) or `Application` services (`GameSession`, `ExecutionRunner`).
- Small components. Parameters are `[Parameter] public required T X { get; set; }` or have defaults; child → parent via `EventCallback<T>`.
- Inject with `[Inject] private T X { get; set; } = default!;` in code-behind (or `@inject` in markup, consistently per project; prefer code-behind).
- Lists with `@key="command.Id"` (stable ids from Core, important for drag & drop and highlighting).
- Event handlers return `Task`, not `async void`.

## State and services

The services are pure C# in the module's `Application` folder (no Blazor types), registered by `Add<X>Module()`.

| Service | Lifetime | Role |
|---|---|---|
| `GameSession` (Game) | scoped (= one per tab in WASM) | current grid, hero (avatar + name, **memory only**), program, current `ExecutionStep`, status |
| `ExecutionRunner` (Game) | scoped | Play loop / Debug step / Reset, owns the `Interpreter` and the `CancellationTokenSource` |
| `LevelRepository` (Levels) | scoped | embedded standard levels + custom levels via `IAppStorage` |
| `IAppStorage` (Shared contract) | scoped, host implementation | `GetAsync`/`SetAsync`/`RemoveAsync` for strings (JSON) |

- `GameSession` raises `event Action? Changed`. Components subscribe in `OnInitialized`, unsubscribe in `Dispose` (`@implements IDisposable`), and re-render via `InvokeAsync(StateHasChanged)`.
- `ExecutionRunner.PlayAsync`: `while` running → `Step()` → `session.UpdateRun(status, step)` → `await Task.Delay(tempo, timeProvider, ct)`. Reset cancels the token and discards the interpreter. Catch `OperationCanceledException` only in the runner.
- Disable controls by state: Play/Debug disabled while running, the editor is read-only during a run, Reset always enabled.
- The hero name never goes into `IAppStorage`.

## Web host (`Hosts/HeroPath.Web/Program.cs`)

```csharp
builder.Services
    .AddSharedUI()                                    // localization, layout services
    .AddLevelsModule()
    .AddGameModule();
builder.Services.AddScoped<IAppStorage, LocalStorageAppStorage>();   // Hosts/HeroPath.Web/Services
builder.Services.AddFluentUIComponents();
var host = builder.Build();
await host.Services.ApplyStoredCultureAsync();        // see localization skill
await host.RunAsync();
```

`LocalStorageAppStorage` uses `IJSRuntime.InvokeAsync<string?>("localStorage.getItem", key)` etc. No custom JS file needed. Wrap calls in try/catch for `JSException` (storage may be blocked or full) and degrade gracefully: the game must still be playable without storage.

## Fluent UI Blazor

- Only for dialogs (`FluentDialog`, StartDialog), selection (`FluentSelect`, LevelPicker), slider (`FluentSlider`, tempo) and buttons. Board, palette and program blocks are **custom** components with playful styling.
- Override the look with CSS custom properties / isolated CSS. Don't fight Fluent's internals with `!important` chains.
- Fluent UI's web components and CSS ship with the NuGet package (local `_content`), so no CDN is needed. Verify that after publishing.

## Drag & drop (ProgramEditor)

- Native HTML5: `draggable="true"`, `@ondragstart`, `@ondragover:preventDefault`, `@ondrop`, `@ondragend`. Keep the "currently dragged block" in the component state (C#), not in `DataTransfer`.
- Drop targets: between blocks (insert index) and inside repeat bodies. Respect the nesting depth from Core: a repeat cannot be dropped at depth 2. Show the invalid drop target visually; never throw.
- Order of implementation (risk mitigation from the docs): flat list with click → drag & drop → repeat → nesting.
- Click alternative: click on a palette block = append to the end (or into the selected repeat), plus a remove button per block.

## Board and animation

- `GameBoard` (Levels.UI, no Game types): parameters `Grid` (required), `HeroPosition` (`Position`), `MarkedCell` (`Position?`), `StepDuration` (default 500 ms), `HeroContent` (`RenderFragment?`, e.g. `<HeroMax />`). CSS Grid `grid-template-columns: repeat(var(--cols), 1fr)`, square cells via `aspect-ratio: 1`; the width follows the available height (`--hp-board-reserved`).
- Board test ids: `game-board`, `cell-{x}-{y}` with `data-kind` (`start|goal|tree|rock|pond|empty`) and `data-marked="true"` (dashed outline plus hatching, readable in greyscale), `hero` with `data-x`/`data-y`. `PlayPage` has `side-panel` (palette, program and controls follow) and `level-name`.
- Program editing (S3): `BlockPalette` buttons `palette-up|down|left|right` (disabled when `!CanAdd || !IsEditable`); `ProgramEditor` has `program` (the `<ol>`), `program-empty` (hint), `program-clear`, and per block `program-block` with `data-index` (0-based), `data-command-id`, `data-direction` (`up|down|left|right`) plus `program-remove-{index}`. The list scrolls inside the side panel (flex column, `overflow-y: auto`), never the page.
- `GameSession` API: `Load(Grid)`, `Grid`, `HeroPosition`, `Commands` (read-only `MoveCommand` list), `Add(Direction)`, `Remove(Guid)`, `Clear()`, `CanAdd` (false at `ProgramLimits.MaxProgramCommands`), `IsEditable` (read-only, true only while `Status == Ready`), `Status` (`RunStatus`), `CurrentStep`, `MarkedCell`, `ToProgram()`, `Changed`. Add/Remove/Clear are silent no-ops when locked, full or unknown; `Changed` fires only on real changes. Run state is written only by `ExecutionRunner` (internal mutators).
- Run controls (S4): `ControlBar` with `control-play`, `control-debug`, `control-reset` (always enabled), `control-tempo` (`<input type=range min=0 max=2 step=1>`, `aria-valuetext`, label `tempo-value`) and `status` (`aria-live=polite`, `data-state` = `ready|running|paused|failed|succeeded`, `data-error` = `ExecutionError` name only when failed, icon shape plus text). Play and Debug are disabled for an empty program, while `Running` (both) and after an end state; Debug stays enabled while `Paused`. Play is awaited in the click handler (`Task`), the loop yields at `Task.Delay`. `ProgramEditor` marks the block of `CurrentStep.CommandId` with `data-current="true"` (thick outline plus arrow marker) and on `Failed` additionally `data-error="true"` (dashed outline plus cross marker). `PlayPage` places `ControlBar` below the program in the side panel and passes `HeroPosition`, `MarkedCell` and `StepDuration = Runner.Tempo.Duration` to `GameBoard`.
- The hero is an absolutely positioned layer that moves with `transform: translate(...)` and `transition` (duration bound to the tempo). Don't re-render the hero inside different cells; that breaks the animation.
- Highlight the current block via `ExecutionStep.CommandId`. On an error, mark both the block and the board cell.
- Respect `@media (prefers-reduced-motion: reduce)` (no transitions or confetti).

## Accessibility (for kids, but it applies to all)

- Real `<button>` elements for all actions (keyboard-accessible by default), visible focus.
- Start and goal distinguishable by **pattern and shape**, not just colour.
- Localized `aria-label`/`<title>` on blocks and SVGs.

## bUnit tests (in `tests/HeroPath.Modules.<X>.Tests` next to the xUnit tests)

```csharp
public class ControlBarTests : BunitContext
{
    public ControlBarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
        Services.AddLocalization();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    [Fact]
    public void Play_IsDisabled_WhileRunning() { /* Render<ControlBar>(p => p.Add(...)); */ }
}
```

- bUnit 2.x API: `BunitContext`, `Render<T>()`, `Find`, `FindAll`, `WaitForAssertion`.
- `InMemoryAppStorage` lives as a test fake in each test project that needs it (no shared test library until two modules need the same fakes).
- Full user flows across components are E2E tests (`e2e-playwright` skill), not bUnit.
- Priority per Architektur §8: `ProgramEditor` (add, remove, nest), `ControlBar`, `GameBoard` rendering, language switching.
- Test via the click alternative (drag events are hard to simulate). Real drag & drop is covered by one dedicated E2E test and a manual check.
- For the runner, use `FakeTimeProvider`. No real delays in tests.
