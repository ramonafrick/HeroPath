---
name: svg-assets
description: Drawing HeroPath's own SVG graphics — heroes Max and Lisa, start field (footprints), goal (castle on golden checkerboard), obstacles (tree, rock, pond), items (strawberry, pizza) — as inline-SVG Razor components in HeroPath.Shared.UI with a shared palette, colour-blind-safe shapes and patterns, localized titles, and no third-party artwork. Use when creating or changing any graphic, icon or board visual.
---

# SVG graphics

Docs: `docs/Projektidee.md` («Spielelemente»), `docs/Architektur C4.md` decision 5. **All graphics are drawn by ourselves.** No copying from icon sets, clip art, games or AI images of existing characters.

## Form

- **Inline-SVG Razor components** in `HeroPath.Shared.UI/Graphics/` (shared by the board in Levels.UI and the game in Game.UI) (e.g. `HeroMax.razor`, `ObstacleTree.razor`). No HTTP requests, styling via CSS, and the same components work in a later MAUI host.
- `viewBox="0 0 100 100"`, no fixed `width`/`height`. The cell size determines the scale.
- `role="img"` plus a localized `<title>` (resx key `Graphic_<Name>`), or `aria-hidden="true"` if purely decorative.
- Simple, round, friendly shapes: few paths, no gradients needed, line width ~4–6 units, so it stays readable at 32 px.
- One component per element. Variants via parameter (e.g. `Avatar`), not via copies.

## Palette (CSS custom properties in `HeroPath.Shared.UI/wwwroot/css/theme.css`, linked as `_content/HeroPath.Shared.UI/css/theme.css`)

| Variable | Use |
|---|---|
| `--hp-start` | start field green |
| `--hp-goal` | goal gold |
| `--hp-max-shirt` / `--hp-max-pants` | yellow / blue |
| `--hp-lisa-dress` | red |
| `--hp-tree`, `--hp-rock`, `--hp-pond` | obstacles |
| `--hp-strawberry`, `--hp-pizza` | items |
| `--hp-outline` | dark outline for all figures |

SVGs use `fill="var(--hp-...)"` so the theme can change in one place (themes such as «Fussballplatz» later).

## Elements

| Element | Must be recognizable by |
|---|---|
| Max | yellow T-shirt, blue trousers, short hair |
| Lisa | red dress, longer hair |
| Start | green area **plus footprint pattern** |
| Goal | castle **on golden checkerboard pattern** |
| Tree | round crown on a trunk (shape ≠ rock) |
| Rock | angular grey mass |
| Pond | flat blue oval with wave lines |
| Strawberry | red, seeds, green leaf |
| Pizza | triangle slice with dots |

## Colour-blind safety

- Each pair that could be confused differs in **shape or pattern**, not only in colour (start/goal, tree/pond, strawberry/pizza, Max/Lisa).
- Dark outline (`--hp-outline`) around all figures and items for contrast on every background.
- Check: does the board still work in greyscale? (DevTools → Rendering → Emulate vision deficiencies.)

## Patterns for start and goal

- Define them as `<pattern>` inside the respective component, with a **unique id per instance** (e.g. `id="start-pattern-@_id"`). Several SVGs on the page with the same id break the pattern reference.
- Alternatively, use a CSS background on the cell (a small inline SVG as a CSS data URL in the isolated CSS). That is also fine and avoids id collisions.

## Animation

- Movement comes from the board (see the `blazor-ui` skill), not from the SVG.
- Small idle animations (e.g. a flag waving on the castle) only via CSS, switched off with `prefers-reduced-motion`.
