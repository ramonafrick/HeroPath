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
- A single E2E failure with `net::ERR_NETWORK_CHANGED` while loading `_framework` files is transient (the browser lost the wasm download). Rerun that test once before treating it as a finding.
- Screenshots: `artifacts/screenshots/` (gitignored, cleared at the start of each run).
- The README images in `docs/images/` are committed copies of `run.repeat-round.laptop.en` (`play-debug.png`), `start-page.laptop.en`, `success-confetti.laptop.en` (`success.png`) and `editor-built.laptop.en` (`level-editor.png`). When a change alters what they show, copy the new screenshots over them in the same commit.

## Infrastructure (`tests/HeroPath.E2E/Infrastructure`)

| Type | Purpose |
|---|---|
| `AppFixture` (assembly fixture) | starts the host and browser, `NewContextAsync(locale, width, height, reducedMotion = true)` with `ReducedMotion.Reduce` (pass `reducedMotion: false` only where the animation is the point) |
| `E2EEnvironment` | `Enabled`, `BaseUrl`, repository root, screenshot directory |
| `LayoutAssertions` | geometry checks: `AssertNoPageScrollAsync` (no page scroll; waits for `document.fonts.ready` and two animation frames, then polls up to 5 s, and builds the diagnostic message only after the timeout), `AssertSidePanelFitsAsync` (fails with a message when the side panel is missing), `AssertShortcutHelpLeavesControlsFreeAsync` (open help inside the viewport, covering none of picker, palette, controls, status, message, next-level, program), `AssertRepeatHeadersOnOneLineAsync` (select, minus, count, plus, trailing text of every repeat on one line, no overlap, inside the row). Call it in laptop and desktop flows, in DE and EN, with a selected and a nested repeat |
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
- Everything `PublishedSiteTests` calls navigates with relative paths (`GotoAsync("./")`, `GotoAsync("./editor")`): these tests run live under `/HeroPath/`, where `/` leaves the app. The normal suite runs at the root, so `GotoAsync("/")` is fine there.
- Use the click alternative in E2E tests. Real HTML5 drag & drop only in `DragDropTests`, with the manual mouse sequence (hover, `Mouse.DownAsync`, move, measure the target after the drag started, several moves over it, screenshot, `Mouse.UpAsync`): `dragover` needs at least two moves and the drop zones grow after `dragstart`, so `DragAndDropAsync` is not used.
- Drag helpers (`DragDropTests`) must wait for the render state, not for time: all zones carry `data-valid` before measuring, the target box is stable, `data-hover` is set before the screenshot or `Mouse.UpAsync()`. A drop ends its drag in a render that may still be pending, so the next drag first waits until no zone has `data-valid`.
- Long runs (step limit: 1000 moves at 200 ms) use the browser clock: `page.Clock.InstallAsync()` before `GotoAsync`, then `Clock.RunForAsync(250)` in a loop while `status` is `running` (each call fires one pending timer). See `MessageTests.StepLimit_...`.
- Never assert on animation timing. Reduced motion is on, and set the tempo to fast before Play with `page.GetByTestId("control-tempo").FillAsync("2")` (range 0 slow, 1 medium, 2 fast); wait for `status` `data-state` with `Expect` instead.
- Storage failures: to test a failing write, let the app load first and then replace the setter in the page, `page.EvaluateAsync("() => { Storage.prototype.setItem = () => { throw new DOMException('quota', 'QuotaExceededError'); }; }")`. `LocalStorageAppStorage` turns the `JSException` into `false`, so the editor shows «Speichern hat nicht geklappt». That is more reliable than filling the quota (size depends on the browser). Never use `AddInitScriptAsync` for it (the levels would not load either).
- Viewports in flows: a flow that must exist for laptop and desktop is a `[Theory]` with `[InlineData(1366, 768, "laptop")]`, `[InlineData(1920, 1080, "desktop")]` and `NewContextAsync("de-CH", width, height)`; screenshot names use `$"name.{viewport}.de"`. Playwright scrolls an element into view before it clicks it, so after a click the page may have scrolled; do not assert the scroll position of an earlier state.
- `LayoutAssertions.AssertVisibleInEditorPanelAsync(page, selectors...)` (last match of each selector fully inside `editor-panel` and the viewport: the save/import message and the newest list entry) and `AssertInViewportAsync(page, selector)` (after a fragment navigation).
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
- Success animation (`SuccessAnimationTests`, M6 S4, laptop): solving by clicks shows `success-confetti`, with normal motion `next-level` is clickable while it animates, a language switch keeps the same DOM node (expando on the element), Reset removes it. Screenshots freeze the animation mid-way (`document.getAnimations()`: pause confetti at `currentTime = 900`, finish the rest): `success-confetti.laptop.de|en` (normal motion), `success-reduced-motion.laptop.de|en` (dashed dark border with a gold band, checked in the greyscale shot).
- Sound (`SoundToggleTests`, M6 S5, laptop): off at first start, on, language switch keeps state and translates name and tooltip, reload keeps it, 44 px target. Hearing cannot be tested: an init script counts `AudioContext.prototype.createOscillator` calls, so a solved level with sound on makes tones and with sound off makes none, both without browser errors. Screenshots `sound-off|on.laptop.de|en`.
- Level switching (`LevelSwitchTests`): solve `easy-1`, next level, picker switch clears the program, solved check mark on the start page
- Choose hero → choose level → assemble program → Play → success message
- Debug step by step: highlighted block moves (`data-current`), status shows pause (`RunFlowTests`)
- Error path: obstacle/edge → kid-friendly message, block and cell marked
- Reset restores hero and items
- Language switch keeps hero name and program (no reload!)
- Level editor: create, save, reload page, level is still there
- Level files (`LevelFileTests`, M5 S5, laptop DE and EN): a level is seeded with `AddInitScriptAsync`, export with `page.RunAndWaitForDownloadAsync(() => click)` (`SuggestedFilename` ends in `.heropath.json`, `download.SaveAsAsync` to a temp directory outside the repo, the content parses with `LevelJson.Read`; the E2E project references the pure Levels project for that), import with `page.GetByTestId("editor-import").SetInputFilesAsync(path)` (a hidden-looking `input type=file` works, no file chooser needed) → a second level with the same name in the list and on the start page; a garbage file and a 300 KB file give `editor-import-message` with `data-state="error"` and an unchanged list. Screenshots `editor-import-ok|error.laptop.<lang>`. Temp files go to `Path.GetTempPath()`, deleted in `finally`.
- Editor screenshots (laptop and desktop theories): `EditorTests` build, refusal and 20x20 flows plus `EditorManageTests` leave and `CustomLevelTests` run as laptop and desktop theories (`editor-built|unsolvable|resized|20x20|name-empty|hint-strawberry`, `confirm-leave`, `start-custom-levels`, `play-custom-level`, each `.laptop|desktop.de|en`), and `EditorTests` adds `editor-storage-failed` and `editor-limit` (20 seeded levels, the 21st save is refused) at laptop; `LevelFileTests` adds `editor-import-too-large`; `EditorManageTests` adds `start-preselected.laptop.en`. After «Ausprobieren» without a hero the URL ends in `#level-card-custom-…` and the card is in the viewport; the jump link `start-jump-custom` leads to `#custom-levels`. The default name follows the language until the child types one (`editor-name` «Mein Level 1» / «My level 1»).

## Screenshot helper and failure simulation

- `Screenshots.CaptureAsync(page, name, keepMouse: false)` moves the mouse to (0, 0) and waits two animation frames before it shoots, so the cell the last click left the pointer on does not show its hover state (a pale cell in the editor board). Pass `keepMouse: true` while a drag is in progress (`DragDropTests`). `Screenshots.CaptureViewportAsync` takes the same two screenshots (normal and greyscale) of the viewport only, not the full page: use it where the scroll position is the point, e.g. `start-preselected-viewport.<laptop|desktop>.<de|en>` after «Ausprobieren» jumped to the card (the language is switched in the editor first, because the language switcher sits at the top of the page and clicking it scrolls the page back up).
- `editor-delete-failed.laptop.<lang>` (`EditorManageTests`, level seeded with `AddInitScriptAsync`, then `Storage.prototype.setItem` and `removeItem` throw) and `editor-export-failed.laptop.<lang>` (`LevelFileTests`, `URL.createObjectURL` throws, which `file-download.js` calls first, so `BrowserFileDownload` returns `false`). Observed: a storage override installed **after** the app already wrote once is ignored (the interop call keeps the function it resolved first; a save in the same test before the override made the delete succeed). Seed the state with the init script and install the override before the first write of that kind.
- `LayoutAssertions.AssertVisibleInEditorPanelAsync` also requires that the element lies fully inside its scrolling column with 8 px of room, so a message box that took the focus is a whole block.

## Editor layout assertions

- `LayoutAssertions.AssertEditorColumnsFitAsync(page)` (no editor column scrolls, the template select is fully visible; called in the initial and name-empty states in DE and EN) and `AssertToolLabelsFitAsync(page)` (no tool label overflows or breaks inside a word; called in the 20x20 flow). Both are measured in the browser, so a fade or scroll cue cannot hide an overflow.
- Importing the same file twice gives «Käse-Weg (2)» (`LevelFileTests`): the list and the start page show one «Käse-Weg» and one «Käse-Weg (2)».

## Keyboard (`KeyboardTests`, M6 S6, laptop)

- Solves `easy-2` from the first block to success with `page.Keyboard` only after the start page (arrows, Space), reset with `r`; a held key (`DownAsync` repeated, Playwright sets `repeat`) adds one block; focus in the tempo slider, the level select and the name field fires nothing; Space on a focused block button, palette button or `control-play` only triggers that button; Shift+ArrowUp/Down, Backspace/Delete and Escape on a selected block; `d`, `Shift+D`, `R`; Ctrl, Alt, Meta and Shift+Left/Right do nothing; the start page and the editor add no blocks and the play page keeps the program from before. Whether a key was filtered is read from `event.defaultPrevented` through an extra `window` listener (`RecordDefaultPreventedScript`): handled keys are prevented, filtered ones are not. Do not use Ctrl+R (reloads the page).
- Blur the focused control (`document.activeElement.blur()`) before a test presses Space or letters meant for the page: after a click or `FillAsync` the focus sits on a button or the slider.
- `ShortcutHelp_IsCollapsedFitsTheScreenOpensOverTheBoardAndEscapeCloses` (laptop 1366x768, desktop 1920x1080 and short 1366x600, each DE and EN): closed state without page scroll and `AssertSidePanelFitsAsync`, open state over the lower board area with `AssertShortcutHelpLeavesControlsFreeAsync` (inside the viewport, covers no control) and `AssertShortcutHelpLeavesBoardFreeAsync` (covers neither level name, hero badge, item counter nor a goal cell; with `heroMustBeFree` also not the hero), keys still work while it is open, Escape closes the list first (selection kept), the next Escape clears the selection, the summary toggles. Screenshots `shortcut-help-closed|shortcut-help|shortcut-help-with-block.<laptop|desktop|short>.<de|en>`.
- More help tests: `ShortcutHelp_OnTheWideBoardOfLongRoad_...` (laptop DE and EN, desktop, short; `shortcut-help-wide.<viewport>.<lang>`), `ShortcutHelp_OnANarrowCustomBoard_...` (a seeded 3x20 level; `shortcut-help-narrow.<laptop|desktop|short>.de`), `ShortcutHelp_AfterASuccess_...` (desktop, the hero at the goal stays visible; `shortcut-help-success.desktop.de`) and `ShortcutHelp_ClosesWhenPlayOrDebugStarts_AndStaysClosedForTheRun` (Debug and Play by button and by key: the list closes, the summary is `aria-disabled` and does not open during the run, opens again after Reset). The list can cover the start cell and the lower rows by design; goal cells are never covered in these levels.
- `HandledKeys_ArePreventedAndOthersAreNot`: dispatches every chord of `ShortcutKeys.Handled` (the E2E project references the pure Game project for it) as a synthetic `keydown` on the body and expects `preventDefault`, and unhandled keys (Shift+ArrowLeft/Right, `a`, Enter, Tab, ...) not.
- Play page layout (`PlayPageLayoutTests`, laptop and desktop, DE and EN): a 20-character name (`W`s), sound toggle on, all seven levels solved, back to `strawberries`, pizza detour: the success message (hero name, pizza, all-solved sentence) plus «Nächstes Level» and the shortcut summary fit without page scroll, the side panel does not overflow, the message does not scroll inside its box, and the open help covers no control. The real heights (header, side panel end, last action row end, spare room) go to one file per case, `artifacts/screenshots/play-layout.message-long.<viewport>.<lang>.txt` (`play-layout.shortcut-help.<viewport>.<lang>.txt` for the page scroll height of the short viewport in `KeyboardTests`). Screenshots `message-long|message-long-help.<laptop|desktop>.<de|en>`.

## Published site (`PublishedSiteTests`, M6 S9)

Opt-in against a site finished with `deploy/finish-site.ps1 -BaseHref / -Pwa` and served on `http://localhost:<port>/` by `deploy/Start.ps1` (setup and commands in the `release` skill). Skipped unless `HEROPATH_E2E=1` and `HEROPATH_PUBLISHED_URL` are set; also set `HEROPATH_BASE_URL` to the same URL so `AppFixture` does not start the dev host. Run only these with `--filter-class HeroPath.E2E.PublishedSiteTests` (the CI job `published` does). The tests build their own context (`BaseURL` = the published URL), an init script turns every `securitypolicyviolation` event into a console error so `BrowserErrors` catches it, and the offline test uses `context.SetOfflineAsync(true)` after the worker is `activated` and a reload made the page controlled (the worker does not call `clients.claim`). Code evaluated through Playwright bypasses the CSP, so only DOM-injected scripts prove it works. Screenshots `published-offline.start-page|editor-deep-link|success.laptop.de`.
