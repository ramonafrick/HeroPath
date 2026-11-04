@echo off
title HeroPath
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start.ps1"
if errorlevel 1 (
    echo.
    echo HeroPath konnte nicht gestartet werden.
    echo.
    echo Was jetzt hilft: HeroPath laeuft auch im Internet unter
    echo     https://ramonafrick.github.io/HeroPath/
    echo Diese Adresse im Browser oeffnen. Weitere Hinweise stehen in LIESMICH.txt.
    echo.
    pause
)
