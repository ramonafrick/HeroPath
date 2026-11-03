---
name: dotnet10
description: Modern .NET 10 / C# 14 conventions for HeroPath — solution setup (HeroPath.slnx, Central Package Management, TreatWarningsAsErrors, editorconfig), project files and module projects (see architecture skill), C# 14 features (field keyword, extension members, null-conditional assignment), records and value objects, async/cancellation and TimeProvider, DI, JSON with embedded level resources, xUnit v3 and bUnit testing, and dotnet CLI commands. Use for general C# code in any HeroPath project, when creating the solution or adding packages/projects, or when fixing compiler/analyzer errors.
---

# .NET 10 / C# 14 — HeroPath conventions

Source of truth for architecture and requirements: `docs/Architektur C4.md`, `docs/Projekt_Kontext.md`. This skill covers *how* to write C#. Related skills: `architecture` (modules, where code goes), `game-rules` (game behaviour), `blazor-ui` (components), `localization` (texts), `e2e-playwright` (E2E).

## Solution setup

The repo-level files and the module projects exist. Keep them that way:

| File | Rule |
|---|---|
| `HeroPath.slnx` | XML solution format in the repo root. Add projects with `dotnet sln HeroPath.slnx add <path>`. |
| `Directory.Build.props` | `net10.0`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `AnalysisLevel latest`, `EnforceCodeStyleInBuild true`, `GenerateDocumentationFile true` with `NoWarn CS1591` (needed so IDE0005 "unused using" is reported on build — it **is** a build error here), `LangVersion` not set (default = C# 14). Don't repeat these in `.csproj` files. Templates from `dotnet new` add `TargetFramework`/`Nullable`/`ImplicitUsings` — remove them. |
| `Directory.Packages.props` | **Central Package Management** with transitive pinning — every version lives here (`Label="Blazor"` / `Label="Testing"`). `.csproj`: `<PackageReference Include="X" />` without `Version`. |
| `.editorconfig` | file-scoped namespaces and `_camelCase` private fields as **warnings** (= build errors), modern-syntax preferences as suggestions, 4 spaces (2 for XML/JSON). |
| `.gitignore` | from `dotnet new gitignore` (covers `bin/`, `obj/`, `.vs/`, `artifacts/`). |
| `global.json` | pins the .NET 10 SDK; `"test": { "runner": "Microsoft.Testing.Platform" }` so `dotnet test` runs xUnit v3 natively. |

Adding a package: add `<PackageVersion>` in `Directory.Packages.props`, then `<PackageReference>` in the project. Only add packages that are justified in the docs (Fluent UI Blazor, Localization, xUnit v3, bUnit, Playwright) or approved by the user. Pure module projects (`Shared`, `Modules.Levels`, `Modules.Game`) stay package-free. No CDN references, no packages that pull in external runtime assets.

### Projects

Layout, dependency rules and "where does it go" are in the **`architecture`** skill. C#-specific points:

- Pure projects: `Microsoft.NET.Sdk` (`dotnet new classlib`). UI projects: `Microsoft.NET.Sdk.Razor` (`dotnet new razorclasslib`, then delete the sample `Component1`, `ExampleJsInterop`, `wwwroot/*`) with `<SupportedPlatform Include="browser" />`.
- Test projects: `Microsoft.NET.Sdk`, `OutputType Exe`, `IsPackable false`.
- `ProjectReference` paths are relative (`..\..\Levels\HeroPath.Modules.Levels\...`). Add to `HeroPath.slnx` with `--solution-folder src/Modules/<X>`.

## C# style

- Code, identifiers and comments in **English**. User-facing text never in code — only via resx (`de`, `en`).
- Primary constructors for DI, collection expressions `[]`, `required` members, pattern matching, switch expressions.
- `readonly record struct` for small value types (`Position`), `record` for immutable results (`MoveResult`, `ExecutionStep`), `enum` for closed sets (`Direction`, `ObstacleKind`, `MoveOutcome`, `ExecutionState`).
- Interfaces only where they pay off (storage, level source, platform services) — not for every class.
- Keep classes small; game logic belongs in the module's `Domain` folder, never in Razor markup.

### C# 14 features worth using

```csharp
// field keyword — validation without a manual backing field
public int Count
{
    get;
    init => field = value is >= ProgramLimits.MinRepeatCount and <= ProgramLimits.MaxRepeatCount
        ? value
        : throw new ArgumentOutOfRangeException(nameof(Count));
}

// Extension members
public static class DirectionExtensions
{
    extension(Direction direction)
    {
        public Position Offset => direction switch
        {
            Direction.Up => new(0, -1),
            Direction.Down => new(0, 1),
            Direction.Left => new(-1, 0),
            Direction.Right => new(1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }
}

// null-conditional assignment
session?.CurrentStep = step;
```

## Domain limits as constants

Limits from `Projekt_Kontext.md` live as named constants in the owning module, not as magic numbers in UI or tests:

| Limit | Value | Constant |
|---|---|---|
| Max executed steps per run | 1000 | `ProgramLimits.MaxExecutedSteps` (Game) |
| Repeat count | 1–20 | `ProgramLimits.MinRepeatCount` / `MaxRepeatCount` (Game) |
| Repeat nesting depth | max. 2 | `ProgramLimits.MaxRepeatNestingDepth` (Game) |
| Grid width / height | 3–20, default 10×10 | `LevelLimits.*` (Levels) |

## Errors

- **Game errors are not exceptions.** Hitting an obstacle, leaving the grid, step limit or goal not reached are normal outcomes: model them as values (`MoveOutcome`, `ExecutionError` in `ExecutionStep`). The UI maps them to localized kid-friendly messages.
- Exceptions only for programming errors / invalid input at boundaries: `ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException.ThrowIfLessThan`, etc.
- Invalid level JSON (custom levels from storage/import) → validate and return a result with errors; never crash the app. Catch broadly only at the UI boundary (`ErrorBoundary`).

## Async, cancellation, time

- I/O methods (storage, JS interop): `async Task`/`ValueTask`, suffix `Async`, last parameter `CancellationToken cancellationToken`.
- Never `.Result`, `.Wait()`, `async void` (except Blazor event handlers — prefer `Task`-returning handlers anyway).
- No `ConfigureAwait(false)` (Blazor sync context in UI projects, and services in `Application` are awaited from components). Domain code is synchronous by design (`Interpreter.Step()`).
- Play loop / tempo (`ExecutionRunner` in `Modules.Game/Application`): use `TimeProvider` + `Task.Delay(delay, timeProvider, cancellationToken)` and a `CancellationTokenSource` for Reset/Stop. Tests use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`, already in the Testing group, referenced only by `HeroPath.Modules.Game.Tests`) instead of real delays.

## DI

- Each module registers itself via `Add<X>Module()` in its UI project. `GameSession` and `ExecutionRunner` are scoped, `TimeProvider.System` via `TryAddSingleton`. Details are in the `architecture` skill.
- The host registers platform implementations: `Hosts/HeroPath.Web` → `services.AddScoped<IAppStorage, LocalStorageAppStorage>()`. A future MAUI host does the same with its own implementation, and modules must not know which.
- In Blazor WASM "scoped" behaves like singleton per browser tab; that's fine for `GameSession`.

## JSON and levels

- `System.Text.Json` only, `JsonSerializerOptions.Web` defaults.
- Standard levels: JSON files in `Modules.Levels/Infrastructure/Levels/`, included as `<EmbeddedResource Include="Infrastructure\Levels\*.json" />` (already in the csproj), loaded via `typeof(LevelDefinition).Assembly.GetManifestResourceStream(...)`. No `HttpClient`/`wwwroot` for levels.
- Use a source-generated `JsonSerializerContext` (`[JsonSerializable(typeof(LevelDefinition))]`) — WebAssembly publish trims, and reflection-based serialization is fragile there.
- Model level JSON as explicit records; keep property names stable because custom levels in `localStorage` (and later exports) depend on them.
- The hero's name is never serialized or persisted (privacy, see docs).

## Logging

Keep it minimal — it's a client-side kids' app. `ILogger<T>` with structured placeholders where useful for debugging; never log the hero's name. No telemetry, no external sinks.

## Testing

- One test project per module (`tests/HeroPath.Modules.<X>.Tests`) with **xUnit v3** for domain/application and **bUnit** for that module's components. Package `xunit.v3.mtp-v2` (Microsoft Testing Platform v2, matches the MTP runner in `global.json`), test projects are `OutputType Exe`, `Xunit`/`Bunit` are global usings in the csproj.
- `tests/HeroPath.ArchitectureTests` enforces the module dependency rules. It reads DLL metadata instead of loading assemblies, because Windows Application Control on this machine can block loading freshly built DLLs. Keep it that way.
- `tests/HeroPath.E2E` (Playwright) only runs with `HEROPATH_E2E=1` (see the `e2e-playwright` skill).
- A test project without any test makes `dotnet test` fail (MTP exit code 8). Every test project needs at least one real test.
- Every logic change in a module gets a test (rule from `Projekt_Kontext.md`).
- `[Theory]` + `[InlineData]` for movement/obstacle/edge tables; cover nested repeat, step limit, items, `RequireAllItems`, level validation.
- Assertions: plain xUnit `Assert` by default. Don't add FluentAssertions (v8+ has a commercial license, conflicts with an MIT kids' project without discussion).
- Test names: `Method_Scenario_ExpectedResult`.
- `TestContext.Current.CancellationToken` for async tests.

## CLI cheat sheet

```bash
dotnet build HeroPath.slnx
dotnet test --solution HeroPath.slnx
dotnet test --project tests/HeroPath.Modules.Game.Tests --filter "FullyQualifiedName~Interpreter"
HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E    # E2E + screenshots
dotnet format HeroPath.slnx --verify-no-changes
dotnet watch --project src/Hosts/HeroPath.Web             # local dev with hot reload
dotnet publish src/Hosts/HeroPath.Web -c Release          # static files for GitHub Pages / mini-server
dotnet list HeroPath.slnx package --outdated
dotnet list HeroPath.slnx package --vulnerable
```

Remember: the published WebAssembly app does not run via `file://` — serve the publish folder with a web server.
