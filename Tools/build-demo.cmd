@echo off
setlocal EnableExtensions
for %%i in ("%~dp0..") do set "ROOT=%%~fi"
set "PROJECT=%ROOT%\UnityProject_Demo"
set "PLAYER=%ROOT%\Builds\Windows\JewelryStoreDemo.exe"
set "UNITY=%JEWELRY_UNITY%"
if not defined UNITY set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
if not defined JEWELRY_DEMO_MODE set "JEWELRY_DEMO_MODE=desktop"
if not exist "%PROJECT%\Assets" (
 mkdir "%PROJECT%"
 tar -xf "%ROOT%\Downloads\JewelryStore_Unity6000.6.2f1.zip" -C "%PROJECT%"
 if errorlevel 1 exit /b 1
)
if /i "%~1"=="run" goto launch
call "%ROOT%\Tools\sync-project.cmd" "%PROJECT%" || exit /b 1
if /i "%~1"=="bake" goto regenerate
if /i "%~1"=="rebuild" goto build
if not exist "%PLAYER%" goto build
powershell -NoProfile -Command "$p=Get-Item -LiteralPath '%PLAYER%'; $n=Get-ChildItem -LiteralPath '%ROOT%\Prototype' -Recurse -File ^| Where-Object {$_.Extension -in '.cs','.shader' -and $_.LastWriteTimeUtc -gt $p.LastWriteTimeUtc} ^| Select-Object -First 1; if($n){exit 1}"
if errorlevel 1 goto build
goto launch
:regenerate
rem Explicit bake regenerates the scene: save intentional layout edits before using it.
set "JEWELRY_COMBINED_OUTPUT=%PLAYER%"
"%UNITY%" -batchmode -force-d3d11 -projectPath "%PROJECT%" -executeMethod CombinedBuild.Run -logFile "%ROOT%\Builds\build-latest.log"
if errorlevel 1 goto fail
goto launch
:build
rem Rebuild existing scene without resetting manual object positions.
"%UNITY%" -batchmode -projectPath "%PROJECT%" -executeMethod EditableDelivery.Run -logFile "%ROOT%\Builds\build-latest.log"
if errorlevel 1 goto fail
:launch
if not exist "%PLAYER%" goto fail
start "" "%PLAYER%" -demo-mode %JEWELRY_DEMO_MODE% -screen-fullscreen 1 -window-mode borderless
exit /b 0
:fail
echo Build unavailable or failed. Check Builds\build-latest.log. No scene was overwritten by a normal rebuild.
pause
exit /b 1
