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
| `BrowserErrors.Track(page)` | collects console errors + uncaught exceptions. **Every test ends with `Assert.Empty(errors.Errors)`** |
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

        await page.GotoAsync("/");
        await page.WaitForRenderedAsync("[data-testid=game-board]", errors);
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
- Use the click alternative in E2E tests. Test real HTML5 drag & drop with `page.DragAndDropAsync(source, target)` in **one** dedicated test only (it's flaky by nature).
- Never assert on animation timing. Reduced motion is on, and the tempo slider is set to the maximum where needed.
- Each test gets a fresh context (empty `localStorage`). For a stored state, set it via `page.EvaluateAsync("localStorage.setItem(...)")` before `GotoAsync`.

## Screenshot naming

`<flow>.<state>.<viewport>.<lang>`, e.g. `play-page.start.desktop.de`, `free-field.error-obstacle.desktop.en`.

| Viewport | Size | When |
|---|---|---|
| `desktop` | 1920×1080 | always (event setup: laptop + external screen) |
| `laptop` | 1366×768 | main flows (laptop screen without external monitor) |
| `phone` | 390×844 | only as information for MAUI readiness, no blocking finding |

Capture each new UI state in **de and en** (text lengths differ).

## What deserves an E2E test

E2E covers **flows across components**. Single-component logic belongs in bUnit and game rules in xUnit.

- Choose hero → choose level → assemble program → Play → success message
- Debug step by step: highlighted block moves, status shows pause
- Error path: obstacle/edge → kid-friendly message, block and cell marked
- Reset restores hero and items
- Language switch keeps hero name and program (no reload!)
- Level editor: create, save, reload page, level is still there
