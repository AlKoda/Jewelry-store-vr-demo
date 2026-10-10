@echo off
setlocal
set "UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
if not exist "%UNITY_EDITOR%" (
  echo Unity 6000.6.2f1 was not found. Edit UNITY_EDITOR in this launcher.
  pause
  exit /b 1
)
if not exist "%~dp0UnityProject_Forms\Assets\CrimeSceneDemo\Scenes\JewelryStoreForms.unity" (
  echo Extract the Forms project ZIP into UnityProject_Forms beside this launcher.
  pause
  exit /b 1
)
start "" "%UNITY_EDITOR%" -noUpm -projectPath "%~dp0UnityProject_Forms" -openfile "%~dp0UnityProject_Forms\Assets\CrimeSceneDemo\Scenes\JewelryStoreForms.unity"
