@echo off
setlocal
set "JEWELRY_EXE=%~dp0Builds\Windows\JewelryStoreDemo.exe"
if not exist "%JEWELRY_EXE%" (
  echo No build found. Run Build-Windows.cmd first.
  pause
  exit /b 1
)
start "" "%JEWELRY_EXE%"
