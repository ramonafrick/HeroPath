# HEROPATH-START-SCRIPT
# Small local web server for the HeroPath release ZIP. ASCII only on purpose:
# Windows PowerShell 5.1 reads a script without BOM as ANSI, not as UTF-8.
param(
    [int]$Port = 5080,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'

if ($ExecutionContext.SessionState.LanguageMode -ne 'FullLanguage') {
    Write-Host ''
    Write-Host 'HeroPath kann auf diesem Computer nicht gestartet werden.'
    Write-Host 'Eine Sicherheitsrichtlinie erlaubt dem Startskript keinen lokalen Server.'
    Write-Host ('(PowerShell-Modus: ' + $ExecutionContext.SessionState.LanguageMode + ')')
    exit 2
}

$root = Join-Path $PSScriptRoot 'wwwroot'
if (-not (Test-Path -LiteralPath (Join-Path $root 'index.html'))) {
    Write-Host ''
    Write-Host 'Der Ordner "wwwroot" fehlt oder ist unvollstaendig.'
    Write-Host 'Bitte das ZIP erneut und vollstaendig entpacken.'
    exit 3
}
$root = [System.IO.Path]::GetFullPath($root).TrimEnd('\') + '\'

$mime = @{
    '.html' = 'text/html; charset=utf-8'
    '.js'   = 'text/javascript; charset=utf-8'
    '.css'  = 'text/css; charset=utf-8'
    '.svg'  = 'image/svg+xml'
    '.json' = 'application/json; charset=utf-8'
    '.webmanifest' = 'application/manifest+json'
    '.wasm' = 'application/wasm'
    '.dat'  = 'application/octet-stream'
    '.blat' = 'application/octet-stream'
    '.ico'  = 'image/x-icon'
    '.png'  = 'image/png'
    '.txt'  = 'text/plain; charset=utf-8'
}

$listener = $null
$boundPort = 0
for ($candidate = $Port; $candidate -lt ($Port + 20); $candidate++) {
    $attempt = New-Object System.Net.HttpListener
    $attempt.Prefixes.Add("http://localhost:$candidate/")
    try {
        $attempt.Start()
        $listener = $attempt
        $boundPort = $candidate
        break
    } catch {
        $attempt.Close()
    }
}
if ($null -eq $listener) {
    Write-Host ''
    Write-Host ('Kein freier Anschluss gefunden (' + $Port + ' bis ' + ($Port + 19) + ').')
    Write-Host 'Bitte andere Programme schliessen und nochmals versuchen.'
    exit 4
}

function Send-Text($response, [int]$status, [string]$text) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $response.StatusCode = $status
    $response.ContentType = 'text/plain; charset=utf-8'
    $response.ContentLength64 = $bytes.Length
    $response.OutputStream.Write($bytes, 0, $bytes.Length)
}

function Resolve-Target([string]$rawUrl) {
    $path = $rawUrl
    $cut = $path.IndexOfAny([char[]]@('?', '#'))
    if ($cut -ge 0) { $path = $path.Substring(0, $cut) }
    $path = [System.Uri]::UnescapeDataString($path)
    if ($path.IndexOf([char]0) -ge 0 -or $path.IndexOf(':') -ge 0) { return @{ Status = 404 } }
    $relative = $path.TrimStart('/', '\').Replace('/', '\')
    try {
        $full = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($root, $relative))
    } catch {
        return @{ Status = 404 }
    }
    if (-not $full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return @{ Status = 403 }
    }
    if ([System.IO.File]::Exists($full)) { return @{ Status = 200; File = $full } }
    if ([System.IO.Path]::GetExtension($full) -ne '') { return @{ Status = 404 } }
    return @{ Status = 200; File = (Join-Path $root 'index.html') }
}

$url = "http://localhost:$boundPort/"
Write-Host ''
Write-Host "HeroPath laeuft unter $url"
Write-Host 'Zum Beenden dieses Fenster schliessen oder Strg+C druecken.'
Write-Host ''

if (-not $NoBrowser) {
    try { Start-Process $url } catch { Write-Host "Bitte den Browser oeffnen und $url eingeben." }
}

try {
    while ($listener.IsListening) {
        $pending = $listener.GetContextAsync()
        while (-not $pending.Wait(250)) { }
        if ($pending.IsFaulted -or $pending.IsCanceled) {
            Start-Sleep -Milliseconds 50
            continue
        }
        $context = $pending.Result
        try {
            $request = $context.Request
            $response = $context.Response
            $response.Headers.Add('Cache-Control', 'no-cache')
            $response.Headers.Add('X-Content-Type-Options', 'nosniff')
            $remote = $request.RemoteEndPoint
            if ($null -eq $remote -or -not [System.Net.IPAddress]::IsLoopback($remote.Address) -or $null -eq $request.Url -or $request.Url.Host -ne 'localhost') {
                Send-Text $response 403 'Forbidden'
            } elseif ($request.HttpMethod -ne 'GET' -and $request.HttpMethod -ne 'HEAD') {
                $response.Headers.Add('Allow', 'GET, HEAD')
                Send-Text $response 405 'Method Not Allowed'
            } else {
                $target = Resolve-Target $request.RawUrl
                if ($target.Status -ne 200) {
                    Send-Text $response $target.Status 'Not Found'
                } else {
                    $bytes = [System.IO.File]::ReadAllBytes($target.File)
                    $type = $mime[[System.IO.Path]::GetExtension($target.File).ToLowerInvariant()]
                    if (-not $type) { $type = 'application/octet-stream' }
                    $response.StatusCode = 200
                    $response.ContentType = $type
                    $response.ContentLength64 = $bytes.Length
                    if ($request.HttpMethod -eq 'GET') {
                        $response.OutputStream.Write($bytes, 0, $bytes.Length)
                    }
                }
            }
            $response.Close()
        } catch {
            try { $context.Response.Abort() } catch { }
        }
    }
} finally {
    $listener.Stop()
    $listener.Close()
    Write-Host 'HeroPath wurde beendet.'
}
