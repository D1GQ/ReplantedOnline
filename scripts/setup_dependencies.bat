@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

REM Set base directory where this script is located
set "SCRIPT_DIR=%~dp0"
set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

set "PROJECT_ROOT="
set "ROOT="

REM Find the project root directory (where .csproj is located)
if exist "%SCRIPT_DIR%\ReplantedOnline.csproj" (
    set "PROJECT_ROOT=%SCRIPT_DIR%"
    set "PROJECT_FILE=ReplantedOnline.csproj"
    set "ROOT="
) else if exist "%SCRIPT_DIR%\src\ReplantedOnline.csproj" (
    set "PROJECT_ROOT=%SCRIPT_DIR%"
    set "PROJECT_FILE=src\ReplantedOnline.csproj"
    set "ROOT=src\"
) else if exist "%SCRIPT_DIR%\..\ReplantedOnline.csproj" (
    set "PROJECT_ROOT=%SCRIPT_DIR%\.."
    set "PROJECT_FILE=..\ReplantedOnline.csproj"
    set "ROOT="
) else if exist "%SCRIPT_DIR%\..\src\ReplantedOnline.csproj" (
    set "PROJECT_ROOT=%SCRIPT_DIR%\.."
    set "PROJECT_FILE=..\src\ReplantedOnline.csproj"
    set "ROOT=src\"
) else if exist "%SCRIPT_DIR%\..\..\ReplantedOnline.csproj" (
    set "PROJECT_ROOT=%SCRIPT_DIR%\..\.."
    set "PROJECT_FILE=..\..\ReplantedOnline.csproj"
    set "ROOT="
) else (
    echo ERROR: ReplantedOnline.csproj not found!
    echo Searched in:
    echo   - %SCRIPT_DIR%
    echo   - %SCRIPT_DIR%\src
    echo   - %SCRIPT_DIR%\..
    echo   - %SCRIPT_DIR%\..\src
    echo   - %SCRIPT_DIR%\..\..
    pause
    exit /b 1
)

REM Normalize project root path (resolve ..\ and remove trailing backslash)
for %%i in ("%PROJECT_ROOT%") do set "PROJECT_ROOT=%%~fi"

echo.

REM Define dependencies
REM Format: URL|FILENAME|TARGET_PATH (TARGET_PATH is relative to project root, ROOT will be prepended)
set "DEP_0=https://github.com/PalmForest0/BloomEngine/releases/download/v0.3.2-beta/BloomEngine.dll|BloomEngine.dll|References\Dependencies\"

REM Count dependencies properly
set DEP_COUNT=0
:count_loop
if defined DEP_%DEP_COUNT% (
    set /a DEP_COUNT+=1
    goto count_loop
)

set /a DOWNLOADED=0
set /a SKIPPED=0
set /a FAILED=0

REM Check for curl or bitsadmin
where curl >nul 2>nul && set "DOWNLOADER=curl" && set "CURL_OPTS=-L -o"
where bitsadmin >nul 2>nul && set "DOWNLOADER=bitsadmin"
if not defined DOWNLOADER (
    echo ERROR: No download tool found! Install curl or ensure bitsadmin is available.
    pause
    exit /b 1
)

for /l %%i in (0,1,%DEP_COUNT%) do (
    set "DEP_ENTRY=!DEP_%%i!"
    
    if not "!DEP_ENTRY!"=="" (
        REM Parse the entry
        for /f "tokens=1-3 delims=|" %%a in ("!DEP_ENTRY!") do (
            set "URL=%%a"
            set "FILENAME=%%b"
            set "TARGET_PATH=%%c"
        )
        
        REM Build full path using PROJECT_ROOT and ROOT (like the second script does)
        set "FULL_PATH=!PROJECT_ROOT!\!ROOT!!TARGET_PATH!!FILENAME!"
        
        echo [%%i] Checking !FILENAME!...
        
        if exist "!FULL_PATH!" (
            echo   Already exists: !ROOT!!TARGET_PATH!!FILENAME!
            set /a SKIPPED+=1
        ) else (
            echo   Downloading to: !ROOT!!TARGET_PATH!!FILENAME!
            
            REM Create target directory
            set "TARGET_DIR=!PROJECT_ROOT!\!ROOT!!TARGET_PATH!"
            if not exist "!TARGET_DIR!" mkdir "!TARGET_DIR!"
            
            REM Download based on available tool
            if "!DOWNLOADER!"=="curl" (
                curl !CURL_OPTS! "!FULL_PATH!" "!URL!" >nul 2>&1
            ) else if "!DOWNLOADER!"=="bitsadmin" (
                bitsadmin /transfer "DownloadJob_%%i" /download /priority normal "!URL!" "!FULL_PATH!" >nul 2>&1
            )
            
            if exist "!FULL_PATH!" (
                echo   Download successful!
                set /a DOWNLOADED+=1
            ) else (
                echo   DOWNLOAD FAILED: !FILENAME!
                set /a FAILED+=1
            )
        )
        echo.
    )
)

echo ===== DOWNLOAD SUMMARY =====
echo Total dependencies: %DEP_COUNT%
echo Downloaded: !DOWNLOADED!
echo Already present: !SKIPPED!
echo Failed: !FAILED!
echo ============================
exit /b