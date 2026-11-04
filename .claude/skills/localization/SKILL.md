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

- Keys so far. Shared: `App_Title`, `Language_Switch_Label`, `Language_German`, `Language_German_Short`, `Language_English`, `Language_English_Short`, `Sound_Label|TurnOn|TurnOff` (the sound switch: constant name, tooltip names the action), `Graphic_<TypeName>` (`HeroMax`, `HeroLisa`, `StartField`, `GoalCastle`, `ObstacleTree`, `ObstacleRock`, `ObstaclePond`, `ItemStrawberry`, `ItemPizza`). Levels: `Board_Label`, `LevelPicker_Label`, `LevelPicker_GroupCustom`, `Level_<id>` (`Level_free-field`, `Level_easy-1`, …). Game: `Block_Up|Down|Left|Right`, `Palette_Title`, `Program_Title`, `Program_Clear`, `Program_Remove` ({0} = block number such as 3.2), `Program_MoveUp|MoveDown` ({0} = block number), `Palette_Repeat`, `Palette_DropDelete`, `Block_Repeat` ({0} = count), `Repeat_Before|After`, `Repeat_Decrease|Increase` ({0} = block number), `Repeat_Round` ({0} = round, {1} = count), `InsertTarget_Root|Repeat` ({0} = block number), `Program_Block` ({0} = number, {1} = direction), `Program_Current`, `Program_Error`, `Controls_Title`, `Control_Play|Debug|Reset`, `Tempo_Label`, `Tempo_<Tempo>` (`Slow|Medium|Fast`), `Program_Hint`, `Message_Obstacle_Tree|Rock|Pond` ({0} = block number), `Message_OutOfBounds` ({0} = block number), `Message_GoalNotReached` and `Message_Succeeded` ({0} = hero name), `Message_Succeeded_Pizza_One` ({0} = hero name) and `_Other` ({0} = hero name, {1} = pizza count, FR22), `ItemCounter_Strawberry_One|Other` and `ItemCounter_Pizza` ({0} = collected, {1} = total), `Message_ItemsMissing_One|Other` ({0} = count), `Message_StepLimitExceeded` ({0} = step limit), `Message_EmptyRepeat|EmptyProgram|ProgramFull|NestingTooDeep`, `Start_Title|Intro|HeroGroup|NameLabel|NameHint|LevelsTitle|CustomLevelsTitle|Go`, `Hero_Change`, `Hero_BadgeLabel`, `Level_Next`, `Level_Solved`, `Message_AllSolved`, `Status_Label`, `Status_<RunStatus>` (`Ready|Running|Paused|Failed|Succeeded`). Every `Tempo` and `RunStatus` value must have its key in both resx files (`GameResourcesTests`).
- `Message_EmptyProgram` names both ways to fill the program («Zieh Bausteine ins Programm oder klick sie an.» / «Drag blocks into your program or click them.»). The empty program area in `ProgramEditor` is a text-free drawing, so no text is duplicated.
- Editor keys (M5 S3, Levels): `Editor_Title|Intro|Tools_Title|Size_Title|Width|Height|NameLabel|NameHint|RequireAll|RequireAll_Disabled|New|DefaultName` ({0} = number), `Editor_Tool_<EditorTool>`, `Editor_Width|Height_Decrease|Increase`, `Editor_Status_Label`, `Editor_Status_Solvable_One|Other` ({0} = steps), `Editor_Status_GoalUnreachable|StrawberriesUnreachable|NameMissing|Invalid`, `Editor_Status_TooManySteps` ({0} = step limit), `Editor_Save`, `Editor_Save_Blocked|Saved|StorageFailed|Invalid`, `Editor_Save_LimitReached` ({0} = 20), `Editor_Hint_<EditHint>` (`TooManyStrawberries|TooManyPizzas` take {0} = limit), `Editor_Removed_One|Other` ({0} = count), `Editor_List_Title|Empty|Open` ({0} = name), `Cell_Label` ({0} = column, {1} = row, both from 1, {2} = content), `Cell_Empty|Start|Goal|Goal_Strawberry|Goal_Pizza|Tree|Rock|Pond|Strawberry|Pizza`. Game: `Start_Build`, `Editor_Back`. Shared: `Graphic_ToolEraser`. M5 S4 keys: Shared `Confirm_Yes`, `Confirm_Cancel`; Levels `Editor_Clear|Try|Delete|Delete_Failed|Template_Label|Template_Placeholder`, `Editor_List_Delete` ({0} = name), `Confirm_<Delete|Clear|Leave|Discard>_<Title|Message|Yes>` (`Confirm_Delete_Message` {0} = level name as text), `Confirm_Stay`; Game `Play_Edit`. `LevelsResourcesTests` checks every enum-derived key, equal keys and placeholders in DE and EN and no «ß». The default name «Mein Level N» is built when a level is opened and follows a language switch (`LevelEditorState.RenameDefault`, called from `LevelEditor.OnLanguageChanged`) as long as the child did not type, save or open it: «My level N» in English. After that it is the child's text and stays. Editor tone: say what is wrong and what to do («Mach einen Weg frei.»), never show the solution path.
- M5 S5 keys (Levels): `Editor_Export`, `Editor_List_Export` ({0} = level name), `Editor_Export_Failed`, `Editor_Import_Title|Button|Hint`, `Editor_Import_Ok` ({0} = level name as text), `Editor_Import_Invalid|TooLarge|Unreadable|NotSavable|StorageFailed`, `Editor_Export_Hint` (privacy hint next to the export buttons), Game `Start_JumpCustom` («Zu meinen Levels»), `Editor_Import_LimitReached` ({0} = 20). The «ß» to «ss» mapping of `LevelEditing.ExportFileName` is file-name mapping only, UI texts stay free of «ß».
`<X>Resources.cs` is an empty marker class (`public sealed class GameResources;`) in namespace `HeroPath.Modules.Game.UI.Resources`. Use `IStringLocalizer<GameResources>`, no ResX designer classes.
- Neutral resx = German. `<NeutralLanguage>` is not needed.
- Only UI projects contain resx. Pure module projects return error kinds and numbers, never texts.
- `Hosts/HeroPath.Web/HeroPath.Web.csproj` already has `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` (required for switching culture at runtime). **Do not** set `InvariantGlobalization`.
- **Satellite assemblies gotcha**: Blazor WebAssembly loads the satellite assemblies (the `en` resx) only for the culture active at startup, so switching the language at runtime would silently keep the German texts (bUnit does not notice, it runs on the .NET runtime). `Hosts/HeroPath.Web/wwwroot/index.html` therefore starts Blazor manually (`autostart="false"` plus `Blazor.start({ configureRuntime: dotnet => dotnet.withConfig({ loadAllSatelliteResources: true }) })`). `loadAllSatelliteResources` is read by the .NET runtime loader but is not in its public type definitions; `LanguageTests` (E2E, switch without reload) guards it. Remove neither the `autostart` attribute nor the script.
- Supported cultures: `de-CH`, `en`. Unknown or stored garbage → `de-CH`.

## Key naming

`Area_Purpose[_Variant]`, PascalCase, e.g.

| Key | de | en |
|---|---|---|
| `App_Title` | HeldenWeg | Hero's Path |
| `Block_Up` | Hoch | Up |
| `Block_Repeat` | Wiederhole {0} Mal | Repeat {0} times |
| `Message_Obstacle_Tree` | Autsch! Da steht ein Baum im Weg. Schau dir Baustein {0} noch einmal an. | Ouch! There's a tree in the way. Take another look at block {0}. |
| `Message_OutOfBounds` | Hoppla! Hier ist die Welt zu Ende. Baustein {0} führt aus dem Spielfeld. | Oops! The world ends here. Block {0} leads off the board. |
| `Message_EmptyRepeat` | Leg zuerst Bausteine in den Wiederhole-Block. | Put some blocks into the repeat block first. |
| `Message_Succeeded` | Geschafft, {0}! Du hast die Burg erreicht. | You did it, {0}! You reached the castle. |
| `Level_free-field` | Freies Feld | Open Field |

- **One key per full sentence and variant** (`Message_Obstacle_Tree`, `_Rock`, `_Pond`). Never build sentences by concatenating fragments: grammar differs between languages.
- Placeholders `{0}`, `{1}`: document in the resx `<comment>` what they contain (e.g. `{0} = step number`).
- Plural: separate keys (`Message_ItemsMissing_One`, `Message_ItemsMissing_Other`).
- Every `GameMessageCode` has exactly one text mapping in `MessageBox`, tested with bUnit (`MessageBoxTests`, all codes in DE and EN). The session returns codes and numbers (`GameMessage`), `MessageBox` adds the hero name.

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
- `LanguageService(IAppStorage, IDocumentLanguage)`: after the culture is applied (`ApplyStoredAsync` at startup and every `SetAsync`) it calls `IDocumentLanguage.SetAsync(culture)` (`HeroPath.Shared`, host `BrowserDocumentLanguage` sets the `lang` attribute of the html element, no JS file). A throwing or unavailable document never breaks the switch. `index.html` starts with `lang="de-CH"`. The `LanguageSwitcher` buttons keep their own `lang` (`de`, `en`) so each name is read in its language. Tests that use `AddSharedUI()` register `Services.AddFakeDocumentLanguage()`.
- Components with texts derive from `LocalizedComponentBase` (or `SessionComponentBase` in Game.UI), which subscribes to `Changed` in `OnInitialized`, unsubscribes in `Dispose` and re-renders via `InvokeAsync(StateHasChanged)`. A page with `<PageTitle>` does the same (see `PlayPage`).
- At startup (`Program.cs`, after `Build()`, before `RunAsync()`) call `await host.Services.ApplyStoredCultureAsync()`. Default is `de-CH`.
- Tests that switch culture change process-wide state: put them in `[Collection("Culture")]` and restore the cultures afterwards (`CultureScope`, or `CultureBunitContext` for bUnit, both in `tests/HeroPath.Testing`; pass the culture explicitly: `new CultureScope("de-CH")`).
- Only the language and the child's custom levels are stored, never the hero name. The name of a custom level is user text: not in resx, not translated, rendered only as text (`LevelDisplayExtensions.NameOf`).

## Checklist for a text change

1. Key in the owning `<X>Resources.resx` **and** `<X>Resources.en.resx` (module texts in the module, cross-module texts in `SharedResources`).
2. German without `ß`, guillemets, «du» form.
3. Placeholders documented in `<comment>`.
4. Used via `Localizer["Key"]` / `Localizer["Key", arg]`, no fallback string in code.
5. Keys built from enum values (`Status_{RunStatus}`, `Tempo_{Tempo}`) get a completeness test per culture (`GameResourcesTests`). Use `ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)`: `GetString` for `en` silently falls back to German and hides a missing key.


## M6 S6 keys (Game)

`Shortcuts_Title` («Tasten» / «Keys», the summary), one action per full phrase `Shortcuts_Insert|Move|Delete|Deselect|Play|Debug|Reset`, key names `Shortcuts_Key_Arrows` (accessible name of the arrow group), `Shortcuts_Key_Shift|Delete|Backspace|Escape|Space` (DE «Umschalt», «Entf», «Rücktaste», «Esc», «Leertaste»). The letter keys D and R and the arrow glyphs are not translated. `GameResourcesTests` checks that all of them exist in both cultures.
