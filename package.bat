@echo off
setlocal
chcp 65001 >nul
title OmniForge Production Installer Packager

echo =====================================================================
echo    OmniForge Utility Suite - Production Installer Builder (.EXE)
echo =====================================================================
echo.

:: 1. Kiem tra .NET SDK
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [LOI] Khong tim thay .NET SDK tren may cua ban.
    echo Vui long cai dat .NET 9.0 SDK tai https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

:: 2. Tu dong tim kiem trinh bien dich Inno Setup (ISCC.exe)
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
    echo [LOI] Khong tim thay trinh bien dich Inno Setup: ISCC.exe
    echo Vui long cai dat Inno Setup 6 tu https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

echo [OK] Trinh bien dich Inno Setup : "%ISCC%"
echo.

:: 3. Dong cac tien trinh OmniForge.App dang chay neu co
taskkill /F /IM OmniForge.App.exe >nul 2>&1

:: 4. Chuyen ve thu muc goc va chuan bi thu muc artifacts
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

echo [1/3] Dang don dep va chuan bi thu muc artifacts...
if exist "artifacts\publish\win-x64" rmdir /s /q "artifacts\publish\win-x64" 2>nul
if not exist "artifacts\installer" mkdir "artifacts\installer" 2>nul

:: 5. Bien dich Release Self-Contained WinUI 3 App
echo [2/3] Dang bien dich dotnet publish (Release, win-x64, Self-Contained)...
dotnet publish src\OmniForge.App\OmniForge.App.csproj ^
    -c Release ^
    -r win-x64 ^
    -p:Platform=x64 ^
    --self-contained true ^
    -o artifacts\publish\win-x64 ^
    --nologo

if %errorlevel% neq 0 (
    echo.
    echo [LOI] Qua trinh dotnet publish that bai. Vui long kiem tra thong bao tren.
    echo.
    pause
    exit /b 1
)

:: Kiem tra su ton tai cua icon va executable
if not exist "artifacts\publish\win-x64\OmniForge.App.exe" (
    echo [LOI] Khong tim thay OmniForge.App.exe trong thu muc publish.
    pause
    exit /b 1
)

if not exist "artifacts\publish\win-x64\Assets\app.ico" (
    echo [CANH BAO] Dang bo sung Assets\app.ico vao goi publish de tranh loi icon...
    if not exist "artifacts\publish\win-x64\Assets" mkdir "artifacts\publish\win-x64\Assets"
    copy /y "src\OmniForge.App\Assets\app.ico" "artifacts\publish\win-x64\Assets\app.ico" >nul
)

:: 6. Dong goi Inno Setup thanh tap tin cai dat .exe
echo [3/3] Dang dong goi bo cai dat Inno Setup .EXE chuan Production...
"%ISCC%" /Qp installer\OmniForge.iss

if %errorlevel% neq 0 (
    echo.
    echo [LOI] Qua trinh dong goi Inno Setup that bai.
    echo.
    pause
    exit /b 1
)

echo.
echo =====================================================================
echo  [THANH CONG] Dong goi Setup .EXE hoan tat xuat sac.
echo  Thu muc dau ra: %SCRIPT_DIR%artifacts\installer
echo =====================================================================
echo.

:: 7. Dem nguoc 5 giay roi tu dong dong cua so
echo Cua so se tu dong dong sau:
for /L %%I in (5,-1,1) do (
    echo    ... %%I giay
    ping 127.0.0.1 -n 2 >nul
)

exit /b 0
