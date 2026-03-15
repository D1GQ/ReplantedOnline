@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

REM Set base directory where this script is located
set "BASE_DIR=%~dp0"
set "BASE_DIR=%BASE_DIR:~0,-1%"

set "ROOT="

if exist "ReplantedOnline.csproj" (
    set "PROJECT_FILE=ReplantedOnline.csproj"
    set "ROOT="
) else if exist "src\ReplantedOnline.csproj" (
    set "PROJECT_FILE=src\ReplantedOnline.csproj"
    set "ROOT=src\"
) else if exist "..\ReplantedOnline.csproj" (
    set "ROOT="
) else (
    echo ERROR: ReplantedOnline.csproj not found!
    pause
    exit /b 1
)

REM Define dependencies
REM Format: URL|FILENAME|TARGET_PATH
set "DEP_0=https://github.com/PalmForest0/BloomEngine/releases/download/v0.3.2-beta/BloomEngine.dll|BloomEngine.dll|%ROOT%References\Dependencies\"

REM counting method
set DEP_COUNT=0
:count_loop
if defined DEP_%DEP_COUNT% (
    set /a DEP_COUNT+=1
    goto count_loop
)

set /a DEP_COUNT-=1
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

for /l %%i in (0,1,!DEP_COUNT!) do (
    set "DEP_ENTRY=!DEP_%%i!"
    
    if "!DEP_ENTRY!"=="" goto :skip_empty
    
    REM Parse the entry
    for /f "tokens=1-3 delims=|" %%a in ("!DEP_ENTRY!") do (
        set "URL=%%a"
        set "FILENAME=%%b"
        set "TARGET_PATH=%%c"
    )
    
    set "FULL_PATH=!BASE_DIR!\!TARGET_PATH!!FILENAME!"
    
    echo [%%i] Checking !FILENAME!...
    
    if exist "!FULL_PATH!" (
        echo   Already exists: !TARGET_PATH!!FILENAME!
        set /a SKIPPED+=1
    ) else (
        echo   Downloading to: !TARGET_PATH!!FILENAME!
        
        REM Create target directory (ensure it ends with \)
        if "!TARGET_PATH:~-1!" NEQ "\" set "TARGET_PATH=!TARGET_PATH!\"
        if not exist "!BASE_DIR!\!TARGET_PATH!" mkdir "!BASE_DIR!\!TARGET_PATH!"
        
        REM Download based on available tool
        if "!DOWNLOADER!"=="curl" (
            curl !CURL_OPTS! "!FULL_PATH!" "!URL!" >nul 2>&1
        ) else if "!DOWNLOADER!"=="bitsadmin" (
            bitsadmin /transfer "DownloadJob" /download /priority normal "!URL!" "!FULL_PATH!" >nul 2>&1
        )
        
        if exist "!FULL_PATH!" (
            echo   Download successful!
            set /a DOWNLOADED+=1
        ) else (
            echo   DOWNLOAD FAILED: !FILENAME!
            set /a FAILED+=1
        )
    )
    :skip_empty
    echo.
)

echo ===== DOWNLOAD SUMMARY =====
echo Total dependencies: !DEP_COUNT!
echo Downloaded: !DOWNLOADED!
echo Already present: !SKIPPED!
echo Failed: !FAILED!
echo ============================
echo.
pause
