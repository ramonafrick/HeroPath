---
name: orchestrate
description: >
  Full development workflow for a feature, run end-to-end in 9 gated phases:
  Clarify (grill-me) → Plan (vertical slices) → Implement (per slice,
  code+tests) → Build (loop to green) → Verify (Playwright E2E + visual
  check of screenshots) → Review (quality+security, severity-picked fixes)
  → Document → Update relevant project guidance → Audit changed guidance
  for size/SRP. Human approval gates after Clarify and after Plan. Invoke
  with /orchestrate [feature description].
disable-model-invocation: true
---

# Orchestrator: Full Development Workflow

Task/feature: $ARGUMENTS

Run the 9 phases in order. Delegated phases may isolate context. Use only agents that are available, and do not claim model or tool capabilities that do not exist in the current environment. Phases that are not delegated (1, 8, 9, all HITL gates) run in the current session model.

| Phase | Name | Model | How |
|---|---|---|---|
| 1 | Clarify | session model | `grill-me` skill, inline (needs AskUserQuestion) |
| — | **HITL gate** | — | AskUserQuestion: approve requirements.md? |
| 2 | Plan | session model or an available Plan agent | modules and vertical slices |
| — | **HITL gate** | — | AskUserQuestion: approve plan.md, which slice first? |
| 3+4 | Implement + Build (per slice, loop) | Sonnet / Haiku | agent `implementer` → agent `build-fixer` |
| 5 | Verify (E2E + visual) | Sonnet | agent `visual-checker` (only if visible UI changed) |
| 6 | Review | Sonnet | agents `code-reviewer` + `security-reviewer` in parallel |
| 7 | Documentation | Sonnet | agent `docs-writer` |
| 8 | Long-term memory | session model | inline, updates the project's `.claude/` |
| 9 | Size/SRP audit | session model | inline |

Before starting: determine from the `architecture` skill and the repository which projects are affected (`Shared`, `Modules.Levels`, `Modules.Game`, each with `.UI`, `Hosts/HeroPath.Web`). Ask only about host or scope decisions that cannot be derived from the request. A MAUI app is not an existing host and must not silently be taken as a prerequisite.

Project skills (name them per slice in the phase 2 plan; the agents load them through their `skills:` frontmatter): `architecture` (modules, dependencies), `dotnet10` (C#/solution), `game-rules` (game rules), `blazor-ui` (components), `localization` (texts), `level-design` (levels), `svg-assets` (graphics), `e2e-playwright` (E2E/screenshots), `release` (CI/deployment).

---

## PHASE 1: Clarify

Follow the `grill-me` skill for `$ARGUMENTS` (7 areas, `AskUserQuestion`, max. 2 questions per round). Result: `docs/requirements.md`.

**STOP**: Show `docs/requirements.md` and ask explicitly via `AskUserQuestion`: approve and continue to phase 2, or changes? Do not continue without explicit approval.

---

## PHASE 2: Plan (vertical slices)

Create a plan in the current context or use an available Plan agent. Pass along `docs/requirements.md`, the project documents and the `architecture` skill.

Research before planning: for every technical choice that the requirements, code and docs do not settle, check the code, `docs/` and official sources, and record in the plan the options, a recommendation and the sources (file paths, URLs). Do not plan on assumptions; open questions go to the gate below.

A good slice cuts through the projects that are actually affected (Domain → Application → UI → host registration), is testable without the next slice and stays small enough to review. It respects the dependency rules (`Shared ← Levels ← Game`). A new module only according to the criteria in the `architecture` skill.

Result: `docs/plan.md` with a module overview and per slice: name, goal, projects/files, tests (xUnit/bUnit/**E2E flow with screenshot names** if visible), acceptance, dependencies. Plus the build and test commands from the repository.

**STOP**: Show the slice table and the open decisions with their options and sources, and ask via `AskUserQuestion`: approve, and which slice first?

---

## PHASE 3+4: Implement → Build (loop per slice)

For each slice, in the approved order:

1. **Implement**: Agent tool, `subagent_type: "implementer"`. Pass the slice definition from plan.md, the relevant section of requirements.md and the affected files. For visible changes this includes `data-testid` attributes and the E2E test for the flow.
2. **Build**: Agent tool, `subagent_type: "build-fixer"`, with the changed files. It loops internally until green or 3 attempts. This includes the ArchitectureTests (and with them `ClaudeWorkflowTests`). The E2E tests do not run here; without `HEROPATH_E2E` they stay skipped.
3. If build-fixer still reports red after 3 attempts: **STOP**, show the error and ask the user. Do not keep guessing.
4. Commit only if the user asked for it or approved it. Otherwise leave changes uncommitted.
5. Short context reset: note "Slice X done, next: Y", then continue.

Only when all slices are done: continue to phase 5.

---

## PHASE 5: Verify (E2E + visual check)

Only if a slice changed visible UI (Razor, CSS, SVG, resx). Otherwise skip and mention it in the wrap-up.

1. Agent tool, `subagent_type: "visual-checker"`. Pass the acceptance criteria and the changed components. It runs `HEROPATH_E2E=1 dotnet test --project tests/HeroPath.E2E` and reads all screenshots, normal and greyscale, each in German and English.
2. A failing E2E test is a [MUST] finding. If the cause is clearly in the code, hand it to `build-fixer` first with the error message.
3. Findings from the visual check are **not** fixed immediately; they go into the selection in phase 6.
4. If the checker reports gaps (component without screenshot), add them as [SHOULD] findings.

---

## PHASE 6: Review (quality + security + visual, max. 3 rounds)

Round 1 of at most 3:

1. Dispatch in parallel: agent `code-reviewer` (simplification, consistency, module boundaries) and agent `security-reviewer` (security/SAST, incl. privacy of minors) on all files changed in phases 3+4.
2. Merge the findings of code-reviewer, security-reviewer and visual-checker (phase 5) into one list, sorted by severity ([MUST]/🔴 first). Each finding states its source.
3. `AskUserQuestion` (multiSelect): list each finding as an option (severity, source, short description). The user picks which ones to fix now.
4. Fix the selected findings.
5. Back to **phase 4** (build-fixer) on the changed files. If visual findings were fixed: run **phase 5** again afterwards so the fix is proven on the screenshots.
6. If findings remain open after this round: continue to phase 7 and record them as known limitations.
7. More rounds are possible, but **never more than 3 in total**.

---

## PHASE 7: Documentation

Use `docs-writer` or document directly: pass the plan (if any), the implemented files and findings that were not addressed. Document only behaviour that is actually implemented. Change the README only if setup, usage or public commands are affected. If a new module was added, update the module table in `docs/Architektur C4.md`.

---

## PHASE 8: Long-term memory (project, not personal cross-session memory)

Update project guidance only if the work produced a lasting convention or a proven gotcha. This concerns existing `.claude/` files or `CLAUDE.md`, always written in English. Do not create long-term notes outside the repository.

1. Compare new, proven findings with the existing project guidance and skills.
2. Update only what actually changed or grew: new gotchas, new conventions, new modules/components/files, changed commands, new `data-testid` conventions. Skills must describe every new public type of the Levels and Game modules (`ClaudeWorkflowTests`).
3. Add nothing that can already be derived from the code or the git history.

---

## PHASE 9: Size/SRP audit

For each file under `.claude/` touched or created in phase 8:

- **SKILL.md body**: target < 500 lines. Above the limit, split into `references/*.md` and link them one level deep from SKILL.md. Reference files over 100 lines get a table of contents.
- **Project guidance**: keep it concise and focused on its topic. Split only when the size really requires it.
- SRP check: does a file contain several unrelated topics? Then split it by domain.
- Language check: the file is in English (German only for quoted product texts).

---

## Rules

- Never start a slice while the build from previous work is still red.
- Never skip review (phase 6) or tests (part of phase 3). Skip verify (phase 5) only if no visible UI changed.
- HITL gates are turn-based: after the stop the answer ends, and work continues only with a new user message. Silence or a change of topic does not count as approval.
- No assumptions: research first (code, `docs/`, official sources), cite the sources, and present options with a recommendation instead of deciding silently.
- After 2 failed fix attempts on the same problem: stop guessing and involve the user.
- Use delegation only if suitable agents are actually available and useful for the task.
- Do not treat proposed or future architecture (e.g. MAUI) as implemented project state.
