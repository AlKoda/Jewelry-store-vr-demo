@echo off
rem VR demo for Meta Quest 3 over Quest Link or Air Link: builds when the source changed, then starts it in VR mode.
rem Start Quest Link (or Air Link) on the PC and put the headset on first. Without a running headset
rem the demo opens in desktop mode and says so on screen.
rem   Play-VR.cmd [bake, rebuild or run]   (see Tools\build-demo.cmd)
set "JEWELRY_DEMO_MODE=vr"
call "%~dp0Tools\build-demo.cmd" %* & exit /b
