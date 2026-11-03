---
name: release
description: Building, publishing and deploying HeroPath — GitHub Actions CI (build, format, unit + architecture + Playwright E2E tests, screenshot artifacts), dotnet publish of the Blazor WebAssembly app, GitHub Pages deployment (base href, .nojekyll, 404.html), release ZIP for offline office laptops with a local static server, and the pre-event checklist. Use when setting up CI/CD, preparing a release, or making the app run on the Zukunftstag laptops.
---

# Release and deployment

Docs: `docs/Architektur C4.md` §7, `docs/Projekt_Kontext.md` §6. **Pushing, tagging, creating releases or enabling Pages only after explicit confirmation from the user** (these are visible to others).

## CI — `.github/workflows/ci.yml`

On `push` and `pull_request` to `main`:

```yaml
- uses: actions/checkout@v4
- uses: actions/setup-dotnet@v4
  with: { global-json-file: global.json }
- run: dotnet restore HeroPath.slnx
- run: dotnet format HeroPath.slnx --verify-no-changes --no-restore
- run: dotnet build HeroPath.slnx -c Release --no-restore
- run: dotnet test --solution HeroPath.slnx -c Release --no-build
- name: E2E
  run: dotnet test --project tests/HeroPath.E2E -c Release --no-build
  env: { HEROPATH_E2E: "1" }
- uses: actions/upload-artifact@v4
  if: always()
  with: { name: screenshots, path: artifacts/screenshots }
```

The E2E step installs Chromium itself (via the fixture). On Linux runners Playwright may also need system libraries: if the browser fails to start, add `pwsh tests/HeroPath.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps chromium` before the step (`pwsh` is preinstalled on GitHub runners).

Runner `ubuntu-latest` is enough. Check the action versions at the time you create the workflow.

## Publish

```bash
dotnet publish src/Hosts/HeroPath.Web -c Release -o artifacts/publish
# static site: artifacts/publish/wwwroot
```

- The host has one own JS module, `wwwroot/js/file-download.js` (export; it revokes the object URL one second after the click, so the download can still start in slow browsers). `BrowserFileDownload` imports it with the relative path `./js/file-download.js`, which Blazor resolves against the base href, so it works under `/HeroPath/` on GitHub Pages. Check that `artifacts/publish/wwwroot/js/file-download.js` exists after publishing (`wwwroot` of the host is published by default).
- Never commit the publish output (`artifacts/` is in `.gitignore`; add it if missing).
- After publishing, check that `wwwroot` has **no external URLs** (`grep -r "https://" artifacts/publish/wwwroot --include=*.html --include=*.css`). Offline readiness is a quality goal.

## GitHub Pages — `.github/workflows/pages.yml`

Repo `ramonafrick/HeroPath` → the app runs under `/HeroPath/`.

1. Publish as above.
2. `sed -i 's|<base href="/" />|<base href="/HeroPath/" />|' wwwroot/index.html`
3. `cp wwwroot/index.html wwwroot/404.html` (deep links)
4. `touch wwwroot/.nojekyll` (otherwise `_framework`/`_content` are ignored)
5. `actions/upload-pages-artifact` with `path: artifacts/publish/wwwroot`, then `actions/deploy-pages` (permissions `pages: write`, `id-token: write`).

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
