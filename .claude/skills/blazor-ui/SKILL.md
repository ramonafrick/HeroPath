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

- `GameSession` raises `event Action? Changed`. Components get the subscription from the base classes below.
- `ExecutionRunner.PlayAsync`: `while` running → `Step()` → `session.UpdateRun(status, step)` → `await Task.Delay(session.Tempo.Duration, timeProvider, ct)`, in `try/finally` (an exception ends the run `Paused`, never `Running`). `Reset` cancels the token and discards the interpreter, `Stop` cancels and leaves the run `Paused`. Catch `OperationCanceledException` only in the runner.
- Disable controls by state: Play/Debug disabled while running, the editor is read-only during a run, Reset always enabled.
- The hero name never goes into `IAppStorage`.

## Component lifecycle (rules for ALL components)

Base classes remove the subscription boilerplate: `LocalizedComponentBase` (`Shared.UI/Components`, subscribes `LanguageService.Changed`, `Dispose(bool)`, idempotent) and `SessionComponentBase : LocalizedComponentBase` (`Game.UI/Components`, adds `GameSession.Changed`, protected `Session`). `GraphicBase`, `MainLayout`, `LanguageSwitcher`, `GameBoard` use the first, `PlayPage`, `ControlBar`, `ProgramEditor`, `BlockPalette` the second. A razor file repeats the base with `@inherits` (the code-behind names the same base). `MainLayout` is a layout by convention: it declares `[Parameter] Body` itself.

| Hook | Use for | Never |
|---|---|---|
| `OnInitialized` | one-time work: load state into services (`PlayPage`: `Session.Load`, **before** `base.OnInitialized()`), subscribe to service events (in the base classes) | async loading, re-reading parameters |
| `OnParametersSet` | derive render values from parameters, only where parameters exist (`GameBoard`: styles and cell kinds, rebuilt when the `Grid` instance changes) | subscribing, loading services |
| `OnAfterRender` | JS interop that needs the DOM | loading or changing state (it triggers another render) |
| service event | `InvokeAsync(StateHasChanged)` (`HandleChanged` in the base) | calling `StateHasChanged` directly: events can arrive outside the render context |
| own UI handler (`@onclick`) | change the service, nothing else: Blazor re-renders after the handler | manual `StateHasChanged` |
| `Dispose` | unsubscribe; `PlayPage` additionally calls `Runner.Stop()` | leaving a loop running |

State is durable because it lives in the scoped services, not in components: `GameSession.Load` of an equal grid is a no-op, so a component that is created again (return to the page, re-render) finds program and run as they were. Leaving `PlayPage` pauses a running Play (`Runner.Stop()`: status `Paused`, program, position and interpreter kept), so the page shows a consistent state on return and the child continues with Play or Debug. Reset would have thrown away progress the child did not ask to lose.

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

## Drag & drop (ProgramEditor, M3 S4)

- Only Blazor events, no custom JS. Everything has a click alternative that stays fully working.
- `ProgramDragState` (`Game.UI/Services`, scoped, registered in `AddGameModule`) holds what is dragged (`DragSource.NewMove(Direction)`, `NewRepeat`, `Existing(Guid)`) and raises `Changed` on start/end. It asks the `GameSession` for validity (`CanAddAt`, `CanAddRepeatAt`, `CanMove`, `CanRemove`, all false while locked) and re-validates on every drop (`Drop(slot)`, `DropOnPalette()`), so a drop after the program changed (e.g. a run started) does nothing. `Start` is ignored while not editable. `End()` always clears (idempotent, raises `Changed` once). `PlayPage.Dispose` calls `Drag.End()` next to `Runner.Stop()`, so leaving the page never leaves a drag behind. No rules and no throwing in the UI. Dragging never changes the selection.
- `DragAwareComponentBase : SessionComponentBase` (palette, drop zone, block) injects it and re-renders on `Changed`.
- Sources: palette buttons (`draggable` only when enabled, `@ondragstart`/`@ondragend`) and the `block-row-{path}` div of every program block (`draggable` only while editable; the row, not the `<li>`, so nested blocks do not drag their parent). Block rows carry `data-dragging="true"` while dragged.
- Lists: `BlockList` (`ParentId`, `ParentNumber`, `Commands`, `PathPrefix`) renders the items of one list without a wrapper: a `DropZone` before every block, the `ProgramBlock`, an end `DropZone`. `ProgramEditor` (root `<ol>`) and `ProgramBlock` (repeat body `<ol>`) both use it. `ProgramBlock` derives its render state (current, error, hinted, progress, name) in computed properties in the code-behind.
- Targets: `DropZone` (`<li aria-hidden>`, not focusable, no aria-labels) before every block and at the end of every list (root, every repeat body, also an empty one). `data-testid="drop-zone"`, `data-slot="{parent}:{index}"` where parent is `root` or the repeat's number (`3.2`) and index is the position in that list before the move (`root:0`, `3:1`), `data-valid="true|false"` only while dragging, `data-hover="true"` while the pointer is over it and a drag is running (hover is derived at render and reset on `Drag.Changed`, never mutated while rendering), `data-end="true"` on the last zone of a list. Idle: 0.625 rem high (replaces the former row gap and leaves room for the corner badges of the next block; end zones 0). While dragging: 1.5 rem. Valid and idle: only a thin, faint dotted line, so the list stays calm. Valid and hovered: solid thick bar (0.625 rem). Invalid and hovered: a hatched, dashed `zone-hatch` strip (zone height plus 4 px) and a «Nein» symbol (circle with slash, own SVG, 2.25 rem, overflowing the zone without changing the layout), readable in greyscale. A block's own position counts as invalid (nothing would change).
- `@ondragover:preventDefault="valid"` (and the same for `dragenter`) takes a bool expression, evaluated at render, so it must be valid after the render that follows `dragstart`. `@ondrop:preventDefault` always. Children of a zone have `pointer-events: none`, otherwise `dragleave` flickers.
- Palette = delete target: `data-testid="palette"` handles `dragover`/`drop`; while an existing block is dragged an overlay `palette-delete` (dashed frame, trash icon, text `Palette_DropDelete`) covers the palette. Dropping removes the block with its body.
- Known limit of the no-JS variant (decision D6 in `docs/plan.md`): Blazor cannot call `dataTransfer.setData`. Secondary sources say Firefox only starts a drag when data is set in `dragstart`; MDN does not confirm it and it is **unverified** (E2E runs Chromium only). Check it on the company browser in the M6 trial run. Touch devices have no HTML5 drag; the click alternative covers both.
- bUnit: `element.DragStart()`, `Drop()`, `DragEnd()` on `block-row-{path}`, palette buttons, `drop-zone` and `palette` (`ProgramDragTests`). Quote ids with dots in selectors: `[data-testid='block-row-1.1']`.
- E2E (`DragDropTests`): the manual mouse sequence in `BeginDragAsync` (wait until no zone still has `data-valid` from the previous drag, hover, `Mouse.DownAsync`, a first move so the drag starts, wait until every zone has `data-valid` (the zones grow one render after the other), measure the target with `StableBoxAsync` (same box in two consecutive animation frames), move there in several steps and wiggle, so `dragover` fires at least twice, wait for `data-hover` on the target). Measuring earlier put the pointer next to the target (the flaky failures of M3). `DragToAsync` is not used because the target moves after the drag starts. Screenshots are taken mid-drag before `Mouse.UpAsync()`.
- Click alternative: click on a palette block = append at `InsertTarget` (end of the program, or the body of the selected repeat); a click on a block selects it, and only the selected block shows ▲▼🗑 (own icons, 44x44, `aria-label`). All side-panel buttons are at least 44x44 px and never clip their text (`RunFlowTests.AssertFitsAsync` checks it). The tempo slider shows Slow/Medium/Fast labels under the track.

## Board and animation

- `GameBoard` (Levels.UI, no Game types): parameters `Grid` (required), `HeroPosition` (`Position`), `MarkedCell` (`Position?`), `StepDuration` (default 500 ms), `HeroContent` (`RenderFragment?`, e.g. `<HeroMax />`). CSS Grid `grid-template-columns: repeat(var(--cols), 1fr)`, square cells via `aspect-ratio: 1`; the width follows the available height (`--hp-board-reserved`).
- Board test ids: `game-board`, `cell-{x}-{y}` with `data-kind` (`start|goal|tree|rock|pond|empty`) and `data-marked="true"` (dashed outline plus hatching, readable in greyscale), `hero` with `data-x`/`data-y`. `PlayPage` has `side-panel` (palette, program and controls follow) and `level-name`.
- The hero is an absolutely positioned layer that moves with `transform: translate(...)` and `transition` (duration bound to the tempo). Don't re-render the hero inside different cells; that breaks the animation.
- Highlight the current block via `ExecutionStep.CommandId`. On an error, mark both the block and the board cell.
- Respect `@media (prefers-reduced-motion: reduce)` (no transitions or confetti).

## Play page components (Game.UI)

- Program editing (M3 S1):
  - `BlockPalette`: `palette-up|down|left|right` (disabled when `!CanAdd || !IsEditable`) and `palette-repeat` (disabled when `!IsEditable || !CanAddRepeat`, full width below the four direction buttons).
  - `ProgramEditor`: `program` (the root `<ol>`, `data-empty="true"` when empty), `insert-target` (line with `data-target` = `root` or the repeat's number such as `3.2`), `program-clear`. An empty program shows `program-placeholder` (dashed hatched box with a block drawing, `aria-hidden`, no text; the hint lives only in `MessageBox`; the end zone fills the box while dragging). `program-empty`, `program-remove-{index}` and `data-index` no longer exist.
  - `ProgramBlock` (recursive, one `<li>` per block): `program-block` with `data-path` (hierarchical number `3.2.1`), `data-kind` (`move|repeat`), `data-selected`, `data-command-id`, `data-direction` (moves only), `data-current`, `data-error`, `data-hint` (`true` on the empty repeat the last refused start pointed at: dotted outline plus info badge), `data-active-repeat` (`true` on a repeat in `CurrentStep.Repeats`: striped frame plus dotted thick body bar, until Reset).
  - Inside a block: `program-select-{path}` (select button, `aria-pressed`); only on the selected block `program-up`, `program-down`, `program-remove` (44 px round buttons, icons from `ProgramIcon`). A repeat header adds `repeat-minus-{path}`, `repeat-count-{path}`, `repeat-plus-{path}`, `repeat-after-{path}` and, while active, `repeat-round-{path}` («Runde 2 von 3»); its body is `<ol data-testid="repeat-body-{path}">`.
  - Layout: single column, rows `2.75rem`, indented per level (`repeat-body`: 2 px margin, 10 px padding, 8 px bar, solid or dotted with the same width so a run does not shift the layout). A row is a `block-head` (`block-head-{path}`) that never wraps: select button plus, for a repeat, minus, count, plus and the trailing word on ONE line. The repeat icon is only on top-level repeats, so depth 2 (path 2.3) fits the 1366x768 program column. A selected or active repeat gets a `block-foot` below with the round pill (left) and ▲▼🗑 (right); a selected move keeps its buttons in the head row. Rows and zones have `flex-shrink: 0`, so they keep their size when the list scrolls.
  - The corner badge (`program-marker`) sits 6 px outside the top-left corner, inside the list padding and the idle zone gap, so it never covers the previous row.
  - E2E guards in `LayoutAssertions`: `AssertRepeatHeadersOnOneLineAsync`, `AssertRowsKeepSizeAsync`.
  - Page: the list scrolls inside the side panel, never the page. Under `@media (max-height: 56rem) and (min-width: 82rem)` (1366x768) the side panel has two columns: tools (palette, controls, status, message; 0.9fr) and program (1.1fr). The page group (board plus panel) is centred; `PlayPage` passes `--board-cols`/`--board-rows` so the board column is as wide as the board.
- `GameSession` API (see also `game-rules`): `Load(Grid)` (equal grid: no-op; different grid: run reset, program kept), `Grid`, `HeroPosition`, `Program` (`ProgramDefinition`), `SelectedId`, `Select(Guid?)`, `InsertTarget` (`BlockSlot`), `Add(Direction)`, `AddRepeat()`, `Remove(Guid)`, `Clear()`, `MoveUp/MoveDown(Guid)`, `CanMoveUp/CanMoveDown(Guid)`, `Move(Guid, BlockSlot)`, `CanMove(Guid, BlockSlot)`, `CanRemove(Guid)`, `Add(Direction, BlockSlot)`, `AddRepeat(BlockSlot)`, `CanAddAt(BlockSlot)`, `CanAddRepeatAt(BlockSlot)`, `SetRepeatCount(Guid, int)`, `CanAdd` (false at `ProgramLimits.MaxProgramCommands`), `CanAddRepeat`, `IsEditable` (read-only, true only while `Status == Ready`), `Status` (`RunStatus`), `CurrentStep`, `MarkedCell` (computed once per applied step), `Tempo` (settable, raises `Changed`), `ToProgram()`, `Changed`. Edits and `Select` are silent no-ops when locked, full, invalid or unchanged; `Changed` fires only on real changes. Run state is written only by `ExecutionRunner` (internal mutators).
- Messages (M3 S3): `MessageBox` (Game.UI) sits directly below `ControlBar` in the tools column of `PlayPage` and renders `GameSession.Message` in a persistent `aria-live=polite` wrapper. Test ids: `message` with `data-kind` (`error|success|hint`) and `data-code` (`GameMessageCode` name). Shape per kind (dashed border plus triangle, thick border plus star, dotted border plus info circle), colour is additional. Text from `Message_<Code>` (obstacle per `Message_Obstacle_<Kind>`, missing items `_One`/`_Other`); hero name placeholder «Max» is a constant in the component. The program hint for an empty program lives only here.
- Run controls (M2 S4): `ControlBar` with `control-play`, `control-debug`, `control-reset` (always enabled), `control-tempo` (`<input type=range min=0 max=2 step=1>`, `aria-valuetext`, label `tempo-value`) and `status` (`aria-live=polite`, `data-state` = `ready|running|paused|failed|succeeded`, `data-error` = `ExecutionError` name only when failed, icon shape plus text). Play and Debug are disabled for an empty program, while `Running` (both) and after an end state; Debug stays enabled while `Paused`. Play is awaited in the click handler (`Task`), the loop yields at `Task.Delay`. `ProgramEditor` marks the block of `CurrentStep.CommandId` with `data-current="true"` (thick outline plus a round badge with a play triangle at the block corner) and on `Failed` additionally `data-error="true"` (red dashed outline plus a warning-triangle badge with an exclamation mark, positioned absolutely at the corner so the block text is never shortened). `PlayPage` places `ControlBar` directly below the palette and above the program in the side panel (stable button positions, the program list fills the rest and scrolls) and passes `HeroPosition`, `MarkedCell` and `StepDuration = Session.Tempo.Duration` to `GameBoard`. Play and Debug are bound to `Runner.CanStart` (the only place of that rule).

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
- Shared fakes live in `tests/HeroPath.Testing` (a plain class library without tests): `InMemoryAppStorage`, `ThrowingAppStorage`, `CultureScope`, `CultureBunitContext`. Don't copy them into a test project; the three test projects have `HeroPath.Testing` as a global using.
- bUnit classes that assert localized text derive from `CultureBunitContext` (explicit culture, default `de-CH`, restored on dispose) and carry `[Collection("Culture")]`. `DisposeComponentsAsync()` simulates leaving a page.
- Full user flows across components are E2E tests (`e2e-playwright` skill), not bUnit.
- Priority per Architektur §8: `ProgramEditor` (add, remove, nest), `ControlBar`, `GameBoard` rendering, language switching.
- Test via the click alternative where possible. Drag handlers are covered with bUnit `DragStart/Drop/DragEnd` (`ProgramDragTests`) and real drag & drop by the dedicated E2E `DragDropTests`.
- For the runner, use `FakeTimeProvider`. No real delays in tests.
