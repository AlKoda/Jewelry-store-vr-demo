@echo off
setlocal
rem Opens UnityProject_Glass (extract Downloads\JewelryStore_Glass_Unity6000.6.2f1.zip into it once)
rem after Tools\sync-project.cmd has mirrored the latest Prototype Scripts, Editor, Shaders and
rem ThirdParty into it, so the project also carries the realism packs the zip does not.
set "UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
set "PROJECT=%~dp0UnityProject_Glass"
if not exist "%UNITY_EDITOR%" (
  echo Unity 6000.6.2f1 was not found. Edit UNITY_EDITOR in this launcher.
  pause
  exit /b 1
)
if not exist "%PROJECT%\Assets" (
  echo Extract Downloads\JewelryStore_Glass_Unity6000.6.2f1.zip into UnityProject_Glass beside this launcher first.
  pause
  exit /b 1
)
echo Copying the latest scripts and assets into UnityProject_Glass...
call "%~dp0Tools\sync-project.cmd" "%PROJECT%" || (pause & exit /b 1)
echo Opening UnityProject_Glass with Package Manager temporarily disabled. Unity imports newly copied packs when it opens, which can take several minutes.
start "" "%UNITY_EDITOR%" -noUpm -projectPath "%PROJECT%"
