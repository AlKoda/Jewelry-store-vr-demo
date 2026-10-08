@echo off
setlocal
set "JEWELRY_UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
set "JEWELRY_PROJECT=%~dp0UnityProject"
if not exist "%JEWELRY_UNITY%" (
  echo Unity 6000.6.2f1 was not found. Edit JEWELRY_UNITY to your Editor path.
  pause
  exit /b 1
)
if not exist "%JEWELRY_PROJECT%\Assets" (
  echo Extract Downloads\JewelryStore_Unity6000.6.2f1.zip into UnityProject first.
  pause
  exit /b 1
)
echo Opening geometry/tool project with Package Manager temporarily disabled.
start "" "%JEWELRY_UNITY%" -noUpm -projectPath "%JEWELRY_PROJECT%"
