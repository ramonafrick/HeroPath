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
2. **Release ZIP** (`.github/workflows/package.yml`, on `workflow_dispatch` and tags `v*`, `windows-latest`, `contents: write`): `dotnet publish`, `deploy/build-zip.ps1`, `deploy/smoke-test.ps1` on the unpacked ZIP, `upload-artifact` (`HeroPath-zip`), and on a tag `gh release create` (or `gh release upload --clobber` if it exists; the tag comes in through `env: TAG`, never inlined into the script). Contents: `HeroPath/Start.bat`, `Start.ps1`, `LIESMICH.txt`, `wwwroot/` (publish output without `*.br`/`*.gz`, base href stays `/`). About 3.3 MB zipped, 8.7 MB unpacked. No EXE (decision E1, option A): a PowerShell script is readable by the IT and needs nothing installed.
3. Before the event: test on **one** target laptop with the corporate browser (checklist in `docs/Probelauf.md`).

`deploy/` files:

- `Start.ps1`: `HttpListener` on `http://localhost:<port>/`, first free port from 5080 (5080 to 5099), parameters `-Port` and `-NoBrowser`. Serial request loop with try/catch per request (a client abort must not stop the server). Refuses with `ConstrainedLanguage` (exit 2), missing `wwwroot` (3) and no free port (4). GET and HEAD only (405 otherwise), `Cache-Control: no-cache`, MIME table incl. `.wasm`, `.dat`, `.blat`, `.json`. Path: raw URL, strip the query, unescape, reject NUL and `:`, `GetFullPath` under `wwwroot\`, must start with the root plus separator (ordinal, ignore case), else 403. An existing file is served, an extension-less path (`/editor`, `/play`, a directory) gets `index.html`, a path with an unknown file extension gets 404. No directory listing. Ends with Ctrl+C or by closing the window (`finally` stops the listener).
- `Start.bat`: calls `powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start.ps1"`; on exit code 1 or more it prints a German hint with the Pages URL and pauses.
- `LIESMICH.txt`: German mentor guide (unpack, start, end, blocked file, Pages URL).
- `build-zip.ps1 -PublishDir -Output`: stages the folder, copies `wwwroot` with `robocopy /XF *.br *.gz`, normalises the three text files and zips with `System.IO.Compression`.
- `smoke-test.ps1 -Folder <unpacked HeroPath folder>`: starts `Start.ps1` with Windows PowerShell 5.1 on a free port, raw `TcpClient` requests (HttpClient would normalise `..`): `/` html, a `_framework/*.wasm` with `application/wasm` and full length, HEAD, `/editor` and `/play` fallback, unknown `.wasm` 404, POST 405, traversal variants (`..%2f`, `%2e%2e`, `..\`, `%252e`, `C:`, `:x`) must not return `Start.ps1` (marker `HEROPATH-START-SCRIPT`), the server survives an aborted download, a request over a non-loopback address is not served, the server is stopped at the end (also on failure). Exit code 1 on failure.

Pitfalls (Windows PowerShell 5.1, the shell on the laptops):

- A script **without BOM is read as ANSI**, so `Start.ps1`, `Start.bat`, `build-zip.ps1` and `smoke-test.ps1` stay **ASCII only** (German console texts without umlauts: «laeuft», «oeffnen»). `LIESMICH.txt` is text for Notepad and needs umlauts: it is **UTF-8 with BOM and CRLF in the ZIP**. In the repository it is UTF-8 without BOM and LF (`.gitattributes` forces `eol=lf`), and `build-zip.ps1` converts it (BOM, CRLF). `Start.bat` has `eol=crlf` in `.gitattributes`, and the build converts again.
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
