@echo off
setlocal EnableExtensions
rem Opens the editable Unity project for your own adjustments.
rem The first time, it unpacks Downloads\JewelryStore_Unity6000.6.2f1.zip into UnityProject beside this file
rem (an existing UnityProject_Glass from earlier is used instead, so nothing set up before is lost).
rem Every time, Tools\sync-project.cmd first copies the latest Prototype Scripts, Editor, Shaders and
rem ThirdParty into it. Edit code in Prototype, not inside the project's copies of those four folders:
rem the next sync overwrites them. Scenes, settings and anything else in the project are yours to keep.
rem Needs Unity 6000.6.2f1 (Unity Hub default path, or set JEWELRY_UNITY to Unity.exe).
set "ROOT=%~dp0"
set "SNAPSHOT=%ROOT%Downloads\JewelryStore_Unity6000.6.2f1.zip"
set "UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
if defined JEWELRY_UNITY set "UNITY_EDITOR=%JEWELRY_UNITY%"
if not exist "%UNITY_EDITOR%" (
  echo Unity 6000.6.2f1 was not found. Install it with Unity Hub, or set JEWELRY_UNITY to the full path of Unity.exe.
  pause
  exit /b 1
)
set "PROJECT=%ROOT%UnityProject"
if not exist "%PROJECT%\Assets" if exist "%ROOT%UnityProject_Glass\Assets" set "PROJECT=%ROOT%UnityProject_Glass"
if not exist "%PROJECT%\Assets" (
  if not exist "%SNAPSHOT%" (
    echo Missing "%SNAPSHOT%"
    pause
    exit /b 1
  )
  echo First run: unpacking the Unity project into UnityProject, one time only...
  mkdir "%PROJECT%" 2>nul
  tar -xf "%SNAPSHOT%" -C "%PROJECT%"
  if errorlevel 1 powershell -NoProfile -Command "Expand-Archive -Force -LiteralPath '%SNAPSHOT%' -DestinationPath '%PROJECT%'"
)
if not exist "%PROJECT%\Assets" (
  echo Unpacking failed.
  pause
  exit /b 1
)
echo Copying the latest scripts and assets into "%PROJECT%"...
call "%ROOT%Tools\sync-project.cmd" "%PROJECT%" || (pause & exit /b 1)
echo Opening Unity with Package Manager temporarily disabled. Newly copied assets are imported when it opens, which can take several minutes.
start "" "%UNITY_EDITOR%" -noUpm -projectPath "%PROJECT%"
