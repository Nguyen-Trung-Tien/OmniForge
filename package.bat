@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul
title OmniForge Production Installer Packaging Pipeline

:: Initialize ANSI color formatting
for /F "delims=#" %%E in ('"prompt #$E# & for %%e in (1) do rem"') do set "ESC=%%E"
set "RESET=%ESC%[0m"
set "BOLD=%ESC%[1m"
set "RED=%ESC%[91m"
set "GREEN=%ESC%[92m"
set "YELLOW=%ESC%[93m"
set "CYAN=%ESC%[96m"
set "WHITE=%ESC%[97m"
set "GRAY=%ESC%[90m"

echo.
echo %CYAN%%BOLD%========================================================================%RESET%
echo %CYAN%%BOLD%   OmniForge Utility Suite :: Production Installer Packaging Pipeline   %RESET%
echo %CYAN%%BOLD%========================================================================%RESET%
echo.

:: 1. Check for .NET SDK
echo %GRAY%[1/5]%RESET% Verifying .NET 9.0 SDK installation...
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo %RED%%BOLD%[ERROR] .NET SDK was not found in your system PATH.%RESET%
    echo %YELLOW%Please download and install .NET 9.0 SDK from:%RESET%
    echo https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

for /f "delims=" %%V in ('dotnet --version 2^>nul') do set "DOTNET_VERSION=%%V"
echo   %GREEN%[  OK  ]%RESET% .NET SDK Version: %WHITE%!DOTNET_VERSION!%RESET%

:: 2. Locate Inno Setup Compiler (ISCC.exe)
echo %GRAY%[2/5]%RESET% Locating Inno Setup 6 compiler (ISCC.exe)...
set "ISCC="

if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" (
    set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
    goto :FoundISCC
)

if exist "C:\Users\%USERNAME%\AppData\Local\Programs\Inno Setup 6\ISCC.exe" (
    set "ISCC=C:\Users\%USERNAME%\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
    goto :FoundISCC
)

if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
    goto :FoundISCC
)

if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
    set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
    goto :FoundISCC
)

where iscc >nul 2>&1
if %errorlevel% equ 0 (
    for /f "delims=" %%I in ('where iscc 2^>nul') do (
        set "ISCC=%%I"
        goto :FoundISCC
    )
)

:FoundISCC
if "%ISCC%"=="" (
    echo.
    echo %RED%%BOLD%[ERROR] Inno Setup 6 Compiler [ISCC.exe] was not found.%RESET%
    echo %YELLOW%Please download and install Inno Setup 6 from:%RESET%
    echo https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

echo   %GREEN%[  OK  ]%RESET% Inno Setup Compiler: %WHITE%"!ISCC!"%RESET%

:: 3. Terminate running OmniForge processes & prepare directory
echo %GRAY%[3/5]%RESET% Cleaning workspace and preparing artifact directories...
taskkill /F /IM OmniForge.App.exe >nul 2>&1

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

if exist "artifacts\publish\win-x64" rmdir /s /q "artifacts\publish\win-x64" 2>nul
if not exist "release\installer" mkdir "release\installer" 2>nul
echo   %GREEN%[  OK  ]%RESET% Build and release directories ready.

:: 4. Publish Self-Contained WinUI 3 App
echo.
echo %CYAN%%BOLD%------------------------------------------------------------------------%RESET%
echo %GRAY%[4/5]%RESET% Publishing Self-Contained Release (win-x64)...
echo %CYAN%%BOLD%------------------------------------------------------------------------%RESET%

dotnet publish src\OmniForge.App\OmniForge.App.csproj ^
    -c Release ^
    -r win-x64 ^
    -p:Platform=x64 ^
    --self-contained true ^
    -o artifacts\publish\win-x64 ^
    --nologo

if %errorlevel% neq 0 (
    echo.
    echo %RED%%BOLD%[ERROR] dotnet publish failed. Please inspect build errors above.%RESET%
    echo.
    pause
    exit /b 1
)

:: Validate essential publish artifacts
if not exist "artifacts\publish\win-x64\OmniForge.App.exe" (
    echo.
    echo %RED%%BOLD%[ERROR] OmniForge.App.exe was not found in publish output.%RESET%
    pause
    exit /b 1
)

if not exist "artifacts\publish\win-x64\resources.pri" (
    if not exist "artifacts\publish\win-x64\OmniForge.App.pri" (
        echo.
        echo %RED%%BOLD%[ERROR] Package Resource Index [PRI] file missing from publish directory.%RESET%
        pause
        exit /b 1
    )
)

if not exist "artifacts\publish\win-x64\Assets\app.ico" (
    echo   %YELLOW%[ WARN ]%RESET% Supplementing Assets\app.ico in publish directory...
    if not exist "artifacts\publish\win-x64\Assets" mkdir "artifacts\publish\win-x64\Assets"
    copy /y "src\OmniForge.App\Assets\app.ico" "artifacts\publish\win-x64\Assets\app.ico" >nul
)
echo   %GREEN%[  OK  ]%RESET% Release binaries and XAML assets verified.

:: 5. Compile Inno Setup Installer
echo.
echo %CYAN%%BOLD%------------------------------------------------------------------------%RESET%
echo %GRAY%[5/5]%RESET% Compiling Inno Setup Production Installer (.EXE)...
echo %CYAN%%BOLD%------------------------------------------------------------------------%RESET%

"%ISCC%" /Qp installer\OmniForge.iss

if %errorlevel% neq 0 (
    echo.
    echo %RED%%BOLD%[ERROR] Inno Setup packaging failed. Please check the log output above.%RESET%
    echo.
    pause
    exit /b 1
)

:: Find the generated installer and calculate size
set "INSTALLER_NAME="
set "INSTALLER_BYTES=0"
for %%F in ("release\installer\*.exe") do (
    set "INSTALLER_NAME=%%~nxF"
    set "INSTALLER_PATH=%%~fF"
    set "INSTALLER_BYTES=%%~zF"
)

set /a "INSTALLER_MB=INSTALLER_BYTES / 1048576"

echo.
echo %GREEN%%BOLD%========================================================================%RESET%
echo %GREEN%%BOLD%  [SUCCESS] Production Installer Created Successfully!                  %RESET%
echo %GREEN%%BOLD%========================================================================%RESET%
echo   %WHITE%%BOLD%Package  :%RESET% %CYAN%!INSTALLER_NAME!%RESET%
echo   %WHITE%%BOLD%Size     :%RESET% ~!INSTALLER_MB! MB
echo   %WHITE%%BOLD%Release  :%RESET% %SCRIPT_DIR%release\installer
echo %GREEN%%BOLD%========================================================================%RESET%
echo.

echo Window will close automatically in:
for /L %%I in (5,-1,1) do (
    echo    ... %%I seconds
    ping 127.0.0.1 -n 2 >nul
)

exit /b 0
