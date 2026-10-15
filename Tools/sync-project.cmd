@echo off
setlocal EnableExtensions
rem Mirrors the latest Prototype sources into an extracted Unity project:
rem   Tools\sync-project.cmd "C:\path\to\UnityProject_Demo"
rem Prototype\Scripts, Editor, Shaders and ThirdParty go to Assets\CrimeSceneDemo\<same name>.
rem /MIR also removes files deleted from Prototype. .meta files are excluded from the copy and
rem from the removal, so the project keeps its GUIDs and import settings and Unity writes the
rem .meta files of new assets. A project unpacked from the Glass zip alone lacks the twelve
rem realism packs (left out of the zip for size), and the night store then builds their
rem primitive fallbacks; the planned NightStoreValidation check that all twelve are present fails.
rem Called by Start-Demo.cmd (UnityProject_Demo) and Open-Unity-Glass.cmd (UnityProject_Glass).
rem The historical launchers (Open-Unity-Blockout, -Forms, -Interior and -Robbery) do not call it
rem on purpose: they open the frozen October 9-10 snapshots, and /MIR would delete files there and
rem reimport about 1,200 assets under the old importer.
rem Exit code 0 when all four folders are mirrored, 1 otherwise. /R:2 /W:2: a file another
rem program holds fails after two short retries instead of robocopy's default of a million.
set "ROOT=%~dp0.."
for %%i in ("%ROOT%") do set "ROOT=%%~fi"
set "PROJECT=%~1"
if not defined PROJECT (
  echo Usage: Tools\sync-project.cmd "path\to\UnityProject"
  echo Mirrors Prototype Scripts, Editor, Shaders and ThirdParty into Assets\CrimeSceneDemo of that project.
  exit /b 1
)
for %%i in ("%PROJECT%") do set "PROJECT=%%~fi"
if "%PROJECT:~-1%"=="\" set "PROJECT=%PROJECT:~0,-1%"
if not exist "%PROJECT%\Assets\" (
  echo Not a Unity project, the Assets folder is missing: "%PROJECT%\Assets"
  exit /b 1
)
if not exist "%PROJECT%\ProjectSettings\" (
  echo Not a Unity project, the ProjectSettings folder is missing: "%PROJECT%\ProjectSettings"
  exit /b 1
)
for %%f in (Scripts Editor Shaders ThirdParty) do (
  robocopy "%ROOT%\Prototype\%%f" "%PROJECT%\Assets\CrimeSceneDemo\%%f" /MIR /XF *.meta /R:2 /W:2 /NFL /NDL /NJH /NJS /NP >nul
  if errorlevel 8 (
    echo Copying "%ROOT%\Prototype\%%f" to "%PROJECT%\Assets\CrimeSceneDemo\%%f" failed.
    echo Close any program that holds a file there, check the free disk space and try again.
    exit /b 1
  )
)
exit /b 0
