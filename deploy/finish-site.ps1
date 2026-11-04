# Finishes a published HeroPath site. Run it once on a fresh `dotnet publish` output (the wwwroot folder).
# Usage: ./deploy/finish-site.ps1 -Root artifacts/publish/wwwroot [-BaseHref /HeroPath/] [-Pwa] [-Pages]
#
# Every change to index.html after the publish goes through this script, because the Content-Security-Policy
# hash and the hash in service-worker-assets.js depend on the final bytes. Steps, in this order:
#   1. verify <base href="/" /> and rewrite it to -BaseHref
#   2. delete all *.br and *.gz (they would keep the old index.html)
#   3. insert the Content-Security-Policy meta tag directly after <meta charset>
#   4. -Pwa: add <meta name="heropath-pwa" content="on" /> and write the hash of the final index.html into
#      service-worker-assets.js (the SDK computed it before this script changed the file)
#   5. -Pages: create 404.html (copy of index.html, for deep links) and .nojekyll
# Anything unexpected throws, so the build fails instead of shipping a site that does not start.
# Runs in Windows PowerShell 5.1 and pwsh, on Windows and Linux. ASCII only (5.1 reads a file without BOM as ANSI).
param(
    [Parameter(Mandatory = $true)][string]$Root,
    [string]$BaseHref = '/',
    [switch]$Pwa,
    [switch]$Pages
)

$ErrorActionPreference = 'Stop'

$rootFull = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Root)
$indexPath = Join-Path $rootFull 'index.html'
if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    throw "No index.html in $rootFull. Run dotnet publish first and pass its wwwroot folder as -Root."
}
if ($BaseHref -notmatch '^/([A-Za-z0-9._-]+/)*$') {
    throw "-BaseHref must start and end with '/' and contain only letters, digits, '.', '_', '-' and '/': $BaseHref"
}

$utf8 = New-Object System.Text.UTF8Encoding($false, $true)

function Read-Text([string]$path) {
    return $utf8.GetString([System.IO.File]::ReadAllBytes($path))
}

function Write-Text([string]$path, [string]$text) {
    [System.IO.File]::WriteAllBytes($path, $utf8.GetBytes($text))
}

function Get-Sha256Base64([byte[]]$bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [Convert]::ToBase64String($sha.ComputeHash($bytes))
    } finally {
        $sha.Dispose()
    }
}

function Find-One([string]$text, [string]$pattern, [string]$what) {
    $found = [regex]::Matches($text, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($found.Count -ne 1) {
        throw "Expected exactly one $what in index.html, found $($found.Count)."
    }
    return $found[0]
}

# The line ending of the file, so inserted lines match it. Bytes of existing lines stay untouched.
function Get-Newline([string]$text) {
    if ($text.Contains("`r`n")) { return "`r`n" }
    return "`n"
}

$html = Read-Text $indexPath
$newline = Get-Newline $html

# 1. base href
Find-One $html '<base href="/" />' '<base href="/" />' | Out-Null
$html = $html.Replace('<base href="/" />', '<base href="' + $BaseHref + '" />')

# 2. compressed copies
$compressed = @(Get-ChildItem -LiteralPath $rootFull -Recurse -File -Force | Where-Object { $_.Extension -eq '.br' -or $_.Extension -eq '.gz' })
foreach ($file in $compressed) { Remove-Item -LiteralPath $file.FullName -Force }
Write-Host ("Deleted {0} compressed file(s)." -f $compressed.Count)

# 3. Content-Security-Policy. The hash covers the text inside <script type="importmap"> (written by the SDK at
# publish time). The browser normalises CRLF and CR to LF before it hashes inline script text, so the same is done here.
# Any other inline script would be blocked, so it is an error here, not a surprise later: every <script> must be the import
# map, or have a real src attribute (not data-src) and an empty body.
$importMap = Find-One $html '<script type="importmap">(.*?)</script>' '<script type="importmap">'
$importMapText = $importMap.Groups[1].Value
if ($importMapText.TrimStart().IndexOf('{') -ne 0 -or $importMapText.IndexOf('"imports"') -lt 0) {
    throw 'The import map in index.html is empty or not a JSON import map. Was the site published?'
}
$scriptStarts = [regex]::Matches($html, '<script\b', 'IgnoreCase').Count
$scripts = [regex]::Matches($html, '<script\b([^>]*)>(.*?)</script\s*>', 'IgnoreCase, Singleline')
if ($scripts.Count -ne $scriptStarts) {
    throw "index.html has $scriptStarts <script> start tag(s) but $($scripts.Count) complete <script> element(s)."
}
foreach ($element in $scripts) {
    $attributes = $element.Groups[1].Value
    if ($attributes -match '^\s*type\s*=\s*"importmap"\s*$') { continue }
    $hasSrc = $attributes -match '(^|\s)src\s*=\s*("[^"]+"|''[^'']+'')'
    if (-not $hasSrc -or $element.Groups[2].Value.Trim().Length -gt 0) {
        throw "Unexpected inline script in index.html: <script$attributes>"
    }
}
$importMapNormalized = $importMapText -replace "`r`n", "`n" -replace "`r", "`n"
$importMapHash = Get-Sha256Base64 ($utf8.GetBytes($importMapNormalized))

# Style elements and stylesheets only from the site itself (style-src-elem). Only style attributes may be inline
# (style-src-attr 'unsafe-inline'), because Blazor components and the game board set style="..." for cell sizes, colours and
# animation. Scripts get no 'unsafe-inline'.
$policy = "default-src 'self'; script-src 'self' 'wasm-unsafe-eval' 'sha256-$importMapHash'; style-src-elem 'self'; style-src-attr 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; worker-src 'self'; manifest-src 'self'; font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'none'"
if ($html.Contains('Content-Security-Policy')) {
    throw 'index.html already has a Content-Security-Policy. Run finish-site.ps1 once on a fresh publish.'
}
$charset = Find-One $html '[ \t]*<meta charset="[^"]*" />' '<meta charset> element'
$indent = $charset.Value.Substring(0, $charset.Value.Length - $charset.Value.TrimStart().Length)
$insert = $newline + $indent + '<meta http-equiv="Content-Security-Policy" content="' + $policy + '" />'
$insertAt = $charset.Index + $charset.Length

# 4. PWA switch
if ($Pwa) {
    $insert += $newline + $indent + '<meta name="heropath-pwa" content="on" />'
}
$html = $html.Insert($insertAt, $insert)
Write-Text $indexPath $html

if ($Pwa) {
    $workerPath = Join-Path $rootFull 'service-worker.js'
    $assetsPath = Join-Path $rootFull 'service-worker-assets.js'
    foreach ($required in @($workerPath, $assetsPath)) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "-Pwa needs $required (publish with the ServiceWorker items)." }
    }
    if (-not (Read-Text $workerPath).Contains('assetsManifest')) {
        throw 'service-worker.js is the development stub, not the published service worker.'
    }

    $assets = Read-Text $assetsPath
    $entries = [regex]::Matches($assets, '\{[^{}]*"url"\s*:\s*"index\.html"[^{}]*\}')
    if ($entries.Count -ne 1) {
        throw "service-worker-assets.js must list index.html exactly once, found $($entries.Count). Refusing to continue."
    }
    $entry = $entries[0]
    $hashField = [regex]::Match($entry.Value, '"hash"\s*:\s*"sha256-[^"]*"')
    if (-not $hashField.Success) { throw 'The index.html entry in service-worker-assets.js has no sha256 hash.' }

    # The hash is over the bytes of the file as written, no normalisation (the service worker checks the download).
    $indexHash = Get-Sha256Base64 ([System.IO.File]::ReadAllBytes($indexPath))
    $newEntry = $entry.Value.Substring(0, $hashField.Index) + '"hash": "sha256-' + $indexHash + '"' + $entry.Value.Substring($hashField.Index + $hashField.Length)
    $assets = $assets.Substring(0, $entry.Index) + $newEntry + $assets.Substring($entry.Index + $entry.Length)
    Write-Text $assetsPath $assets
    Write-Host 'PWA enabled, index.html hash updated in service-worker-assets.js.'
}

# 5. GitHub Pages
if ($Pages) {
    [System.IO.File]::WriteAllBytes((Join-Path $rootFull '404.html'), [System.IO.File]::ReadAllBytes($indexPath))
    [System.IO.File]::WriteAllBytes((Join-Path $rootFull '.nojekyll'), (New-Object byte[] 0))
    Write-Host 'Created 404.html and .nojekyll.'
}

Write-Host ("Site finished: base {0}, CSP on, PWA {1}, Pages {2}." -f $BaseHref, $(if ($Pwa) { 'on' } else { 'off' }), $(if ($Pages) { 'on' } else { 'off' }))
