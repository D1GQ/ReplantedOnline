@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

cls
echo =======================================================================
echo                    REPLANTED ONLINE - PROJECT SETUP
echo =======================================================================
echo.
echo This script will set up your development environment by:
echo.
echo   1. Copying game DLL references from your MelonLoader installation
echo      - Copies required DLLs from your game folder to the project
echo.
echo   2. Downloading external dependencies
echo      - Downloads required libraries
echo.
echo This is required for the project to compile successfully.
echo.
echo =======================================================================
echo.
set /p CONFIRM="Do you want to continue? (Y/N): "

if /i not "!CONFIRM!"=="Y" (
    if /i not "!CONFIRM!"=="YES" (
        exit /b 0
    )
)

echo.
echo Starting setup...
echo.

REM Check if the setup scripts exist
if not exist "scripts\setup_references.bat" (
    echo ERROR: scripts\setup_references.bat not found!
    echo.
    pause
    exit /b 1
)

if not exist "scripts\setup_dependencies.bat" (
    echo ERROR: scripts\setup_dependencies.bat not found!
    echo.
    pause
    exit /b 1
)

cls
call "scripts\setup_references.bat"
echo.
pause

cls
call "scripts\setup_dependencies.bat"
echo.
pause

cls
echo =======================================================================
echo                       SETUP COMPLETED SUCCESSFULLY!
echo =======================================================================
echo.
echo Replanted Online is now ready to build.
echo.
pause