@echo off
setlocal

if "%~1"=="" (
    echo Usage: scripts\publish-sdk.cmd VERSION [additional Publish-DreambitSdk.ps1 options]
    echo Example: scripts\publish-sdk.cmd 0.1.8
    exit /b 2
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Publish-DreambitSdk.ps1" %*
exit /b %ERRORLEVEL%
