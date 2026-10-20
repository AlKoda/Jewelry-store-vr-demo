@echo off
setlocal EnableExtensions
rem Copies the evidence photos taken in the Quest 3 app to Photos-From-Quest beside the README.
rem The headset must be connected by USB with USB debugging allowed. Photos stay on the headset too.
for %%i in ("%~dp0..") do set "ROOT=%%~fi"
set "UNITY=%JEWELRY_UNITY%"
if not defined UNITY set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
for %%i in ("%UNITY%") do set "UNITYDIR=%%~dpi"
set "ADB=%UNITYDIR%Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
if not exist "%ADB%" (
 echo adb was not found. Install Android Build Support for Unity 6000.6.2f1.
 pause
 exit /b 1
)
if not exist "%ROOT%\Photos-From-Quest" mkdir "%ROOT%\Photos-From-Quest"
"%ADB%" pull /sdcard/Android/data/com.alkoda.jewelrystorecrimescene/files/EvidencePhotos "%ROOT%\Photos-From-Quest"
if errorlevel 1 (
 echo Could not copy the photos. Is the headset connected, and has a photo been taken yet?
 pause
 exit /b 1
)
echo Photos copied to "%ROOT%\Photos-From-Quest".
start "" "%ROOT%\Photos-From-Quest"
