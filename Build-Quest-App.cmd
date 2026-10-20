@echo off
rem Meta Quest 3 standalone app: builds it from the same project and scene as the PC demo, then
rem installs or updates it on the headset over USB. See Docs\QUEST_STANDALONE.md.
rem   Build-Quest-App.cmd [rebuild or install]
call "%~dp0Tools\build-quest.cmd" %*
