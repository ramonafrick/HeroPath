---
name: blazor-ui
description: Blazor WebAssembly and Razor Class Library conventions for the module UI projects (HeroPath.Shared.UI, HeroPath.Modules.*.UI) and the web host — MAUI-ready component rules (everything in RCLs, no direct localStorage/window access, _content paths), component structure (code-behind, CSS isolation, parameters, EventCallback), GameSession state and re-rendering, ExecutionRunner play loop, Fluent UI Blazor usage, HTML5 drag & drop with click alternative, CSS Grid board and animations, accessibility, IAppStorage, and bUnit testing patterns. Use when creating or changing Razor components, UI services, styles, the web host, or bUnit tests.
---

# Blazor UI — module UI projects and web host

Docs: `docs/Architektur C4.md` §4.1 (components), §4.3 (MAUI rules), §5 (Play/Debug sequence), §6 (decisions). Placement comes from the `architecture` skill, game behaviour from `game-rules`, texts from `localization`, graphics from `svg-assets`, E2E from `e2e-playwright`.

## Placement rules (non-negotiable, Architektur §4.3)

1. Components live in the UI project of their module: `HeroPath.Modules.Game.UI` (StartPage, PlayPage, HeroBadge, HeroFigure, BlockPalette, ProgramEditor, ControlBar), `HeroPath.Modules.Levels.UI` (board view, LevelPicker, LevelEditor). Cross-module UI (MainLayout, NotFound, theme, SVG graphics, LanguageSwitcher) lives in `HeroPath.Shared.UI`. `Hosts/HeroPath.Web` contains only `Program.cs`, `wwwroot/index.html` + `css/app.css` (loading/error UI), `App.razor` (router with module assemblies in `AdditionalAssemblies`) and **web implementations** of `Shared` contracts.
2. Components never touch `localStorage`, `window`, files or relative `HttpClient` URLs. They use `IAppStorage` and `IFileDownload` (export, M5 S5); `InputFile` (Blazor) is the only way to read an imported file.
3. Every drag & drop action has a **click alternative** (click on a palette block appends it; buttons to remove/move).
4. Responsive layout: CSS Grid/Flexbox, relative units, container queries. No fixed pixel widths. Click/tap targets ≥ 44 px.
5. Static assets from an RCL: `_content/<AssemblyName>/...` (e.g. `_content/HeroPath.Shared.UI/css/theme.css`).
6. Interactive elements and state carriers get a `data-testid` (`palette-right`, `control-play`, `status` with `data-state`) for E2E tests. Shell ids: `app-title` (the single `<h1>` in `MainLayout`, which `FocusOnNavigate Selector="h1"` relies on, so pages must not add another h1), `language-de` / `language-en` (`aria-pressed` marks the active one), `sound-toggle` (`aria-pressed`, icons `sound-icon-on|off`).
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
| `LevelRepository` (Levels) | scoped | embedded standard levels + custom levels (loaded at startup through `ICustomLevelStore` over `IAppStorage`, see `game-rules`) |
| `SoundSettings` (Shared) / `ISoundPlayer` (Shared contract, host `BrowserSoundPlayer`) / `RunSounds` (Game) | scoped | sound on/off (stored under `StorageKeys.Sound`, default off, loaded in `Program.cs` through `ApplyStoredSoundAsync` beside the culture); `SoundToggle` (Shared.UI, in `MainLayout` next to `LanguageSwitcher`) switches it; `RunSounds` plays the step sound, started explicitly by `PlayPage.OnInitialized` (`Sounds.Start()`, idempotent; see `game-rules`) |
| `IAppStorage` (Shared contract) | scoped, host implementation | `GetAsync`/`SetAsync`/`RemoveAsync` for strings (JSON); writes return `ValueTask<bool>` (`false` = not stored) |

- `GameSession` raises `event Action? Changed`. Components get the subscription from the base classes below.
- `ExecutionRunner.PlayAsync`: `while` running → `Step()` → `session.UpdateRun(status, step)` → `await Task.Delay(session.Tempo.Duration, timeProvider, ct)`, in `try/finally` (an exception ends the run `Paused`, never `Running`). `Reset` cancels the token and discards the interpreter, `Stop` cancels and leaves the run `Paused`. Catch `OperationCanceledException` only in the runner.
- Disable controls by state: Play/Debug disabled while running, the editor is read-only during a run, Reset always enabled.
- The hero name never goes into `IAppStorage`.

## Component lifecycle (rules for ALL components)

Base classes remove the subscription boilerplate: `LocalizedComponentBase` (`Shared.UI/Components`, subscribes `LanguageService.Changed`, `Dispose(bool)`, idempotent) and `SessionComponentBase : LocalizedComponentBase` (`Game.UI/Components`, adds `GameSession.Changed`, protected `Session`). `GraphicBase`, `MainLayout`, `LanguageSwitcher`, `GameBoard` use the first, `PlayPage`, `ControlBar`, `ProgramEditor`, `BlockPalette` the second. A razor file repeats the base with `@inherits` (the code-behind names the same base). A language switch calls the virtual `OnLanguageChanged()` before the re-render, for state that depends on the language (`LevelEditor` regenerates an untouched default name there, see `LevelEditorState.RenameDefault`). `MainLayout` is a layout by convention: it declares `[Parameter] Body` itself.

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
var host = builder.Build();
await host.Services.ApplyStoredCultureAsync();        // see localization skill
await host.Services.ApplyStoredSoundAsync();          // stored sound on/off (default off)
await host.Services.LoadCustomLevelsAsync();          // custom levels known before the first render
await host.RunAsync();
```

`LocalStorageAppStorage` uses `IJSRuntime.InvokeAsync<string?>("localStorage.getItem", key)` etc. No custom JS file needed. Wrap calls in try/catch for `JSException` (storage may be blocked or full), return `null` / `false` and degrade gracefully: the game must still be playable without storage.

## Fluent UI Blazor

- No Fluent UI component is used so far (decision D4 of M4: the start page has own cards, the LevelPicker is a native `<select>`; the package is in `Directory.Packages.props` but not referenced or registered). Fluent UI is only a candidate for a slider or dialogs later. Board, palette and program blocks are **custom** components with playful styling.
- Override the look with CSS custom properties / isolated CSS. Don't fight Fluent's internals with `!important` chains.
- Fluent UI's web components and CSS ship with the NuGet package (local `_content`), so no CDN is needed. Verify that after publishing.

## Feature references

- `references/play-page.md`: drag & drop (ProgramEditor), board and animation, start page and routes, level switching and progress, play page components and `GameSession` API.
- `references/level-editor.md`: `GameBoard` edit mode, `EditorPage`/`LevelEditor` and their parts, `ConfirmDialog`, the NavigationLock pattern, fragment navigation, export and import, the scrolling editor columns.

## Accessibility (for kids, but it applies to all)

- Real `<button>` elements for all actions (keyboard-accessible by default), visible focus.
- A `<button>` may contain only phrasing content (no `<div>`, no `GameBoard`). For a clickable card with block content use a `<div>` card with a real button whose `::after` is stretched over the card (StartPage level cards).
- Start and goal distinguishable by **pattern and shape**, not just colour.
- Localized `aria-label`/`<title>` on blocks and SVGs.

## bUnit tests (in `tests/HeroPath.Modules.<X>.Tests` next to the xUnit tests)

```csharp
public class ControlBarTests : BunitContext
{
    public ControlBarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddSingleton<IAppStorage, InMemoryAppStorage>();
    }

    [Fact]
    public void Play_IsDisabled_WhileRunning() { /* Render<ControlBar>(p => p.Add(...)); */ }
}
```

- bUnit 2.x API: `BunitContext`, `Render<T>()`, `Find`, `FindAll`, `WaitForAssertion`.
- Shared fakes live in `tests/HeroPath.Testing` (a plain class library without tests): `InMemoryAppStorage`, `ThrowingAppStorage`, `FakeFileDownload`, `FakeSoundPlayer` (a test that renders `PlayPage` registers `ISoundPlayer` with it), `FakeDocumentLanguage` (`AddSharedUI()` registers a silent `NullDocumentLanguage` with `TryAdd`, so tests need nothing; `Services.AddFakeDocumentLanguage()` only in a test that asserts the language values, after `AddSharedUI()`), `FakeKeyboardShortcuts` (every test that renders `PlayPage` registers `IKeyboardShortcuts` with it; `PressAsync(key, shift)` delivers a key like the platform), `CultureScope`, `CultureBunitContext`. Don't copy them into a test project; the three test projects have `HeroPath.Testing` as a global using.
- bUnit classes that assert localized text derive from `CultureBunitContext` (explicit culture, default `de-CH`, restored on dispose) and carry `[Collection("Culture")]`. `DisposeComponentsAsync()` simulates leaving a page.
- Full user flows across components are E2E tests (`e2e-playwright` skill), not bUnit.
- Priority per Architektur §8: `ProgramEditor` (add, remove, nest), `ControlBar`, `GameBoard` rendering, language switching.
- Test via the click alternative where possible. Drag handlers are covered with bUnit `DragStart/Drop/DragEnd` (`ProgramDragTests`) and real drag & drop by the dedicated E2E `DragDropTests`.
- For the runner, use `FakeTimeProvider`. No real delays in tests.


## Keyboard shortcuts (M6 S6)

- `IKeyboardShortcuts` (Shared contract, host `BrowserKeyboardShortcuts` plus `wwwroot/js/keyboard.js`): `SubscribeAsync(handled, onKey, ct)` takes the table of handled keys (`ShortcutKeys.Handled`, a list of `KeyChord(Key, Shift)` from `HeroPath.Shared`) and returns an `IAsyncDisposable`; a new subscription replaces the old one, a missing platform gives a silent subscription, and a cancellation that ends the call (even while the JS `start` is in flight) stops the listener and frees the `DotNetObjectReference` before it propagates, so no orphan listener keeps calling `preventDefault`. `BrowserKeyboardShortcuts` has no unit test (no host test project exists and nothing may reference a host); `KeyboardTests` covers the filter in the browser. Only `PlayPage` subscribes (in `OnAfterRenderAsync` on the first render, when a hero is chosen and a level is loaded) and releases it in `Dispose`, so the start page and the editor have no shortcuts. A key that arrives after the page is disposed is ignored. `main.play-page` carries `data-shortcuts="active"` once the subscription exists (E2E waits for it before pressing keys, the JS import is asynchronous).
- The filter runs synchronously in JS, because only there can `preventDefault` be decided per key: it ignores `event.repeat`, `isComposing`, Ctrl, Alt and Meta, keys outside the handled set, a target or the active element that is `input`, `textarea`, `select`, `[contenteditable]`, `[role=dialog]` or `dialog`, and Space while a `button`, `a[href]` or `summary` is focused (that key belongs to the control). Handled keys get `preventDefault`, never `stopPropagation`. `keyboard.js` has no own list of keys: `start(callback, keys, shifts)` receives `ShortcutKeys.Handled` as two parallel arrays (plain values, so trimming cannot remove anything) and filters on it, the same table `ShortcutKeys.Resolve` reads. `KeyboardTests.HandledKeys_ArePreventedAndOthersAreNot` dispatches every chord of the table and some unhandled ones and checks `defaultPrevented`.
- `PlayPage.HandleKeyAsync` dispatches through `InvokeAsync` (the call comes from JS), maps the action to the same members as the buttons and awaits `Runner.PlayAsync`; an exception goes to `DispatchExceptionAsync`, so it is not lost silently. The page re-renders through `Session.Changed`.
- `ShortcutHelp` (`Game.UI/Components`, `data-testid="shortcut-help"`, `shortcut-help-toggle` on the `summary`, `shortcut-help-body` on the table box): a `<details>` in the `side-actions` row (beside «Nächstes Level») below `MessageBox`. Its open state is bound (`Open`/`OpenChanged`, `PlayPage` keeps `_helpOpen`): the `summary` click is `preventDefault`ed and toggles the state in C#, so the page can close the list without JS. Closed it is one 44 px button; open, the body floats over the board area to the left of the side panel (`position: absolute` against `.side-panel`, `right: calc(100% + 1.5rem)`, `top: 0`, `max-height: 100%`, scrolls inside itself), so palette, controls, status, message and program stay free and the page never scrolls (below 64rem width it spans the side panel instead). Escape closes an open list first and keeps the selection; Escape again clears the selection (`PlayPage.RunAsync`, `ClearSelection when _helpOpen`). Keys are `<kbd>`, rows pair keys and action; the open state is shown by the dashed border, not by colour alone.
