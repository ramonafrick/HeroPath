# Smoke test for an unpacked release ZIP.
# Usage: ./deploy/smoke-test.ps1 -Folder <path to the unpacked HeroPath folder>
# Starts Start.ps1 with Windows PowerShell 5.1 (as on the laptops), checks it, stops it.
# ASCII only (Windows PowerShell 5.1 reads files without BOM as ANSI).
param(
    [Parameter(Mandatory = $true)][string]$Folder,
    [int]$Port = 0
)

$ErrorActionPreference = 'Stop'
$folder = (Resolve-Path -LiteralPath $Folder).Path
$script:failures = New-Object System.Collections.Generic.List[string]
$script:process = $null
$stdoutFile = Join-Path ([System.IO.Path]::GetTempPath()) ("heropath-smoke-" + [guid]::NewGuid().ToString("N") + ".out.txt")
$stderrFile = Join-Path ([System.IO.Path]::GetTempPath()) ("heropath-smoke-" + [guid]::NewGuid().ToString("N") + ".err.txt")

function Check([string]$name, [bool]$ok, [string]$detail = '') {
    if ($ok) {
        Write-Host "PASS  $name"
    } else {
        Write-Host "FAIL  $name $detail"
        $script:failures.Add($name)
    }
}

function Get-FreePort {
    $l = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, 0)
    $l.Start()
    $p = $l.LocalEndpoint.Port
    $l.Stop()
    return $p
}

function Test-PortInUse([int]$port) {
    $probe = New-Object System.Net.Sockets.TcpClient
    try {
        $probe.Connect("127.0.0.1", $port)
        return $true
    } catch {
        return $false
    } finally {
        $probe.Close()
    }
}

function Send-Raw([int]$port, [string]$requestLine, [string]$address = '127.0.0.1', [string]$extraHeaders = '') {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $client.Connect($address, $port)
        $stream = $client.GetStream()
        $stream.ReadTimeout = 10000
        $request = $requestLine + "`r`nHost: localhost:$port`r`n" + $extraHeaders + "Connection: close`r`n`r`n"
        $bytes = [System.Text.Encoding]::ASCII.GetBytes($request)
        $stream.Write($bytes, 0, $bytes.Length)
        $memory = New-Object System.IO.MemoryStream
        $buffer = New-Object byte[] 65536
        while (($n = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) { $memory.Write($buffer, 0, $n) }
        $text = [System.Text.Encoding]::GetEncoding(28591).GetString($memory.ToArray())
        $split = $text.IndexOf("`r`n`r`n")
        $head = if ($split -ge 0) { $text.Substring(0, $split) } else { $text }
        $body = if ($split -ge 0) { $text.Substring($split + 4) } else { '' }
        $status = 0
        if ($head -match '^HTTP/\d\.\d (\d+)') { $status = [int]$Matches[1] }
        return @{ Status = $status; Head = $head; Body = $body }
    } finally {
        $client.Close()
    }
}

function Get-Header($response, [string]$name) {
    foreach ($line in ($response.Head -split "`r`n")) {
        if ($line -match ('^' + [regex]::Escape($name) + ':\s*(.*)$')) { return $Matches[1] }
    }
    return ''
}

try {
    if ($Port -le 0) { $Port = Get-FreePort }
    if (Test-PortInUse $Port) { throw "Port $Port is already in use, refusing to test a foreign server." }
    $startScript = Join-Path $folder 'Start.ps1'
    $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $startScript + '" -NoBrowser -Port ' + $Port
    $script:process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutFile -RedirectStandardError $stderrFile

    $ready = $false
    for ($i = 0; $i -lt 100 -and -not $ready; $i++) {
        if ($script:process.HasExited) { break }
        try {
            $probe = New-Object System.Net.Sockets.TcpClient
            $probe.Connect('127.0.0.1', $Port)
            $probe.Close()
            $ready = $true
        } catch {
            Start-Sleep -Milliseconds 200
        }
    }
    Check 'server starts' $ready ('(exited: ' + $script:process.HasExited + ')')
    if (-not $ready) { throw 'Server did not start.' }

    Check 'server process is still running' (-not $script:process.HasExited) '(a foreign server may answer)'
    $r = Send-Raw $Port 'GET / HTTP/1.1'
    Check 'GET / is 200 text/html' ($r.Status -eq 200 -and (Get-Header $r 'Content-Type') -like 'text/html*') "($($r.Status) $(Get-Header $r 'Content-Type'))"
    Check 'Cache-Control is no-cache' ((Get-Header $r 'Cache-Control') -eq 'no-cache')
    Check 'index.html has the Content-Security-Policy meta' ($r.Body.Contains('http-equiv="Content-Security-Policy"'))
    Check 'index.html has no unsafe-inline in script-src' ($r.Body -match "script-src [^;]*'sha256-" -and $r.Body -notmatch "script-src [^;]*'unsafe-inline'")
    Check 'index.html has no heropath-pwa meta (no service worker in the ZIP)' (-not $r.Body.Contains('heropath-pwa'))
    $indexLength = $r.Body.Length
    $r = Send-Raw $Port 'GET /manifest.webmanifest HTTP/1.1'
    Check 'manifest is served as application/manifest+json' ($r.Status -eq 200 -and (Get-Header $r 'Content-Type') -eq 'application/manifest+json') "($($r.Status) $(Get-Header $r 'Content-Type'))"

    $wasm = Get-ChildItem -LiteralPath (Join-Path $folder 'wwwroot\_framework') -Filter '*.wasm' | Select-Object -First 1
    Check 'a _framework wasm file exists' ($null -ne $wasm)
    if ($wasm) {
        $r = Send-Raw $Port ('GET /_framework/' + $wasm.Name + ' HTTP/1.1')
        Check 'GET _framework wasm is 200 application/wasm with full length' ($r.Status -eq 200 -and (Get-Header $r 'Content-Type') -eq 'application/wasm' -and $r.Body.Length -eq $wasm.Length) "($($r.Status) $(Get-Header $r 'Content-Type') $($r.Body.Length) of $($wasm.Length))"
        $r = Send-Raw $Port ('HEAD /_framework/' + $wasm.Name + ' HTTP/1.1')
        Check 'HEAD wasm is 200 without body' ($r.Status -eq 200 -and $r.Body.Length -eq 0) "($($r.Status) $($r.Body.Length))"
    }

    $r = Send-Raw $Port 'GET /editor HTTP/1.1'
    Check 'GET /editor falls back to index.html' ($r.Status -eq 200 -and (Get-Header $r 'Content-Type') -like 'text/html*' -and $r.Body.Length -eq $indexLength)
    $r = Send-Raw $Port 'GET /play HTTP/1.1'
    Check 'GET /play falls back to index.html' ($r.Status -eq 200 -and $r.Body.Length -eq $indexLength)
    $r = Send-Raw $Port 'GET /_framework/does-not-exist.wasm HTTP/1.1'
    Check 'unknown .wasm is 404' ($r.Status -eq 404) "($($r.Status))"
    $r = Send-Raw $Port 'GET /_framework/ HTTP/1.1'
    Check 'directory is not listed' ($r.Status -eq 200 -and $r.Body.Length -eq $indexLength -or $r.Status -in 403, 404)
    $r = Send-Raw $Port 'POST / HTTP/1.1' '127.0.0.1' "Content-Length: 0`r`n"
    Check 'POST is 405' ($r.Status -eq 405) "($($r.Status))"

    foreach ($path in '/..%2f..%2fStart.ps1', '/%2e%2e/Start.ps1', '/..\Start.ps1', '/../Start.ps1', '/%2e%2e%5cStart.ps1', '/..%5c..%5cStart.ps1', '/%252e%252e/Start.ps1', '/_framework/..%2f..%2fStart.ps1', '/C:/Windows/win.ini', '/index.html:x') {
        $r = Send-Raw $Port ('GET ' + $path + ' HTTP/1.1')
        $leaked = $r.Body.Contains('HEROPATH-START-SCRIPT')
        Check "traversal $path is refused" (-not $leaked -and $r.Status -ne 200) "($($r.Status), leaked: $leaked)"
    }

    $after = Send-Raw $Port 'GET / HTTP/1.1'
    Check 'server still answers after the bad requests' ($after.Status -eq 200)

    if ($wasm) {
    $abort = New-Object System.Net.Sockets.TcpClient
    $abort.Connect('127.0.0.1', $Port)
    $abortStream = $abort.GetStream()
    $abortBytes = [System.Text.Encoding]::ASCII.GetBytes("GET /_framework/$($wasm.Name) HTTP/1.1`r`nHost: localhost`r`n`r`n")
    $abortStream.Write($abortBytes, 0, $abortBytes.Length)
    $abort.Client.LingerState = New-Object System.Net.Sockets.LingerOption($true, 0)
    $abort.Close()
    Start-Sleep -Milliseconds 300
    $after = Send-Raw $Port 'GET / HTTP/1.1'
    Check 'server survives a client that aborts mid-download' ($after.Status -eq 200)
    }
    Check 'server process is still running at the end' (-not $script:process.HasExited) '(exited early)'

    Write-Host 'INFO  http.sys opens the port on all interfaces; the script itself only answers loopback clients.'

    $external = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } | Select-Object -First 1)
    if ($external.Count -gt 0) {
        $status = 0
        $leaked = $false
        try {
            $viaNetwork = Send-Raw $Port 'GET / HTTP/1.1' $external[0].IPAddress
            $status = $viaNetwork.Status
            $leaked = $viaNetwork.Status -eq 200
        } catch { }
        Check ('request via ' + $external[0].IPAddress + ' is not served') (-not $leaked) "($status)"
    } else {
        Write-Host 'INFO  no non-loopback IPv4 address, external requests not checked'
    }
} catch {
    Write-Host ('ERROR ' + $_.Exception.Message)
    $script:failures.Add('exception: ' + $_.Exception.Message)
} finally {
    if ($script:process -and -not $script:process.HasExited) {
        Stop-Process -Id $script:process.Id -Force -ErrorAction SilentlyContinue
        $script:process.WaitForExit(5000) | Out-Null
    }
    $stillUp = $false
    try {
        $probe = New-Object System.Net.Sockets.TcpClient
        $probe.Connect('127.0.0.1', $Port)
        $probe.Close()
        $stillUp = $true
    } catch { }
    if ($script:process) { Check 'server is stopped at the end' (-not $stillUp) }
}

if ($script:failures.Count -gt 0) {
    foreach ($log in @(@{ Name = 'server stdout'; Path = $stdoutFile }, @{ Name = 'server stderr'; Path = $stderrFile })) {
        if ((Test-Path -LiteralPath $log.Path) -and (Get-Item -LiteralPath $log.Path).Length -gt 0) {
            Write-Host ('---- ' + $log.Name + ' ----')
            Get-Content -LiteralPath $log.Path | Write-Host
        }
    }
    Remove-Item -LiteralPath $stdoutFile, $stderrFile -Force -ErrorAction SilentlyContinue
    Write-Host ''
    Write-Host ('SMOKE TEST FAILED: ' + $script:failures.Count + ' problem(s)')
    exit 1
}
Remove-Item -LiteralPath $stdoutFile, $stderrFile -Force -ErrorAction SilentlyContinue
Write-Host ''
Write-Host 'SMOKE TEST PASSED'
exit 0
