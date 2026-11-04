# Builds the release ZIP: HeroPath/{Start.bat, Start.ps1, LIESMICH.txt, wwwroot/...}
# Usage: ./deploy/build-zip.ps1 -PublishDir artifacts/publish -Output artifacts/HeroPath.zip
# ASCII only (Windows PowerShell 5.1 reads files without BOM as ANSI).
param(
    [string]$PublishDir = 'artifacts/publish',
    [string]$Output = 'artifacts/HeroPath.zip'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$deployDir = $PSScriptRoot
$site = Join-Path (Resolve-Path -LiteralPath $PublishDir).Path 'wwwroot'
if (-not (Test-Path -LiteralPath (Join-Path $site 'index.html'))) {
    throw "No index.html in $site. Run dotnet publish first."
}

$outputFull = [System.IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outputFull) | Out-Null
$staging = Join-Path ([System.IO.Path]::GetTempPath()) ('heropath-zip-' + [guid]::NewGuid().ToString('N'))
$package = Join-Path $staging 'HeroPath'
$target = Join-Path $package 'wwwroot'

try {
    New-Item -ItemType Directory -Force -Path $target | Out-Null

    # Compressed copies are dropped: the local server sends the plain files.
    & robocopy $site $target /E /XF '*.br' '*.gz' /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }

    # Text files are normalised here because .gitattributes forces LF in the repository.
    # Start.bat and Start.ps1: CRLF, ASCII. LIESMICH.txt: CRLF, UTF-8 with BOM (umlauts).
    $ascii = New-Object System.Text.ASCIIEncoding
    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    $strict = New-Object System.Text.UTF8Encoding($false, $true)
    foreach ($item in @(
            @{ Name = 'Start.bat'; Encoding = $ascii },
            @{ Name = 'Start.ps1'; Encoding = $ascii },
            @{ Name = 'LIESMICH.txt'; Encoding = $utf8Bom })) {
        $bytes = [System.IO.File]::ReadAllBytes((Join-Path $deployDir $item.Name))
        $text = $strict.GetString($bytes)
        if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }
        $text = $text -replace "`r`n", "`n" -replace "`n", "`r`n"
        if ($item.Encoding -is [System.Text.ASCIIEncoding] -and $text -match '[^\x00-\x7F]') {
            throw "$($item.Name) must be ASCII only."
        }
        $content = $item.Encoding.GetPreamble() + $item.Encoding.GetBytes($text)
        [System.IO.File]::WriteAllBytes((Join-Path $package $item.Name), $content)
    }

    if (Test-Path -LiteralPath $outputFull) { Remove-Item -LiteralPath $outputFull -Force }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $outputFull, [System.IO.Compression.CompressionLevel]::Optimal, $false)
} finally {
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
}

$size = (Get-Item -LiteralPath $outputFull).Length
Write-Host ('ZIP: {0} ({1:N1} MB)' -f $outputFull, ($size / 1MB))
