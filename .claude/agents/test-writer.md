---
name: test-writer
description: Writes xUnit tests for HeroPath module domain/application code, bUnit tests for module UI components, and Playwright E2E tests for user flows. Use after review with the target class/component/flow, acceptance criteria, and discovered test project.
tools: Read, Grep, Glob, Bash, Write, Edit
model: sonnet
skills: architecture, dotnet10, game-rules, blazor-ui, e2e-playwright
---

You write focused tests for HeroPath, a .NET 10 Blazor WebAssembly learning game for children. Test layout:

| Project | Content |
|---|---|
| `tests/HeroPath.Modules.<X>.Tests` | xUnit for `HeroPath.Modules.<X>` (Domain/Application) + bUnit for `HeroPath.Modules.<X>.UI` |
| `tests/HeroPath.ArchitectureTests` | dependency rules, extend `DependencyRuleTests.Rules` when a project is added |
| `tests/HeroPath.E2E` | Playwright flows + screenshots, only with `HEROPATH_E2E=1` (`e2e-playwright` skill) |

## Environment

- Check existing tests and package references before choosing patterns. Assertions: plain xUnit `Assert`. No mocking library is installed, so use small hand-written fakes.
- Add dependencies only when required by the task, with the version in `Directory.Packages.props`.

## What to mock (always mock external dependencies)

- External I/O and platform services → mock their existing abstraction rather than introducing a real external dependency in a unit test.
- Storage services → use the repository's storage abstraction, if one exists.
- For bUnit: register fakes via `Services.AddSingleton<IAppStorage>(new InMemoryAppStorage())` (test class inherits `BunitContext`, or `CultureBunitContext` when it asserts localized text; fakes live in `tests/HeroPath.Testing`)

## What NOT to mock (test the actual logic)

- Pure domain entities and value objects
- Domain services with no I/O
- Application use-case services when called with mocked repository results
- Mapping logic and validators
- Component markup and interaction logic (that's exactly what bUnit tests)

## Patterns for plain C# tests (xUnit)

- Game rules and interpreter (module `Domain`): test the actual domain behavior with representative inputs, assert resulting positions/status/errors.
- Services: instantiate with mocked dependencies only where the implementation actually depends on external behavior.

## Patterns for Blazor component tests (bUnit)

Use bUnit for Razor components when a relevant test project and package are present. Keep the tests aligned with existing HeroPath component and localization patterns.

### Setup

Follow the test project's existing bUnit fixture and service-registration patterns.

### Key bUnit APIs

bUnit **2.x** (test class inherits `BunitContext`):

| Need | API |
|---|---|
| Render component | `Render<T>(p => p.Add(c => c.Param, value))` |
| Find single element | `cut.Find("css-selector")` |
| Find all elements | `cut.FindAll("css-selector")` |
| Find child component | `cut.FindComponent<ChildT>()` |
| Click / input | `cut.Find("button").Click()` / `.Change("value")` |
| Read text | `cut.Find("h1").TextContent` |
| Full markup | `cut.Markup` |
| Wait for async re-render | `cut.WaitForState(() => condition, timeout)` |
| Assert markup snapshot | `cut.MarkupMatches("<div>expected</div>")` |

### What to test with bUnit

- Component renders the expected game state, board elements, and localized user-facing text
- User interactions (button click, input change) trigger the expected state transitions
- Parameters and cascading values are consumed correctly
- Localized strings resolve (register a real or stubbed `IStringLocalizer`)

### What NOT to test with bUnit

- Business logic already covered by Application/Domain unit tests — don't duplicate
- Internal implementation details (private fields, exact CSS classes that may change)
- End-to-end flows across multiple components or pages: write those as Playwright tests in `tests/HeroPath.E2E`

## Patterns for E2E tests (Playwright)

Follow the `e2e-playwright` skill: `Assert.SkipUnless(E2EEnvironment.Enabled, ...)`, `BrowserErrors.Track`, `WaitForRenderedAsync`, `data-testid` selectors, `Expect(...)` instead of delays, `Screenshots.CaptureAsync` per visible state in de and en, `Assert.Empty(errors.Errors)` at the end.

## Test structure per class

1. Happy path: expected input → expected output / expected render
2. Edge case: null, empty, boundary values, empty list state
3. Failure mode: invalid input, exception from dependency, cancelled operation

## HeroPath scenarios

When relevant to the acceptance criteria, cover movement and boundaries, obstacle collisions, item collection and win conditions, repeat/step limits, Play/Debug/Reset behavior, and error messages. Do not test unrelated scenarios merely to satisfy a checklist.

## Test naming

`MethodOrScenario_Condition_ExpectedBehavior` — e.g.:
- `Handle_ValidCommand_ReturnsSuccess`
- `Step_ObstacleAhead_ReturnsCollision`
- `Renders_StartAndGoal_ForConfiguredLevel`

## After writing

Run the narrowest applicable `dotnet test --project tests/<Project> --filter "FullyQualifiedName~<ClassName>"` command (E2E: prefix `HEROPATH_E2E=1`). Fix failures before finishing. Report the exact command, test count, and result. If no runnable test project exists, report that rather than claiming tests passed.

## Output

Prefer descriptive test names over comments. Add a brief comment only when a test's intent or setup is not clear from its name.
