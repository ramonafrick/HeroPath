---
name: localization
description: HeroPath localization and user-facing text — IStringLocalizer with resx (German as default with Swiss spelling, English), resource location and key naming, culture switching in Blazor WebAssembly without reloading (hero name must survive), stored language via IAppStorage, localized app name (HeldenWeg / Hero's Path), and the kid-friendly tone for messages and errors. Use whenever a user-visible string is added or changed, when mapping Core errors to messages, or when working on the LanguageSwitcher.
---

# Localization and texts

Docs: `docs/Projekt_Kontext.md` §2/§4/§5, message examples in `docs/Projektidee.md` («Fehler und Meldungen»).

## Rules

- **No user-visible text in C# or Razor.** Everything goes through `IStringLocalizer<T>` (module texts: `IStringLocalizer<GameResources>` etc., shared texts: `IStringLocalizer<SharedResources>`), in both languages, in the same change.
- Code, keys and resx comments are English. Values are German (default) and English.
- German = **Swiss spelling**: «ss» instead of «ß», guillemets «…». The `de` values must never contain `ß`.
- App display name: `App_Title` = «HeldenWeg» / «Hero's Path». The project/namespace stays `HeroPath`.

## Files

Each module owns its texts (inspired by MiniMate), shared texts live in Shared.UI:

```
Shared/HeroPath.Shared.UI/Resources/SharedResources.cs (+ .resx, .en.resx)     App_Title, language names, generic buttons
Modules/Game/HeroPath.Modules.Game.UI/Resources/GameResources.cs (+ …)       blocks, controls, execution messages
Modules/Levels/HeroPath.Modules.Levels.UI/Resources/LevelsResources.cs (+ …) level names (Level_<id>), editor texts
```

- `Game.UI` keys so far: `Block_Up|Down|Left|Right`, `Palette_Title`, `Program_Title`, `Program_Empty`, `Program_Remove` ({0} = block number), `Program_Block` ({0} = number, {1} = direction), `Program_Clear`.
- `<X>Resources.cs` is an empty marker class (`public sealed class GameResources;`) in namespace `HeroPath.Modules.Game.UI.Resources`. Use `IStringLocalizer<GameResources>`, no ResX designer classes.
- Neutral resx = German. `<NeutralLanguage>` is not needed.
- Only UI projects contain resx. Pure module projects return error kinds and numbers, never texts.
- `Hosts/HeroPath.Web/HeroPath.Web.csproj` already has `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` (required for switching culture at runtime). **Do not** set `InvariantGlobalization`.
- Supported cultures: `de-CH`, `en`. Unknown or stored garbage → `de-CH`.

## Key naming

`Area_Purpose[_Variant]`, PascalCase, e.g.

| Key | de | en |
|---|---|---|
| `App_Title` | HeldenWeg | Hero's Path |
| `Block_Up` | Hoch | Up |
| `Block_Repeat` | Wiederhole {0} mal | Repeat {0} times |
| `Error_Obstacle_Tree` | Autsch! Da steht ein Baum im Weg. Schau dir Schritt {0} noch einmal an. | Ouch! There's a tree in the way. Take another look at step {0}. |
| `Error_OutOfBounds` | Hoppla! Hier ist die Welt zu Ende. Schritt {0} führt aus dem Spielfeld. | Oops! The world ends here. Step {0} leads off the board. |
| `Error_EmptyRepeat` | Leg zuerst Bausteine in den Wiederhole-Block. | Put some blocks into the repeat block first. |
| `Success` | Geschafft, {0}! Du hast die Burg erreicht. | You did it, {0}! You reached the castle. |
| `Level_free-field` | Freies Feld | Open Field |

- **One key per full sentence and variant** (`Error_Obstacle_Tree`, `_Rock`, `_Pond`). Never build sentences by concatenating fragments: grammar differs between languages.
- Placeholders `{0}`, `{1}`: document in the resx `<comment>` what they contain (e.g. `{0} = step number`).
- Plural: separate keys (`Error_ItemsMissing_One`, `Error_ItemsMissing_Other`).
- Every `ExecutionError` from Core has exactly one mapping in a UI helper (`ExecutionMessageFormatter`), tested with bUnit or xUnit.

## Tone (target group 11–13)

- Use «du», short sentences, encouraging, never blaming («Probier es nochmal!» instead of «Falsch»).
- The message says **what** happened and **where** to look (step number, highlighted block).
- Use the hero's name where the docs do («Geschafft, Lisa!»).
- Use simple words, no technical terms. «Programm», «Baustein» and «Debug» are the only allowed ones (they are learning goals, see the glossary).
- Don't use emoji in resx. Visual feedback (confetti, icons) belongs to the UI.

## Culture switching without reload

A `forceLoad` reload would wipe the hero name, program and level (memory only). Instead:

```csharp
public sealed class LanguageService(IAppStorage storage)
{
    public event Action? Changed;

    public async Task SetAsync(string cultureName, CancellationToken cancellationToken)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        await storage.SetAsync(StorageKeys.Language, cultureName, cancellationToken);
        Changed?.Invoke();
    }
}
```

- Real API (`HeroPath.Shared.UI.Localization`): `LanguageService` (scoped; `Current`, `ApplyStoredAsync`, `SetAsync`, `Changed`, `Normalize`); storage key `StorageKeys.Language`. Unsupported values are normalized to `de-CH`; a throwing `IAppStorage` is swallowed and the switch still happens.
- Components with texts subscribe to `Changed` in `OnInitialized`, unsubscribe in `Dispose`, and re-render via `InvokeAsync(StateHasChanged)`. A page with `<PageTitle>` does the same (see `PlayPage`).
- At startup (`Program.cs`, after `Build()`, before `RunAsync()`) call `await host.Services.ApplyStoredCultureAsync()`. Default is `de-CH`.
- Tests that switch culture change process-wide state: put them in `[Collection("Culture")]` and restore the cultures afterwards (`CultureScope` in `HeroPath.Shared.Tests`).
- Only the language is stored, never the hero name.

## Checklist for a text change

1. Key in the owning `<X>Resources.resx` **and** `<X>Resources.en.resx` (module texts in the module, cross-module texts in `SharedResources`).
2. German without `ß`, guillemets, «du» form.
3. Placeholders documented in `<comment>`.
4. Used via `Localizer["Key"]` / `Localizer["Key", arg]`, no fallback string in code.
