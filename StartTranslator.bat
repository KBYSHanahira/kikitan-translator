@echo off
setlocal EnableExtensions
title Kikitan Translator Launcher
cd /d "%~dp0"

rem ===========================================================================
rem  Kikitan Translator launcher - double-click to run.
rem
rem  Missing prerequisites are downloaded automatically into the ".tools"
rem  folder next to this file (no admin rights, nothing installed system-wide):
rem    - Node.js   (builds and serves the user interface)
rem    - .NET 9 SDK (builds and runs the app)
rem  WebView2 Runtime is installed via Microsoft's official installer only if
rem  Windows does not already have it (Windows 11 ships with it).
rem
rem  Set KIKITAN_FORCE_LOCAL=1 to ignore system-wide Node/.NET and use .tools.
rem ===========================================================================

set "ROOT=%~dp0"
set "TOOLS=%ROOT%.tools"
set "UI_DIR=%ROOT%KikitanTranslator.Photino\UserInterface"
set "APP_DIR=%ROOT%KikitanTranslator.Photino"
set "UI_PORT=1420"
set "NODE_VERSION=24.20.0"
set "DOTNET_CHANNEL=9.0"

set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"

if not exist "%TOOLS%" mkdir "%TOOLS%"

echo.
echo  Kikitan Translator
echo  ------------------
echo.

call :ensure_node || goto :fail
call :ensure_dotnet || goto :fail
call :ensure_webview2 || goto :fail
call :ensure_ui_deps || goto :fail
call :start_ui || goto :fail

echo [5/5] Starting Kikitan Translator (first start can take a minute)...
rem Debug builds load Resources/wwwroot relative to the working directory,
rem so the app must run from its project folder.
pushd "%APP_DIR%"
dotnet run -c Debug
set "APP_EXIT=%ERRORLEVEL%"
popd

call :stop_ui
if not "%APP_EXIT%"=="0" (
    echo.
    echo  The app closed with an error ^(code %APP_EXIT%^). See the messages above.
    pause
)
exit /b %APP_EXIT%


rem ---------------------------------------------------------------------------
:ensure_node
echo [1/5] Checking Node.js...
if exist "%TOOLS%\node\node.exe" (
    set "PATH=%TOOLS%\node;%PATH%"
    echo       Using local Node.js in .tools
    exit /b 0
)
if not "%KIKITAN_FORCE_LOCAL%"=="1" (
    if exist "%ProgramFiles%\nodejs\node.exe" set "PATH=%ProgramFiles%\nodejs;%PATH%"
    where node >nul 2>&1 && (
        echo       Found Node.js on this PC
        exit /b 0
    )
)
echo       Node.js not found - downloading Node.js %NODE_VERSION% (about 30 MB)...
set "NODE_ZIP=%TOOLS%\node.zip"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol='Tls12'; Invoke-WebRequest -UseBasicParsing 'https://nodejs.org/dist/v%NODE_VERSION%/node-v%NODE_VERSION%-win-x64.zip' -OutFile '%NODE_ZIP%'"
if errorlevel 1 (
    echo       Download failed. Check your internet connection and try again.
    exit /b 1
)
tar -xf "%NODE_ZIP%" -C "%TOOLS%"
if errorlevel 1 (
    echo       Could not unpack Node.js.
    exit /b 1
)
move "%TOOLS%\node-v%NODE_VERSION%-win-x64" "%TOOLS%\node" >nul
del "%NODE_ZIP%" >nul 2>&1
if not exist "%TOOLS%\node\node.exe" (
    echo       Node.js install looks incomplete.
    exit /b 1
)
set "PATH=%TOOLS%\node;%PATH%"
echo       Node.js ready
exit /b 0


rem ---------------------------------------------------------------------------
:ensure_dotnet
echo [2/5] Checking .NET 9 SDK...
if exist "%TOOLS%\dotnet\dotnet.exe" goto :use_local_dotnet
if not "%KIKITAN_FORCE_LOCAL%"=="1" (
    dotnet --list-sdks 2>nul | findstr /b "9." >nul && (
        echo       Found .NET 9 SDK on this PC
        exit /b 0
    )
)
echo       .NET 9 SDK not found - downloading it (about 250 MB, one time only)...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol='Tls12'; Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1' -OutFile '%TOOLS%\dotnet-install.ps1'; & '%TOOLS%\dotnet-install.ps1' -Channel %DOTNET_CHANNEL% -InstallDir '%TOOLS%\dotnet' -NoPath"
if errorlevel 1 (
    echo       .NET download failed. Check your internet connection and try again.
    exit /b 1
)
if not exist "%TOOLS%\dotnet\dotnet.exe" (
    echo       .NET install looks incomplete.
    exit /b 1
)
:use_local_dotnet
set "DOTNET_ROOT=%TOOLS%\dotnet"
set "DOTNET_MULTILEVEL_LOOKUP=0"
set "PATH=%TOOLS%\dotnet;%PATH%"
echo       Using local .NET SDK in .tools
exit /b 0


rem ---------------------------------------------------------------------------
:ensure_webview2
echo [3/5] Checking WebView2 Runtime...
set "WV2_KEY={F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\%WV2_KEY%" /v pv >nul 2>&1 && goto :wv2_ok
reg query "HKCU\SOFTWARE\Microsoft\EdgeUpdate\Clients\%WV2_KEY%" /v pv >nul 2>&1 && goto :wv2_ok
echo       WebView2 not found - installing Microsoft's WebView2 Runtime...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol='Tls12'; Invoke-WebRequest -UseBasicParsing 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile '%TOOLS%\MicrosoftEdgeWebview2Setup.exe'"
if errorlevel 1 (
    echo       WebView2 download failed. Check your internet connection and try again.
    exit /b 1
)
"%TOOLS%\MicrosoftEdgeWebview2Setup.exe" /silent /install
del "%TOOLS%\MicrosoftEdgeWebview2Setup.exe" >nul 2>&1
:wv2_ok
echo       WebView2 ready
exit /b 0


rem ---------------------------------------------------------------------------
:ensure_ui_deps
echo [4/5] Preparing the user interface...
if exist "%UI_DIR%\node_modules\vite" exit /b 0
echo       Installing UI packages (first run only)...
pushd "%UI_DIR%"
call npm ci --no-audit --no-fund --loglevel=error
set "NPM_EXIT=%ERRORLEVEL%"
popd
if not "%NPM_EXIT%"=="0" (
    echo       Installing UI packages failed.
    exit /b 1
)
exit /b 0


rem ---------------------------------------------------------------------------
:start_ui
set "UI_STARTED_HERE="
call :find_ui_pid
if defined UI_PID (
    echo       UI server already running on port %UI_PORT%
    exit /b 0
)
start "Kikitan UI Server" /min cmd /c "cd /d "%UI_DIR%" && npm run dev"
set "UI_STARTED_HERE=1"
for /l %%i in (1,1,60) do (
    call :find_ui_pid
    if defined UI_PID exit /b 0
    rem ping instead of timeout: timeout fails when input is redirected
    ping -n 2 127.0.0.1 >nul
)
echo       The UI server did not start within 60 seconds.
call :stop_ui
exit /b 1

:find_ui_pid
set "UI_PID="
for /f "tokens=5" %%p in ('netstat -ano ^| findstr /r /c:":%UI_PORT% .*LISTENING"') do set "UI_PID=%%p"
exit /b 0

:stop_ui
if not defined UI_STARTED_HERE exit /b 0
call :find_ui_pid
if defined UI_PID taskkill /PID %UI_PID% /T /F >nul 2>&1
taskkill /FI "WINDOWTITLE eq Kikitan UI Server*" /T /F >nul 2>&1
exit /b 0


rem ---------------------------------------------------------------------------
:fail
echo.
echo  Could not start Kikitan Translator. See the message above.
pause
exit /b 1
