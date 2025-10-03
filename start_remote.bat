@echo off
echo =====================================
echo DEATHEYE Radar - Remote Mode Starter
echo =====================================
echo.

REM Check if network_config.json exists
if not exist "network_config.json" (
    echo Creating default network_config.json...
    echo {> network_config.json
    echo   "mode": "remote",>> network_config.json
    echo   "remote_port": 9999,>> network_config.json
    echo   "game_port": 5050>> network_config.json
    echo }>> network_config.json
    echo Done!
    echo.
)

echo Current Configuration:
type network_config.json
echo.
echo =====================================
echo.
echo Starting DEATHEYE in REMOTE mode...
echo.
echo Make sure:
echo   1. Ubuntu sender is ready to connect
echo   2. Firewall allows port 9999
echo   3. Your IP is known to sender
echo.
echo Press any key to start...
pause > nul

X975.exe
