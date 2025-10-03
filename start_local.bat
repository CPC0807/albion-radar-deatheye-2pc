@echo off
echo =====================================
echo DEATHEYE Radar - Local Mode Starter
echo =====================================
echo.

REM Check if network_config.json exists
if not exist "network_config.json" (
    echo Creating default network_config.json...
    echo {> network_config.json
    echo   "mode": "local",>> network_config.json
    echo   "remote_port": 9999,>> network_config.json
    echo   "game_port": 5050>> network_config.json
    echo }>> network_config.json
    echo Done!
    echo.
) else (
    REM Update mode to local
    powershell -Command "(Get-Content network_config.json) -replace '\"remote\"', '\"local\"' | Set-Content network_config.json"
)

echo Current Configuration:
type network_config.json
echo.
echo =====================================
echo.
echo Starting DEATHEYE in LOCAL mode...
echo.
echo Make sure:
echo   1. Npcap is installed
echo   2. Albion Online is running on this PC
echo.
echo Press any key to start...
pause > nul

X975.exe
