---
name: release
description: Building, publishing and deploying HeroPath — GitHub Actions CI (build, format, unit + architecture + Playwright E2E tests, published-site tests, screenshot artifacts), deploy/finish-site.ps1 (base href, Content-Security-Policy, installable PWA with service worker), dotnet publish of the Blazor WebAssembly app, GitHub Pages deployment (base href, .nojekyll, 404.html), release ZIP for offline office laptops with a local static server, and the pre-event checklist. Use when setting up CI/CD, preparing a release, or making the app run on the Zukunftstag laptops.
---

# Release and deployment

Docs: `docs/Architektur C4.md` §7, `docs/Projekt_Kontext.md` §6. **Pushing, tagging, creating releases or enabling Pages only after explicit confirmation from the user** (these are visible to others).

## CI — `.github/workflows/ci.yml`

On `push` and `pull_request` to `main`, `permissions: contents: read`. Every action in every workflow is pinned to a **full commit SHA with a `# vN` comment** (`uses: actions/checkout@<sha> # v7`). Resolve a SHA with `git ls-remote https://github.com/actions/<name>.git "refs/tags/<vN>" "refs/tags/<vN>^{}"` (for an annotated tag take the `^{}` line, the dereferenced commit). Pinned on 2026-11-04: `checkout` v7, `setup-dotnet` v6 (`global-json-file: global.json`), `upload-artifact` v7, `download-artifact` v8 (current major), `upload-pages-artifact` v5, `deploy-pages` v5. `.github/dependabot.yml` (`github-actions`, weekly) proposes updates; keep the `# vN` comment when you take one. Every `actions/checkout` has `persist-credentials: false`.

Job `build` on `ubuntu-latest` (also finds case-sensitivity problems):

1. `dotnet restore HeroPath.slnx`
2. `dotnet format HeroPath.slnx --verify-no-changes --no-restore`
3. `dotnet build HeroPath.slnx -c Release --no-restore`
4. `dotnet test --solution HeroPath.slnx -c Release --no-build` (E2E skipped)

Job `e2e` on `windows-latest`, after `build`. Windows on purpose: the app uses the `system-ui` font stack, and on Linux the wider fallback font made `AssertEditorColumnsFitAsync` fail at 1366x768 while Windows, the target platform, passes. Do not move E2E back to Linux.

1. `dotnet build HeroPath.slnx -c Release`
2. E2E: `dotnet test --project tests/HeroPath.E2E -c Release --no-build` with `HEROPATH_E2E: "1"`. The fixture installs Chromium. The output also goes to `e2e.log`, and on failure a follow-up step publishes the failed tests as one error annotation (job logs need a login, annotations do not; `%`, `\r` and `\n` are escaped)
3. `actions/upload-artifact` with `if: always()`, name `screenshots`, path `artifacts/screenshots`, `if-no-files-found: warn`

`AppFixture` starts the web host with `dotnet run -c <Debug|Release>` matching the test build (`#if DEBUG`), so under `-c Release` the already built Release host is reused instead of a Debug build inside the 3-minute startup timeout. When you change a workflow, keep the SHA pins.

Job `published` on `windows-latest`, after `e2e`: builds the E2E project, publishes, runs `deploy/finish-site.ps1 -Root artifacts/publish/wwwroot -BaseHref / -Pwa`, then in **one** step starts `deploy/Start.ps1` in the background on `http://localhost:5301/` (`Start-Process`, output to files, polled until it answers or exits; the server is stopped in `finally`, and it shares the step with the tests because a process of an earlier step is not guaranteed to survive) and runs `dotnet test --project tests/HeroPath.E2E -c Release --no-build --filter-class HeroPath.E2E.PublishedSiteTests` with `HEROPATH_E2E=1`, `HEROPATH_BASE_URL` and `HEROPATH_PUBLISHED_URL` set to that URL. Failures use the same annotation pattern (`published.log`), screenshots go to the artifact `screenshots-published`. The normal suite skips these tests because `HEROPATH_PUBLISHED_URL` is not set there.

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
2. `shell: pwsh` step: `./deploy/finish-site.ps1 -Root artifacts/publish/wwwroot -BaseHref /HeroPath/ -Pwa -Pages` (base href, compressed files, CSP, PWA flag and manifest hash, `404.html`, `.nojekyll`, see below), then a second check that the `index.html` hash matches `service-worker-assets.js`.
3. `upload-pages-artifact@v5` drops dotfiles by default, so `include-hidden-files: true` is needed for `.nojekyll`.
4. Check that `js/file-download.js` exists and that HTML, CSS and `js/*.js` have no external URLs. The host's own `js/*.js` is checked too. Runtime JS in `_framework` contains URLs in metadata and is not checked.
5. `actions/upload-pages-artifact@v5`, then `actions/deploy-pages@v5` (job permissions `pages: write`, `id-token: write`).

Locally, `<base href="/" />` stays. Rewrite it only with `finish-site.ps1`.

## Finishing the site — `deploy/finish-site.ps1` (M6 S9)

`./deploy/finish-site.ps1 -Root <publish wwwroot> [-BaseHref /HeroPath/] [-Pwa] [-Pages]`, run **once** on a fresh publish, by `pages.yml` (`-BaseHref /HeroPath/ -Pwa -Pages`), `build-zip.ps1` (`-BaseHref /`, on the staged copy) and the `published` CI job (`-BaseHref / -Pwa`). It runs in Windows PowerShell 5.1 and pwsh, on Windows and Linux, is ASCII only, reads and writes file bytes (UTF-8, line endings untouched) and throws on anything unexpected. Order:

1. verify exactly one `<base href="/" />` and rewrite it to `-BaseHref` (must start and end with `/`).
2. delete all `*.br` and `*.gz` (they would keep the old `index.html`).
3. insert `<meta http-equiv="Content-Security-Policy" ...>` directly after `<meta charset>`. The script hash is the SHA-256 (base64) of the text inside `<script type="importmap">` (CRLF and CR normalised to LF first, as the browser does before it hashes inline script text). It fails when the import map is missing, when there is another inline script, or when a policy already exists. Policy: `default-src 'self'; script-src 'self' 'wasm-unsafe-eval' 'sha256-...'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; worker-src 'self'; manifest-src 'self'; font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'none'`. Scripts get no `'unsafe-inline'`. `style-src 'unsafe-inline'` is needed for the inline `style` attributes of Blazor components. The `<link rel="preload" as="script">` of the SDK is covered by `script-src 'self'`, and `blazor-start.js` and `pwa.js` are separate files for the same reason (no inline script, no second hash). `PublishedSiteTests` listens for `securitypolicyviolation` across a level run, the editor, an export, a language switch and sound; nothing is reported. A CSP in a meta tag cannot carry `frame-ancestors`, and it does not apply to the service worker script itself (a response header would).
4. `-Pwa`: add `<meta name="heropath-pwa" content="on" />` after the CSP (`js/pwa.js` registers the service worker only with it), check that `service-worker.js` is the published one (contains `assetsManifest`), and write the SHA-256 of the final `index.html` bytes into the `index.html` entry of `service-worker-assets.js` (the SDK computed it before the script changed the file). It refuses when the file or the entry is missing. Integrity checking stays on: the service worker fetches every asset with `integrity`.
5. `-Pages`: `404.html` (copy of the final `index.html`, deep links) and an empty `.nojekyll`.

Never change `index.html` after the publish by `sed` or by hand: the CSP hash and the manifest hash depend on its bytes. `pages.yml` adds a second check (manifest hash equals the file, meta tags, no compressed files left).

## Installable app (PWA, FR22 to FR24)

- Files in `Hosts/HeroPath.Web`: `wwwroot/manifest.webmanifest` (name «HeldenWeg», `lang de-CH`, `start_url` and `scope` `./`, `standalone`, colours from `theme.css`), `wwwroot/icon-192.png`, `icon-512.png` (rendered once from `favicon.svg` with Playwright, own art), `wwwroot/js/pwa.js` (registration), `wwwroot/service-worker.js` (development no-op) and `wwwroot/service-worker.published.js` (own code, not the framework template), wired in `HeroPath.Web.csproj` by `<ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>` and `<ServiceWorker Include="wwwroot\service-worker.js" PublishedContent="wwwroot\service-worker.published.js" />`. The SDK writes the published worker as `service-worker.js` with a `/* Manifest version */` header, so every new build changes it and browsers see an update.
- The published worker: install fetches every file of `service-worker-assets.js` (except `service-worker*`, `404.html`, `.nojekyll`) with its hash into the cache `heropath-cache-<version>`; one failure fails the install. Fetch is cache first, navigations get the cached `index.html` (so `/HeroPath/editor` works offline), activate deletes older caches. No `skipWaiting` and no `clients.claim`: a new version takes over only after every tab and window is closed. All URLs are relative to the scope, so it works under `/HeroPath/`.
- Registration is opt-in by the meta tag. ZIP, `dotnet watch` and every build without `-Pwa` never register (FR24). A failed registration is swallowed.
- Manifest and icons: after changing the icon source re-render both PNGs (throwaway Playwright test, then delete it).
- Manual only: the real installation (Edge, Chrome), the icon in the start menu, the own window, offline start after closing and reopening, and corporate policy (`docs/Probelauf.md`, «Weg 3»).

### PublishedSiteTests (opt-in)

`tests/HeroPath.E2E/PublishedSiteTests.cs`, skipped unless `HEROPATH_E2E=1` and `HEROPATH_PUBLISHED_URL` are set. Locally:

```powershell
dotnet publish src/Hosts/HeroPath.Web -c Release -o artifacts/site
./deploy/finish-site.ps1 -Root artifacts/site/wwwroot -BaseHref / -Pwa
Copy-Item deploy/Start.ps1 artifacts/site/Start.ps1
Start-Process powershell -ArgumentList '-NoProfile','-File','artifacts/site/Start.ps1','-NoBrowser','-Port','5301'
$env:HEROPATH_E2E='1'; $env:HEROPATH_BASE_URL='http://localhost:5301'; $env:HEROPATH_PUBLISHED_URL='http://localhost:5301/'
dotnet test --project tests/HeroPath.E2E --filter-class HeroPath.E2E.PublishedSiteTests
```

`HEROPATH_BASE_URL` keeps the fixture from starting the dev host. The URL must be `http://localhost:<port>/` (a service worker needs a secure context, `127.0.0.1` is refused by `Start.ps1`). Tests: no CSP violation across level, language, sound, editor and export; an injected inline script is blocked (`eval` cannot be tested, code evaluated through Playwright bypasses the CSP); manifest and icons load; the worker reaches `activated`, the cache holds every manifest file, and after `SetOfflineAsync(true)` the start page, the deep link `/editor` and a level run work. The complete normal E2E suite also passes against a CSP build (`HEROPATH_BASE_URL` on a site finished with `-BaseHref /`), which proves the policy breaks nothing.

## Office laptops (offline)

`index.html` by double-click (`file://`) **does not work**. Options in order of preference:

1. **GitHub Pages URL**, if the corporate network allows it.
2. **Release ZIP** (`.github/workflows/package.yml`, on `workflow_dispatch` and tags `v*`, two jobs). Job `build` (`windows-latest`, `contents: read`): `dotnet publish`, `deploy/build-zip.ps1`, `deploy/smoke-test.ps1` on the unpacked ZIP, `upload-artifact` (`HeroPath-zip`, `HeroPath.zip`). Job `release` (`ubuntu-latest`, only on `v*` tags, `needs: build`, `contents: write` plus `actions: read`, no checkout, `gh ... --repo` only): first a gate through `gh api` (the tag commit must be an ancestor of or equal to `main`: `compare/main...<sha>` status `behind` or `identical`, and a successful `ci.yml` push run on `main` for that SHA, otherwise it fails with an error), then `download-artifact`, SHA-256 of the ZIP into the job summary and the release notes, `gh release create --generate-notes --notes` (or `gh release upload --clobber` plus a notes edit that replaces only the old SHA-256 line if the release exists). Tag, repository and SHA come in through `env`, never inlined into the script. Contents of the ZIP: `HeroPath/Start.bat`, `Start.ps1`, `LIESMICH.txt`, `wwwroot/` (publish output without `*.br`/`*.gz`, base href stays `/`, CSP on, no service worker registration). About 3.3 MB zipped, 8.7 MB unpacked. No EXE (decision E1, option A): a PowerShell script is readable by the IT and needs nothing installed.
3. Before the event: test on **one** target laptop with the corporate browser (checklist in `docs/Probelauf.md`).

`deploy/` files:

- `Start.ps1`: `HttpListener` on `http://localhost:<port>/`, first free port from 5080 (5080 to 5099), parameters `-Port` and `-NoBrowser`. Serial request loop with try/catch per request (a client abort or a failed accept must not stop the server; the loop ends only when the listener stops). Refuses with `ConstrainedLanguage` (exit 2), missing `wwwroot` (3) and no free port (4). GET and HEAD only (405 otherwise), `Cache-Control: no-cache`, MIME table incl. `.wasm`, `.dat`, `.blat`, `.json`, `.webmanifest`. Path: raw URL, strip the query, unescape, reject NUL and `:`, `GetFullPath` under `wwwroot\`, must start with the root plus separator (ordinal, ignore case), else 403. A request is also refused with 403 unless it comes from a loopback address **and** its `Host` is `localhost` (DNS rebinding). An existing file is served, an extension-less path (`/editor`, `/play`, a directory) gets `index.html`, a path with an unknown file extension gets 404. No directory listing. Ends with Ctrl+C or by closing the window (`finally` stops the listener).
- `Start.bat`: calls `%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start.ps1"`; it prints the German hint with the Pages URL and pauses only for the script's own start failures (exit 2, 3, 4), a missing `powershell.exe` or 9009. Any other end (closing, Ctrl+C) shows nothing. The exit code of Ctrl+C was not verified.
- `LIESMICH.txt`: German mentor guide (unpack, start, end, blocked file, Pages URL).
- `build-zip.ps1 -PublishDir -Output`: stages the folder, copies `wwwroot` with `robocopy /XF *.br *.gz` (exit codes below 8 are success, then `$global:LASTEXITCODE = 0`, because the GitHub pwsh wrapper ends with `exit $LASTEXITCODE`; both paths are resolved against the PowerShell location), runs `finish-site.ps1 -BaseHref /` on the staged copy (CSP on, no PWA), normalises the three text files and zips with `System.IO.Compression`.
- `finish-site.ps1`: see «Finishing the site».
- `smoke-test.ps1 -Folder <unpacked HeroPath folder>`: refuses to run if the port is already in use, starts `Start.ps1` with Windows PowerShell 5.1 on a free port (stdout and stderr go to temp files, printed on failure; it also checks the server process is alive), raw `TcpClient` requests (HttpClient would normalise `..`): `/` html with the CSP meta and without `heropath-pwa`, the manifest MIME type, a `_framework/*.wasm` with `application/wasm` and full length, HEAD, `/editor` and `/play` fallback, unknown `.wasm` 404, POST 405, traversal variants (`..%2f`, `%2e%2e`, `..\`, `%252e`, `C:`, `:x`) must not return `Start.ps1` (marker `HEROPATH-START-SCRIPT`), the server survives an aborted download, a request over a non-loopback address is not served, the server is stopped at the end (also on failure). Exit code 1 on failure.

Pitfalls (Windows PowerShell 5.1, the shell on the laptops):

- A script **without BOM is read as ANSI**, so `Start.ps1`, `Start.bat`, `build-zip.ps1`, `finish-site.ps1` and `smoke-test.ps1` stay **ASCII only** (German console texts without umlauts: «laeuft», «oeffnen»). `LIESMICH.txt` is text for Notepad and needs umlauts: it is **UTF-8 with BOM and CRLF in the ZIP**. In the repository it is UTF-8 without BOM and LF (`.gitattributes` forces `eol=lf`), and `build-zip.ps1` converts it (BOM, CRLF). `Start.bat` has `eol=crlf` in `.gitattributes`, and the build converts again.
- `HttpListener` is not available under `ConstrainedLanguage` (AppLocker, App Control for Business): `Start.ps1` checks `$ExecutionContext.SessionState.LanguageMode` first and exits with a clear message. Then only the Pages route is left.
- `http://localhost:<port>/` needs no admin rights, but http.sys opens the port on **all** interfaces and only filters on the `Host` header, so `netstat` shows `0.0.0.0:<port>`. `Start.ps1` therefore answers only requests from a loopback `RemoteEndPoint` (403 otherwise). A request with `Host: 127.0.0.1` gets 400 from http.sys: always use `localhost` (also for `HEROPATH_BASE_URL`).
- `Compress-Archive` of PS 5.1 can write backslashes into entry names. `build-zip.ps1` uses `ZipFile.CreateFromDirectory` (.NET 4.6.1 and later write forward slashes).
- The ZIP downloaded from GitHub carries the mark of the web, so Windows may show SmartScreen for `Start.bat` (documented in `LIESMICH.txt`). `-ExecutionPolicy Bypass` covers the `.ps1`.
- Not proven for the target laptops: a strict policy blocks A and B alike (plan E1), only the Pages URL remains.

## Release checklist

- [ ] CI green on `main`
- [ ] `dotnet list HeroPath.slnx package --vulnerable` clean
- [ ] Clicked through manually in the browser: all standard levels, Play/Debug/Reset, DE/EN switch, drag & drop and click alternative
- [ ] No external URLs in the publish output
- [ ] Tag `vX.Y.Z` and GitHub release with ZIP (**only after confirmation**)
- [ ] IT/Security approval for the laptops cleared (open point in `Projekt_Kontext.md` §8)
