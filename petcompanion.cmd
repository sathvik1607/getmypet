@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Dev.ps1" %*
exit /b %errorlevel%
