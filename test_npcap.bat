@echo off
echo ========================================
echo Npcap Installation Test
echo ========================================
echo.

echo Checking Npcap installation...
echo.

if exist "C:\Windows\System32\Npcap\wpcap.dll" (
    echo [OK] Npcap is installed at C:\Windows\System32\Npcap\
    echo.
    dir "C:\Windows\System32\Npcap\" | findstr /i "dll"
) else (
    echo [FAIL] Npcap not found!
    echo Please install from: https://npcap.com/#download
    echo.
    pause
    exit
)

echo.
echo ========================================
echo Next Steps:
echo ========================================
echo.
echo 1. To test packet capture, use Wireshark:
echo    - Run Wireshark as Administrator
echo    - Select your network adapter
echo    - Filter: udp.port == 5050
echo    - Start Albion Online on main PC
echo    - Check if you see UDP packets
echo.
echo 2. If you see packets in Wireshark:
echo    - Your npcap is working correctly
echo    - DEATHEYE should work in local mode
echo.
echo 3. If you don't see packets:
echo    - Check VMware Bridge mode settings
echo    - Uncheck "Replicate physical network connection state"
echo    - Try remote mode with Ubuntu VM sender
echo.
pause
