@echo off
setlocal EnableExtensions
title Jewelry Store Demo
rem One double-click: update the source, build the latest demo if it changed, start it.
rem   Start-Demo.cmd          build when the source changed, then start
rem   Start-Demo.cmd bake     rebuild with baked lighting (several minutes longer)
rem   Start-Demo.cmd rebuild  rebuild now with preview lighting
rem   Start-Demo.cmd run      start the existing build without checking
rem Needs Unity 6000.6.2f1 (Unity Hub default path, or set JEWELRY_UNITY to Unity.exe).
cd /d "%~dp0"
set "ROOT=%~dp0"
set "PROJECT=%ROOT%UnityProject_Demo"
set "SNAPSHOT=%ROOT%Downloads\JewelryStore_Glass_Unity6000.6.2f1.zip"
set "EXE=%ROOT%Builds\Windows\JewelryStoreDemo.exe"
set "STAMP=%ROOT%Builds\Windows\built-from.txt"
set "LOG=%ROOT%build-latest.log"
set "MODE=%~1"

rem 1. Update the source when this folder is a git checkout (a GitHub ZIP download is not).
rem    The pull runs inside one parenthesised block, which cmd reads in full before it
rem    runs it, and the script then restarts, so a pull that changes this file never
rem    leaves cmd reading the new file at an old position.
set "COMMIT=unknown"
if exist "%ROOT%.git" if not defined JEWELRY_UPDATED (
  set "JEWELRY_UPDATED=1"
  where git >nul 2>nul && (echo Checking GitHub for updates...& git -C "%ROOT%." pull --ff-only)
  call "%~f0" %*
  exit /b
)
if exist "%ROOT%.git" call :update

rem 2. Decide whether to build.
if /i "%MODE%"=="run" goto run
if /i "%MODE%"=="rebuild" goto build
if /i "%MODE%"=="bake" goto build
if not exist "%EXE%" goto build
if "%COMMIT%"=="unknown" (
  echo Starting the existing build. Run "Start-Demo.cmd rebuild" after downloading a newer version.
  goto run
)
set "BUILT="
if exist "%STAMP%" set /p BUILT=<"%STAMP%"
if /i not "%BUILT%"=="%COMMIT%" goto build
goto run

:build
call :findunity || goto fail
if exist "%PROJECT%\Assets" goto sync
if not exist "%SNAPSHOT%" (
  echo Missing "%SNAPSHOT%"
  goto fail
)
echo First run: unpacking the Unity project, one time only...
mkdir "%PROJECT%" 2>nul
tar -xf "%SNAPSHOT%" -C "%PROJECT%"
if errorlevel 1 powershell -NoProfile -Command "Expand-Archive -Force -LiteralPath '%SNAPSHOT%' -DestinationPath '%PROJECT%'"
if not exist "%PROJECT%\Assets" goto fail

:sync
echo Copying the latest scripts and assets into the project...
rem /MIR also removes scripts deleted from Prototype; .meta files are left to Unity.
for %%f in (Scripts Editor Shaders ThirdParty) do (
  robocopy "%ROOT%Prototype\%%f" "%PROJECT%\Assets\CrimeSceneDemo\%%f" /MIR /XF *.meta /NFL /NDL /NJH /NJS /NP >nul
  if errorlevel 8 goto fail
)
set "BAKEFLAG="
if /i "%MODE%"=="bake" set "BAKEFLAG=-demoBake"
set "JEWELRY_COMMIT=%COMMIT%"
echo Building the demo with Unity. The first build takes 5 to 15 minutes; please wait...
"%UNITY%" -batchmode -noUpm -projectPath "%PROJECT%" -executeMethod DemoBuild.BuildLatest %BAKEFLAG% -logFile "%LOG%"
if errorlevel 1 goto fail
if not exist "%EXE%" goto fail
>"%STAMP%" echo %COMMIT%
echo Build complete.

:run
if not exist "%EXE%" (
  echo No build yet. Run Start-Demo.cmd without arguments to build one.
  goto fail
)
echo Starting the demo...
start "" "%EXE%"
exit /b 0

:update
where git >nul 2>nul || exit /b 0
for /f %%c in ('git -C "%ROOT%." rev-parse --short HEAD') do set "COMMIT=%%c"
exit /b 0

:findunity
if defined JEWELRY_UNITY if exist "%JEWELRY_UNITY%" (
  set "UNITY=%JEWELRY_UNITY%"
  exit /b 0
)
set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
if exist "%UNITY%" exit /b 0
for /d %%d in ("C:\Program Files\Unity\Hub\Editor\6000.6*") do if exist "%%d\Editor\Unity.exe" set "UNITY=%%d\Editor\Unity.exe"
if exist "%UNITY%" exit /b 0
echo Unity 6000.6.2f1 was not found. Install it with Unity Hub, or set JEWELRY_UNITY to the full path of Unity.exe.
exit /b 1

:fail
echo.
echo The demo could not be built or started.
if exist "%LOG%" (
  echo Errors from the build log:
  powershell -NoProfile -Command "Select-String -Path '%LOG%' -Pattern 'error CS','JEWELRY_BUILD','Exception' | Select-Object -Last 15 | ForEach-Object { $_.Line }"
  echo Full log: "%LOG%"
)
pause
exit /b 1
