@echo off
echo ========================================
echo Npcap Installation Diagnostics
echo ========================================
echo.

echo [1] Checking System32\Npcap directory...
if exist "C:\Windows\System32\Npcap" (
    echo    [OK] Directory exists
    dir "C:\Windows\System32\Npcap" | findstr /i "dll"
) else (
    echo    [FAIL] Directory not found
)

echo.
echo [2] Checking System32\Npcap\wpcap.dll...
if exist "C:\Windows\System32\Npcap\wpcap.dll" (
    echo    [OK] wpcap.dll found
) else (
    echo    [FAIL] wpcap.dll not found
)

echo.
echo [3] Checking System32\Npcap\Packet.dll...
if exist "C:\Windows\System32\Npcap\Packet.dll" (
    echo    [OK] Packet.dll found
) else (
    echo    [FAIL] Packet.dll not found
)

echo.
echo [4] Checking SysWOW64\Npcap directory (for 32-bit apps on 64-bit Windows)...
if exist "C:\Windows\SysWOW64\Npcap" (
    echo    [OK] Directory exists
    dir "C:\Windows\SysWOW64\Npcap" | findstr /i "dll"
) else (
    echo    [FAIL] Directory not found
)

echo.
echo [5] Checking if Npcap service is running...
sc query npcap | findstr /i "RUNNING"
if %errorlevel% == 0 (
    echo    [OK] Npcap service is running
) else (
    echo    [WARN] Npcap service not running or not installed
)

echo.
echo ========================================
echo Diagnosis Complete
echo ========================================
echo.
echo Next steps:
echo.

if not exist "C:\Windows\System32\Npcap\wpcap.dll" (
    echo [ACTION REQUIRED] Reinstall Npcap:
    echo   1. Download from: https://npcap.com/dist/npcap-1.80.exe
    echo   2. During installation, make sure to check:
    echo      [x] Install Npcap in WinPcap API-compatible Mode
    echo   3. Reboot your computer
    echo   4. Run this script again to verify
) else (
    echo [POSSIBLE ISSUE] Npcap is installed but DEATHEYE can't find it.
    echo This might be a 32-bit vs 64-bit compatibility issue.
    echo.
    echo Try these solutions:
    echo   1. Check if DEATHEYE is compiled for the correct platform
    echo   2. Reboot computer to ensure Npcap service starts
    echo   3. Reinstall Npcap with "WinPcap API-compatible Mode" checked
)

echo.
pause
