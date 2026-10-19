@echo off
setlocal
set "PROJECT=%~dp0UnityProject_Demo"
if not exist "%PROJECT%\Assets" (
 mkdir "%PROJECT%"
 tar -xf "%~dp0Downloads\JewelryStore_Unity6000.6.2f1.zip" -C "%PROJECT%"
 if errorlevel 1 exit /b 1
)
call "%~dp0Tools\sync-project.cmd" "%PROJECT%" || exit /b 1
set "UNITY=%JEWELRY_UNITY%"
if not defined UNITY set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
start "" "%UNITY%" -projectPath "%PROJECT%"
