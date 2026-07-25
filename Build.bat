@echo off
setlocal EnableExtensions DisableDelayedExpansion

title BGMMRPlugin - DLL Build

echo.
echo ============================================================
echo   BGMMRPlugin v1.0.0 - DLL Build
echo ============================================================
echo.

set "PROJECT_DIR=%~dp0"
set "PROJECT_FILE=%PROJECT_DIR%BGMMRPlugin.csproj"
set "OUTPUT_DIR=%PROJECT_DIR%dist"
set "BUILT_DLL=%PROJECT_DIR%bin\Release\BGMMRPlugin.dll"
set "INPUT_PATH=%~1"
set "MSBUILD="
set "RESULT_FILE=%TEMP%\BGMMRPlugin_HDT_%RANDOM%_%RANDOM%.txt"
set "ERROR_FILE=%TEMP%\BGMMRPlugin_HDT_ERROR_%RANDOM%_%RANDOM%.txt"

if not exist "%PROJECT_FILE%" (
    echo ERROR: BGMMRPlugin.csproj was not found.
    echo.
    pause
    exit /b 1
)

if not exist "%PROJECT_DIR%lib" mkdir "%PROJECT_DIR%lib"

rem An explicit command-line path always has priority.
if not defined INPUT_PATH (
    if exist "%LOCALAPPDATA%\HearthstoneDeckTracker" (
        set "INPUT_PATH=%LOCALAPPDATA%\HearthstoneDeckTracker"
    )
)

if not defined INPUT_PATH (
    if exist "%ProgramFiles%\Hearthstone Deck Tracker" (
        set "INPUT_PATH=%ProgramFiles%\Hearthstone Deck Tracker"
    )
)

if not defined INPUT_PATH (
    if exist "%ProgramFiles(x86)%\Hearthstone Deck Tracker" (
        set "INPUT_PATH=%ProgramFiles(x86)%\Hearthstone Deck Tracker"
    )
)

if not defined INPUT_PATH (
    echo Enter the Hearthstone Deck Tracker installation folder
    echo or the full path to HearthstoneDeckTracker.exe.
    echo.
    set /p "INPUT_PATH=Path: "
)

set "INPUT_PATH=%INPUT_PATH:"=%"

if not defined INPUT_PATH (
    echo.
    echo ERROR: No HDT path was provided.
    echo.
    pause
    exit /b 1
)

echo Locating a usable HDT assembly...
echo.

powershell.exe ^
    -NoLogo ^
    -NoProfile ^
    -ExecutionPolicy Bypass ^
    -File "%PROJECT_DIR%find_hdt_assembly.ps1" ^
    -InputPath "%INPUT_PATH%" ^
    1>"%RESULT_FILE%" 2>"%ERROR_FILE%"

if errorlevel 1 (
    echo ERROR while locating HearthstoneDeckTracker.exe:
    echo.
    type "%ERROR_FILE%"
    echo.
    del "%RESULT_FILE%" >nul 2>nul
    del "%ERROR_FILE%" >nul 2>nul
    pause
    exit /b 1
)

set /p "HDT_ASSEMBLY="<"%RESULT_FILE%"

del "%RESULT_FILE%" >nul 2>nul
del "%ERROR_FILE%" >nul 2>nul

if not defined HDT_ASSEMBLY (
    echo.
    echo ERROR: No usable HDT assembly was returned.
    echo.
    pause
    exit /b 1
)

for %%F in ("%HDT_ASSEMBLY%") do set "HDT_ASSEMBLY_DIR=%%~dpF"

set "HEARTHDB_ASSEMBLY=%HDT_ASSEMBLY_DIR%HearthDb.dll"
set "HEARTHMIRROR_ASSEMBLY=%HDT_ASSEMBLY_DIR%HearthMirror.dll"

if not exist "%HEARTHDB_ASSEMBLY%" (
    echo.
    echo ERROR: HearthDb.dll was not found beside the selected HDT assembly.
    echo Expected file:
    echo %HEARTHDB_ASSEMBLY%
    echo.
    pause
    exit /b 1
)

if not exist "%HEARTHMIRROR_ASSEMBLY%" (
    echo.
    echo ERROR: HearthMirror.dll was not found beside the selected HDT assembly.
    echo Expected file:
    echo %HEARTHMIRROR_ASSEMBLY%
    echo.
    pause
    exit /b 1
)

echo HDT assembly:
echo %HDT_ASSEMBLY%
echo.

copy /Y "%HDT_ASSEMBLY%" "%PROJECT_DIR%lib\HearthstoneDeckTracker.exe" >nul
if errorlevel 1 goto :copy_error

copy /Y "%HEARTHDB_ASSEMBLY%" "%PROJECT_DIR%lib\HearthDb.dll" >nul
if errorlevel 1 goto :copy_error

copy /Y "%HEARTHMIRROR_ASSEMBLY%" "%PROJECT_DIR%lib\HearthMirror.dll" >nul
if errorlevel 1 goto :copy_error

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%I in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        if not defined MSBUILD set "MSBUILD=%%I"
    )
)

if not defined MSBUILD (
    where msbuild >nul 2>nul
    if not errorlevel 1 set "MSBUILD=msbuild"
)

if not defined MSBUILD (
    echo.
    echo ERROR: MSBuild was not found.
    echo Install Visual Studio 2022 or Build Tools with:
    echo - .NET desktop development
    echo - .NET Framework 4.7.2 Targeting Pack
    echo.
    pause
    exit /b 1
)

echo Cleaning previous build output...

if exist "%PROJECT_DIR%bin" rmdir /s /q "%PROJECT_DIR%bin"
if exist "%PROJECT_DIR%obj" rmdir /s /q "%PROJECT_DIR%obj"
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%" >nul 2>nul

echo.
echo Building BGMMRPlugin...
echo.

"%MSBUILD%" "%PROJECT_FILE%" ^
    /t:Restore,Build ^
    /p:Configuration=Release ^
    /p:Platform=x64 ^
    /m

if errorlevel 1 (
    echo.
    echo ============================================================
    echo   BUILD FAILED
    echo ============================================================
    echo.
    pause
    exit /b 1
)

if not exist "%BUILT_DLL%" (
    echo.
    echo ERROR: BGMMRPlugin.dll was not generated.
    echo Expected file:
    echo %BUILT_DLL%
    echo.
    pause
    exit /b 1
)

copy /Y "%BUILT_DLL%" "%OUTPUT_DIR%\BGMMRPlugin.dll" >nul

if errorlevel 1 (
    echo.
    echo ERROR: The DLL could not be copied to the dist folder.
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   BUILD COMPLETED
echo ============================================================
echo.
echo DLL created:
echo %OUTPUT_DIR%\BGMMRPlugin.dll
echo.
echo Copy this DLL to the HDT Plugins folder.
echo.
pause
exit /b 0

:copy_error
echo.
echo ERROR: HDT reference assemblies could not be copied to the lib folder.
echo.
pause
exit /b 1
