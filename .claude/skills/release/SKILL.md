---
name: release
description: Building, publishing and deploying HeroPath — GitHub Actions CI (build, format, unit + architecture + Playwright E2E tests, screenshot artifacts), dotnet publish of the Blazor WebAssembly app, GitHub Pages deployment (base href, .nojekyll, 404.html), release ZIP for offline office laptops with a local static server, and the pre-event checklist. Use when setting up CI/CD, preparing a release, or making the app run on the Zukunftstag laptops.
---

# Release and deployment

Docs: `docs/Architektur C4.md` §7, `docs/Projekt_Kontext.md` §6. **Pushing, tagging, creating releases or enabling Pages only after explicit confirmation from the user** (these are visible to others).

## CI — `.github/workflows/ci.yml`

On `push` and `pull_request` to `main`, `permissions: contents: read`. Action versions (checked with `git ls-remote` on 2026-11-04): `actions/checkout@v7`, `actions/setup-dotnet@v6` (`global-json-file: global.json`), `actions/upload-artifact@v7`.

Job `build` on `ubuntu-latest` (also finds case-sensitivity problems):

1. `dotnet restore HeroPath.slnx`
2. `dotnet format HeroPath.slnx --verify-no-changes --no-restore`
3. `dotnet build HeroPath.slnx -c Release --no-restore`
4. `dotnet test --solution HeroPath.slnx -c Release --no-build` (E2E skipped)

Job `e2e` on `windows-latest`, after `build`. Windows on purpose: the app uses the `system-ui` font stack, and on Linux the wider fallback font made `AssertEditorColumnsFitAsync` fail at 1366x768 while Windows, the target platform, passes. Do not move E2E back to Linux.

1. `dotnet build HeroPath.slnx -c Release`
2. E2E: `dotnet test --project tests/HeroPath.E2E -c Release --no-build` with `HEROPATH_E2E: "1"`. The fixture installs Chromium. The output also goes to `e2e.log`, and on failure a follow-up step publishes the failed tests as one error annotation (job logs need a login, annotations do not)
3. `actions/upload-artifact` with `if: always()`, name `screenshots`, path `artifacts/screenshots`, `if-no-files-found: warn`

`AppFixture` starts the web host with `dotnet run -c <Debug|Release>` matching the test build (`#if DEBUG`), so under `-c Release` the already built Release host is reused instead of a Debug build inside the 3-minute startup timeout. Check action versions again when you change the workflow.

## Publish

```bash
dotnet publish src/Hosts/HeroPath.Web -c Release -o artifacts/publish
# static site: artifacts/publish/wwwroot
```

- The host has one own JS module, `wwwroot/js/file-download.js` (export; it revokes the object URL one second after the click, so the download can still start in slow browsers). `BrowserFileDownload` imports it with the relative path `./js/file-download.js`, which Blazor resolves against the base href, so it works under `/HeroPath/` on GitHub Pages. Check that `artifacts/publish/wwwroot/js/file-download.js` exists after publishing (`wwwroot` of the host is published by default).
- Never commit the publish output (`artifacts/` is in `.gitignore`; add it if missing).
- After publishing, check that `wwwroot` has **no external URLs** (`grep -r "https://" artifacts/publish/wwwroot --include=*.html --include=*.css`). Offline readiness is a quality goal.

## GitHub Pages — `.github/workflows/pages.yml`

Repo `ramonafrick/HeroPath` (public) → the app runs under `/HeroPath/`. Runs after a green `CI` on a push to `main` (`workflow_run`) and by hand (`workflow_dispatch`). Enabling it once is a repository setting: Settings > Pages > Source: GitHub Actions. Until then the `deploy` job fails, which is expected.

1. Publish as above.
2. Rewrite `<base href="/" />` to `/HeroPath/` in `index.html`, and delete all `*.br` and `*.gz` (Pages compresses itself, and `index.html.br/.gz` would keep the old base href).
3. `cp index.html 404.html` (deep links).
4. `touch .nojekyll`. `upload-pages-artifact@v5` drops dotfiles by default, so `include-hidden-files: true` is needed.
5. Check that `js/file-download.js` exists and that HTML and CSS have no external URLs. Runtime JS in `_framework` contains URLs in metadata and is not checked.
6. `actions/upload-pages-artifact@v5`, then `actions/deploy-pages@v5` (job permissions `pages: write`, `id-token: write`).

Locally, `<base href="/" />` stays. Rewrite it only in the workflow.

## Office laptops (offline)

`index.html` by double-click (`file://`) **does not work**. Options in order of preference:

1. **GitHub Pages URL**, if the corporate network allows it.
2. **Release ZIP** with the `wwwroot` publish folder and a small server:
   - Optional `src/HeroPath.Host` (ASP.NET Core, self-contained `win-x64` EXE) with `UseDefaultFiles()` + `UseStaticFiles()` with a `FileExtensionContentTypeProvider` that knows `.wasm` (`application/wasm`), `.dat`, `.blat` and `.json`, plus a fallback to `index.html`. `Start.bat` starts the EXE and opens `http://localhost:5000`.
   - Alternatively `python -m http.server 8080` in the `wwwroot` folder (only if Python is installed).
3. Before the event: test on **one** target laptop with the corporate browser.

## Release checklist

- [ ] CI green on `main`
- [ ] `dotnet list HeroPath.slnx package --vulnerable` clean
- [ ] Clicked through manually in the browser: all standard levels, Play/Debug/Reset, DE/EN switch, drag & drop and click alternative
- [ ] No external URLs in the publish output
- [ ] Tag `vX.Y.Z` and GitHub release with ZIP (**only after confirmation**)
- [ ] IT/Security approval for the laptops cleared (open point in `Projekt_Kontext.md` §8)
