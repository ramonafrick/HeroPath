@echo off
title HeroPath
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if not exist "%PS%" goto failed
"%PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start.ps1"
set "CODE=%errorlevel%"
if not "%CODE%"=="0" goto failed
goto :eof

:failed
echo.
echo HeroPath konnte nicht gestartet werden.
echo.
echo Was jetzt hilft: HeroPath laeuft auch im Internet unter
echo     https://ramonafrick.github.io/HeroPath/
echo Diese Adresse im Browser oeffnen. Weitere Hinweise stehen in LIESMICH.txt.
echo.
pause
