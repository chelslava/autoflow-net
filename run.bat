@echo off
REM ============================================================
REM AutoFlow.NET - Quick Start Commands for Windows
REM ============================================================

echo.
echo  AutoFlow.NET - Available Commands
echo  ================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo  [ERROR] .NET SDK not found. Please install .NET 10 SDK.
    echo  Download: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo  [OK] .NET SDK detected
echo.

:menu
echo  Select command:
echo.
echo  === RUN EXAMPLES ===
echo  1. Run basic example
echo  2. Run browser login example
echo  3. Run e-commerce example
echo  4. Run RPA Challenge
echo  5. Run REFramework template
echo.
echo  === VALIDATE ===
echo  6. Validate workflow
echo  7. Dry-run workflow
echo.
echo  === CLI TOOLS ===
echo  8. List available keywords
echo  9. Show execution history
echo  10. Show statistics
echo.
echo  === VS CODE ===
echo  11. Open VS Code with extension
echo.
echo  0. Exit
echo.

set /p choice="Enter number: "

if "%choice%"=="1" goto run_basic
if "%choice%"=="2" goto run_login
if "%choice%"=="3" goto run_ecommerce
if "%choice%"=="4" goto run_rpa
if "%choice%"=="5" goto run_reframework
if "%choice%"=="6" goto validate
if "%choice%"=="7" goto dryrun
if "%choice%"=="8" goto keywords
if "%choice%"=="9" goto history
if "%choice%"=="10" goto stats
if "%choice%"=="11" goto vscode
if "%choice%"=="0" goto end

echo Invalid choice.
goto menu

:run_basic
echo.
echo [Running basic example...]
dotnet run --project src/AutoFlow.Cli -- run examples/flow.yaml
goto continue

:run_login
echo.
echo [Running browser login example...]
dotnet run --project src/AutoFlow.Cli -- run examples/browser_login.yaml
goto continue

:run_ecommerce
echo.
echo [Running e-commerce example...]
dotnet run --project src/AutoFlow.Cli -- run examples/browser_ecommerce.yaml
goto continue

:run_rpa
echo.
echo [Running RPA Challenge...]
dotnet run --project src/AutoFlow.Cli -- run examples/rpa_challenge.yaml
goto continue

:run_reframework
echo.
echo [Running REFramework template...]
dotnet run --project src/AutoFlow.Cli -- run examples/reframework/main.yaml
goto continue

:validate
echo.
set /p file="Enter workflow path (e.g., examples/flow.yaml): "
dotnet run --project src/AutoFlow.Cli -- validate %file%
goto continue

:dryrun
echo.
set /p file="Enter workflow path: "
dotnet run --project src/AutoFlow.Cli -- validate %file% --dry-run
goto continue

:keywords
echo.
echo [Available keywords...]
dotnet run --project src/AutoFlow.Cli -- list-keywords
goto continue

:history
echo.
echo [Execution history...]
dotnet run --project src/AutoFlow.Cli -- history
goto continue

:stats
echo.
echo [Statistics...]
dotnet run --project src/AutoFlow.Cli -- stats
goto continue

:vscode
echo.
echo [Opening VS Code...]
code --extensionDevelopmentPath=vscode-autoflow .
goto continue

:continue
echo.
echo ------------------------------------------------------------
pause
cls
goto menu

:end
echo.
echo Goodbye!
exit /b 0
