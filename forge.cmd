@echo off
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0forge.ps1" %*
if %ERRORLEVEL% NEQ 0 (
    if "%ERRORLEVEL%"=="9009" (
        powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0forge.ps1" %*
    ) else (
        exit /b %ERRORLEVEL%
    )
)

