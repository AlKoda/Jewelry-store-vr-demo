@echo off
setlocal
set "JEWELRY_UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
set "JEWELRY_PROJECT=%~dp0UnityProject"
set "JEWELRY_LOG=%~dp0build-windows.log"
if not exist "%JEWELRY_UNITY%" (
  echo Unity 6000.6.2f1 was not found. Edit JEWELRY_UNITY to your Editor path.
  pause
  exit /b 1
)
if not exist "%JEWELRY_PROJECT%\Assets" (
  echo Extract Downloads\JewelryStore_Expanded_Unity6000.6.2f1.zip into UnityProject first.
  pause
  exit /b 1
)
echo Building the Windows demo into Builds\Windows. Log: %JEWELRY_LOG%
"%JEWELRY_UNITY%" -batchmode -noUpm -quit -projectPath "%JEWELRY_PROJECT%" -executeMethod DemoBuild.BuildWindows -logFile "%JEWELRY_LOG%"
if errorlevel 1 (
  echo Build failed. Search the log for JEWELRY_BUILD_FAIL or "error".
  pause
  exit /b 1
)
echo Build complete: %~dp0Builds\Windows\JewelryStoreDemo.exe
pause
