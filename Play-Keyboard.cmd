@echo off
rem Keyboard and mouse demo: builds when the source changed, then starts it in desktop mode.
rem   Play-Keyboard.cmd [bake, rebuild or run]   (see Tools\build-demo.cmd)
set "JEWELRY_DEMO_MODE=desktop"
call "%~dp0Tools\build-demo.cmd" %* & exit /b
