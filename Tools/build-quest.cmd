@echo off
setlocal EnableExtensions
rem Builds the Meta Quest 3 app from the same project and saved night scene as the PC player
rem (QuestBuild.Run, never regenerating the scene) and installs it on a headset connected by USB.
rem   Build-Quest-App.cmd            build if the source changed, then install
rem   Build-Quest-App.cmd rebuild    build now, then install
rem   Build-Quest-App.cmd install    install the existing Builds\Quest\JewelryStoreDemo.apk only
rem Installing over an earlier version keeps the app's photos and settings (adb install -r).
rem Needs Unity 6000.6.2f1 with the Android Build Support module (OpenJDK, SDK and NDK), and the
rem headset in developer mode, connected by USB, with "Allow USB debugging" accepted inside it.
for %%i in ("%~dp0..") do set "ROOT=%%~fi"
set "PROJECT=%ROOT%\UnityProject_Demo"
set "APK=%ROOT%\Builds\Quest\JewelryStoreDemo.apk"
set "LOG=%ROOT%\Builds\build-quest.log"
set "PACKAGE=com.alkoda.jewelrystorecrimescene"
set "UNITY=%JEWELRY_UNITY%"
if not defined UNITY set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
if not exist "%UNITY%" (
 echo Unity 6000.6.2f1 was not found. Install it with Unity Hub, or set JEWELRY_UNITY to Unity.exe.
 goto fail
)
for %%i in ("%UNITY%") do set "UNITYDIR=%%~dpi"
set "ADB=%UNITYDIR%Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
if not exist "%UNITYDIR%Data\PlaybackEngines\AndroidPlayer" (
 echo Android Build Support is not installed for this Unity.
 echo In Unity Hub: Installs ^> 6000.6.2f1 ^> Add modules ^> Android Build Support, with OpenJDK and Android SDK and NDK Tools.
 goto fail
)
if not exist "%ROOT%\Builds" mkdir "%ROOT%\Builds"
if /i "%~1"=="install" goto install
if not exist "%PROJECT%\Assets" (
 echo First run: unpacking the Unity project, one time only...
 mkdir "%PROJECT%"
 tar -xf "%ROOT%\Downloads\JewelryStore_Unity6000.6.2f1.zip" -C "%PROJECT%"
 if errorlevel 1 goto fail
)
call "%ROOT%\Tools\sync-project.cmd" "%PROJECT%" || goto fail
if /i "%~1"=="rebuild" goto build
if not exist "%APK%" goto build
powershell -NoProfile -Command "$p=Get-Item -LiteralPath '%APK%'; $n=Get-ChildItem -LiteralPath '%ROOT%\Prototype' -Recurse -File ^| Where-Object {$_.LastWriteTimeUtc -gt $p.LastWriteTimeUtc} ^| Select-Object -First 1; $s=Get-ChildItem -LiteralPath '%PROJECT%\Assets\CrimeSceneDemo\Scenes' -File ^| Where-Object {$_.LastWriteTimeUtc -gt $p.LastWriteTimeUtc} ^| Select-Object -First 1; if($n -or $s){exit 1}"
if errorlevel 1 goto build
echo The Quest app is up to date with the source.
goto install
:build
echo Building the Quest 3 app. The first Android build imports every texture for the headset
echo and can take 20 to 60 minutes; later builds are much faster. Log: %LOG%
"%UNITY%" -batchmode -buildTarget Android -projectPath "%PROJECT%" -executeMethod QuestBuild.Run -logFile "%LOG%"
if errorlevel 1 goto fail
if not exist "%APK%" goto fail
:install
if not exist "%APK%" (
 echo No app built yet. Run Build-Quest-App.cmd without arguments first.
 goto fail
)
if not exist "%ADB%" (
 echo adb was not found at "%ADB%". Reinstall Android Build Support with the SDK tools.
 goto fail
)
"%ADB%" start-server >nul 2>nul
"%ADB%" get-state >nul 2>nul
if errorlevel 1 (
 echo No headset found. Connect the Quest 3 by USB, put it on and accept "Allow USB debugging",
 echo then run "Build-Quest-App.cmd install". Developer mode must be on in the Meta Horizon phone app.
 goto fail
)
echo Installing on the headset...
"%ADB%" install -r "%APK%"
if errorlevel 1 goto fail
echo Done. In the headset: App Library ^> Unknown Sources ^> Jewelry Store Crime Scene.
pause
exit /b 0
:fail
echo.
echo The Quest app could not be built or installed.
if exist "%LOG%" (
 echo Errors from the build log:
 powershell -NoProfile -Command "Select-String -Path '%LOG%' -Pattern 'error CS','QUEST_BUILD','Exception' ^| Select-Object -Last 15 ^| ForEach-Object { $_.Line }"
)
pause
exit /b 1
