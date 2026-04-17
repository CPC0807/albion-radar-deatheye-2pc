@echo off
echo ========================================
echo Protocol 18 Implementation Diagnostic
echo ========================================
echo.

echo [1/5] Checking if PhotonParser.cs exists...
if exist "Radar\Packets\Photon\PhotonParser.cs" (
    echo OK - File exists
) else (
    echo ERROR - File not found!
    goto :error
)

echo.
echo [2/5] Checking if Protocol18Deserializer.cs exists...
if exist "Radar\Packets\Photon\Protocol18Deserializer.cs" (
    echo OK - File exists
) else (
    echo ERROR - File not found!
    goto :error
)

echo.
echo [3/5] Checking for old method calls in PhotonParser.cs...
findstr /C:"ReceiveRequest" /C:"ReceiveResponse" /C:"ReceiveEvent" "Radar\Packets\Photon\PhotonParser.cs" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo WARNING - Old method calls found!
    echo This should NOT happen. File may not be saved.
    findstr /N /C:"ReceiveRequest" /C:"ReceiveResponse" /C:"ReceiveEvent" "Radar\Packets\Photon\PhotonParser.cs"
) else (
    echo OK - No old method calls found
)

echo.
echo [4/5] Checking if files are in DEATHEYE.csproj...
findstr /C:"Protocol18Deserializer.cs" DEATHEYE.csproj >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo OK - Protocol18Deserializer.cs is in project
) else (
    echo ERROR - Protocol18Deserializer.cs NOT in project!
    goto :error
)

findstr /C:"PhotonParser.cs" DEATHEYE.csproj >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo OK - PhotonParser.cs is in project
) else (
    echo ERROR - PhotonParser.cs NOT in project!
    goto :error
)

echo.
echo [5/5] Checking obj and bin folders (cache)...
if exist "obj\" (
    echo Found obj\ folder - will be deleted during clean
)
if exist "bin\" (
    echo Found bin\ folder - will be deleted during clean
)

echo.
echo ========================================
echo Diagnostic Complete
echo ========================================
echo.
echo RECOMMENDATION:
echo 1. Close Visual Studio completely
echo 2. Delete obj\ and bin\ folders manually
echo 3. Run rebuild.bat
echo.
pause
goto :eof

:error
echo.
echo ========================================
echo ERRORS FOUND
echo ========================================
echo Please check the errors above and fix them.
echo.
pause
