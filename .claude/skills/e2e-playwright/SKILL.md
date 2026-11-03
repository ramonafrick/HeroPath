---
name: e2e-playwright
description: End-to-end tests for HeroPath with Microsoft.Playwright (.NET) and xUnit v3 in tests/HeroPath.E2E — opt-in via HEROPATH_E2E=1, AppFixture that starts the web host and Chromium, BrowserErrors, WaitForRenderedAsync, stable selectors, screenshots (normal + greyscale) in artifacts/screenshots for the visual-checker agent, and what to cover per user flow. Use when writing or running E2E tests, when a feature changes a user-visible flow, or when preparing screenshots for a visual check.
---

# E2E tests with Playwright (.NET)

## Running

```bash
dotnet test --solution HeroPath.slnx                                  # E2E tests are skipped (fast default)
HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E                # starts the web host + Chromium, writes screenshots
HEROPATH_E2E=1 HEROPATH_BASE_URL=http://127.0.0.1:5198 dotnet test --project tests/HeroPath.E2E   # against an already running app
```

- PowerShell: `$env:HEROPATH_E2E = '1'; dotnet test --project tests/HeroPath.E2E`.
- The first run downloads Chromium via `Microsoft.Playwright.Program.Main(["install", "chromium"])`. No Node or `pwsh` needed.
- The host starts with `dotnet run` on `http://127.0.0.1:5199` (startup timeout 3 min). If a run fails with "address in use", kill the leftover `HeroPath.Web` process.
- Screenshots: `artifacts/screenshots/` (gitignored, cleared at the start of each run).

## Infrastructure (`tests/HeroPath.E2E/Infrastructure`)

| Type | Purpose |
|---|---|
| `AppFixture` (assembly fixture) | starts the host and browser, `NewContextAsync(locale, width, height)` with `ReducedMotion.Reduce` |
| `E2EEnvironment` | `Enabled`, `BaseUrl`, repository root, screenshot directory |
| `LayoutAssertions` | geometry checks: `AssertNoPageScrollAsync` (no page scroll), `AssertRepeatHeadersOnOneLineAsync` (select, minus, count, plus, trailing text of every repeat on one line, no overlap, inside the row). Call it in laptop and desktop flows, in DE and EN, with a selected and a nested repeat |
| `BrowserErrors.Track(page)` | collects console errors + uncaught exceptions. **Every test ends with `Assert.Empty(errors.Errors)`** |
| `page.StartGameAsync(errors, levelId = "easy-2", hero = null, name = null)` | opens the start page, optionally picks hero (`max`/`lisa`) and name, picks the level, clicks `start-go` and waits for the play board. Every test that needs the play page starts here (most rely on the `easy-2` layout) |
| `page.WaitForRenderedAsync(selector, errors)` | waits for Blazor rendering; on timeout writes `failure.png` and puts browser errors into the message |
| `Screenshots.CaptureAsync(page, name)` | `name.png` + `name.greyscale.png` (achromatopsia emulation via CDP) |

## Writing a test

```csharp
public class ProgramFlowTests(AppFixture app)
{
    [Fact]
    public async Task Play_ReachesCastle_InFreeField()
    {
        Assert.SkipUnless(E2EEnvironment.Enabled, "Set HEROPATH_E2E=1 to run E2E tests.");

        await using var context = await app.NewContextAsync();
        var page = await context.NewPageAsync();
        var errors = BrowserErrors.Track(page);

        await page.StartGameAsync(errors);                          // start page -> easy-2 -> play page
        await page.GetByTestId("palette-right").ClickAsync();          // click alternative, not drag
        await page.GetByTestId("control-play").ClickAsync();
        await Expect(page.GetByTestId("status")).ToHaveAttributeAsync("data-state", "succeeded");

        await Screenshots.CaptureAsync(page, "free-field.success.desktop.de");
        Assert.Empty(errors.Errors);
    }
}
```

- `Expect(...)` comes from `using static Microsoft.Playwright.Assertions;`. It retries automatically, so never use `Task.Delay`.
- **Selectors**: `data-testid` on interactive elements and state carriers (`data-state`, `data-step`), defined in the component markup. No CSS classes or localized texts as selectors (texts change with the language).
- Use the click alternative in E2E tests. Real HTML5 drag & drop only in `DragDropTests`, with the manual mouse sequence (hover, `Mouse.DownAsync`, move, measure the target after the drag started, several moves over it, screenshot, `Mouse.UpAsync`): `dragover` needs at least two moves and the drop zones grow after `dragstart`, so `DragAndDropAsync` is not used.
- Drag helpers (`DragDropTests`) must wait for the render state, not for time: all zones carry `data-valid` before measuring, the target box is stable, `data-hover` is set before the screenshot or `Mouse.UpAsync()`. A drop ends its drag in a render that may still be pending, so the next drag first waits until no zone has `data-valid`.
- Long runs (step limit: 1000 moves at 200 ms) use the browser clock: `page.Clock.InstallAsync()` before `GotoAsync`, then `Clock.RunForAsync(250)` in a loop while `status` is `running` (each call fires one pending timer). See `MessageTests.StepLimit_...`.
- Never assert on animation timing. Reduced motion is on, and set the tempo to fast before Play with `page.GetByTestId("control-tempo").FillAsync("2")` (range 0 slow, 1 medium, 2 fast); wait for `status` `data-state` with `Expect` instead.
- Each test gets a fresh context (empty `localStorage`). For a stored state that must exist before the app starts (custom levels are loaded in `Program.cs`), use `page.AddInitScriptAsync` with `localStorage.setItem(...)`; note it runs on every navigation including reloads, so guard it with `if (localStorage.getItem(key) === null)`. `page.EvaluateAsync` before `GotoAsync` only works on an already loaded origin.

## Screenshot naming

`<flow>.<state>.<viewport>.<lang>`, e.g. `language.start-page.desktop.de`, `free-field.error-obstacle.desktop.en`.

| Viewport | Size | When |
|---|---|---|
| `desktop` | 1920×1080 | always (event setup: laptop + external screen) |
| `laptop` | 1366×768 | main flows (laptop screen without external monitor) |
| `phone` | 390×844 | only as information for MAUI readiness, no blocking finding |

Capture each new UI state in **de and en** (text lengths differ).

## What deserves an E2E test

E2E covers **flows across components**. Single-component logic belongs in bUnit and game rules in xUnit.

- Start page (`StartPageTests`, screenshots `start-page.<viewport>.<lang>`, `start-page-lisa.laptop|desktop.<lang>`, `start-page-solved.laptop.<lang>`, `start-page-previews.<viewport>.de`; all previews have cells of at least 14 px and equal heights): choose hero, name and level, `/play` redirect without a choice, language switch keeps the form
- Items (`ItemTests`, `strawberries` level at 1366x768 in DE and EN): all items and counters visible at the start, Debug collects a strawberry (`item-x-y` `data-collected`, counter 1 von 3, hidden under reduced motion), Reset brings it back, a pizza detour ends in the pizza success text with the hero name, a straight run to the goal gives `ItemsMissing`. Switch the app language with the `language-en` button, the browser locale does not change it. Screenshots `items-start|collected|success.laptop.<lang>`, `items-missing.desktop.de`
- Coverage (`LevelCoverageTests`, helper `LevelSolutions.SolveAsync` plays the known solutions of all 7 levels with palette clicks): all levels solved gives «Du hast alle Level geschafft!» (`all-solved.laptop.de|en`), start page EN laptop (`start-page.laptop.en`, castle preview cells at least 14 px, «Los!» in the viewport), play page of `castle-maze`, `long-road`, `small-maze` at laptop and desktop in DE and EN without page scroll (`play-<id>.<viewport>.<lang>`, `LayoutAssertions.AssertNoPageScrollAsync`), solved level picker not clipped (`level-picker-solved.laptop.de|en`)
- Wide board (`LevelCoverageTests`): `long-road` at 1366x768 has cells of at least 48 px, repeat headers on one line, no page scroll
- Custom levels (`CustomLevelTests`, M5 S2): seeds `heropath.levels` with one valid and one broken entry via `AddInitScriptAsync`, the start page shows the card under `custom-levels`, the level is played and solved by clicks, the check mark appears, a reload keeps the level. Screenshots `start-custom-levels.desktop|laptop.<lang>`, `play-custom-level.laptop.<lang>`
- Level editor (`EditorTests`, M5 S3): build, save and play (start page, «Level bauen», shrink to 5x5, tool clicks for tree, rock, pond, strawberry, pizza, require-all, name, save, back to start page, card under «Meine Levels», solve by palette clicks, check mark, reload keeps it), desktop fit (cell 40 to 44 px), refusals (walled goal gives `unsolvable` with a disabled save and a reason, empty name gives `invalid`, shrinking reports «2 Dinge wurden entfernt.», the 11th strawberry is refused with a hint), and the 20x20 timing (10 strawberries plus require-all; the time from the 10th strawberry click until `data-min-steps` changes is written to `artifacts/screenshots/editor-20x20.timing.txt` and the test output and must be at most 1 s; no page scroll at 1366x768, cells at least 28 px). Screenshots `editor-new.laptop|desktop.<lang>`, `editor-built|unsolvable|resized|20x20.laptop.<lang>`. The helper `OpenEditorAsync` goes through the start page like a child.
- Managing levels (`EditorManageTests`, M5 S4, laptop DE and EN): template easy-1 copied, changed, saved while the standard level stays unchanged on the play page; delete with dialog (focus on «Ja», cancel and Escape keep the level, confirm removes it, reload keeps it removed); clear with dialog; «Ausprobieren» without a hero (start page with the custom card `aria-pressed`, then «Los!», `play-edit` 44 px, back to the editor) and with a hero (straight to play, hero kept); leaving a dirty editor (cancel stays, confirm leaves, a clean or saved editor never asks). A modal blocks the language switcher, so switch the language before opening the dialog. Screenshots `editor-template`, `confirm-delete`, `confirm-leave`, `play-edit-button` (`.laptop.de|en`), `start-preselected.laptop.de`
- Level switching (`LevelSwitchTests`): solve `easy-1`, next level, picker switch clears the program, solved check mark on the start page
- Choose hero → choose level → assemble program → Play → success message
- Debug step by step: highlighted block moves (`data-current`), status shows pause (`RunFlowTests`)
- Error path: obstacle/edge → kid-friendly message, block and cell marked
- Reset restores hero and items
- Language switch keeps hero name and program (no reload!)
- Level editor: create, save, reload page, level is still there
