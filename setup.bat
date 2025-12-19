@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

echo ============================================
echo  Reference Setup for ReplantedOnline.csproj
echo ============================================
echo.

REM Define list of subdirectories to search in the MelonLoader folder
set "SEARCH_DIRS=Il2CppAssemblies net6"

REM Determine project location
set "PROJECT_ROOT=%cd%"
set "PROJECT_FILE="
set "ROOT="

REM Check if we're already in the src folder
if exist "ReplantedOnline.csproj" (
    set "PROJECT_FILE=ReplantedOnline.csproj"
    set "ROOT="
) else if exist "src\ReplantedOnline.csproj" (
    set "PROJECT_FILE=src\ReplantedOnline.csproj"
    set "ROOT=src\"
) else if exist "..\ReplantedOnline.csproj" (
    set "PROJECT_FILE=..\ReplantedOnline.csproj"
    set "PROJECT_ROOT=%cd%\.."
    set "ROOT="
) else (
    echo ERROR: ReplantedOnline.csproj not found!
    pause
    exit /b 1
)

REM Get game DLLs path from user
set /p GAME_DLLS_PATH="Enter path to (Game\MelonLoader\): "
if "!GAME_DLLS_PATH!"=="" (
    echo No path entered, exiting.
    pause
    exit /b 1
)

REM Remove quotes if user included them
set "GAME_DLLS_PATH=!GAME_DLLS_PATH:"=!"

if not exist "!GAME_DLLS_PATH!" (
    echo ERROR: Path does not exist: !GAME_DLLS_PATH!
    pause
    exit /b 1
)

echo.
echo Scanning project references...
echo.

set /a EXISTS=0
set /a COPIED=0
set /a MISSING=0

REM Parse the project file for HintPaths
for /f "tokens=*" %%A in ('type "!PROJECT_FILE!" ^| findstr /c:"<HintPath>"') do (
    set "LINE=%%A"
    
    REM Extract HintPath value
    set "HINT_PATH=!LINE:*<HintPath>=!"
    set "HINT_PATH=!HINT_PATH:</HintPath>=!"
    
    REM Clean up: remove tabs, spaces, quotes
    set "HINT_PATH=!HINT_PATH:	=!"
    set "HINT_PATH=!HINT_PATH: =!"
    set "HINT_PATH=!HINT_PATH:"=!"
    
    if not "!HINT_PATH!"=="" (
        call :process_reference "!HINT_PATH!"
    )
)

echo.
echo ================================
echo Summary:
echo !EXISTS! DLLs already present
echo !COPIED! DLLs copied from game
echo !MISSING! DLLs still missing
echo ================================
echo.
pause
exit /b

:process_reference
set "HINT_PATH=%~1"

REM Check if DLL already exists at the HintPath location
if exist "!PROJECT_ROOT!\!ROOT!!HINT_PATH!" (
    set /a EXISTS+=1
    echo EXISTS: !HINT_PATH!
    goto :eof
)

REM Extract just the DLL filename
for %%F in ("!HINT_PATH!") do set "DLL_NAME=%%~nxF"
set "DLL_NAME=!DLL_NAME!"

echo MISSING: !DLL_NAME! (expected: !HINT_PATH!)

REM Search for DLL in the game folder using the search directories list
set "SOURCE_DLL="

REM Loop through each search directory
for %%S in (!SEARCH_DIRS!) do (
    if exist "!GAME_DLLS_PATH!\%%S\!DLL_NAME!" (
        set "SOURCE_DLL=!GAME_DLLS_PATH!\%%S\!DLL_NAME!"
        goto :found_dll
    )
)

REM Also check the root folder itself
if exist "!GAME_DLLS_PATH!\!DLL_NAME!" (
    set "SOURCE_DLL=!GAME_DLLS_PATH!\!DLL_NAME!"
)

:found_dll
if not "!SOURCE_DLL!"=="" (
    echo   FOUND in game: !SOURCE_DLL!
    
    REM Create target directory if it doesn't exist
    for %%D in ("!PROJECT_ROOT!\!ROOT!!HINT_PATH!") do (
        set "TARGET_DIR=%%~dpD"
        set "TARGET_DIR=!TARGET_DIR:~0,-1!"
    )
    
    if not exist "!TARGET_DIR!" (
        mkdir "!TARGET_DIR!"
    )
    
    REM Copy the DLL
    set "TARGET_PATH=!PROJECT_ROOT!\!ROOT!!HINT_PATH!"
    copy /y "!SOURCE_DLL!" "!TARGET_PATH!" >nul
    
    if errorlevel 1 (
        echo   FAILED to copy to: !TARGET_PATH!
    ) else (
        echo   COPIED to: !TARGET_PATH!
        set /a COPIED+=1
    )
) else (
    echo   NOT FOUND in game folder
    set /a MISSING+=1
)

echo.
goto :eof