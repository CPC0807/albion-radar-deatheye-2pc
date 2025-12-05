@echo off
echo ========================================
echo VRise - Compile and Run
echo ========================================
echo.

echo [1/3] Cleaning previous build...
msbuild DEATHEYE.sln /t:Clean /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal

echo.
echo [2/3] Building project (Debug)...
msbuild DEATHEYE.sln /t:Rebuild /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo BUILD FAILED! Check errors above.
    echo ========================================
    pause
    exit /b 1
)

echo.
echo ========================================
echo BUILD SUCCESS!
echo ========================================
echo.
echo [3/3] Starting VRise...
echo.
echo IMPORTANT:
echo 1. Make sure Albion Online is running
echo 2. Switch maps to trigger KeySync scan
echo 3. Look for [FOUND!] message in console
echo.
echo ========================================
echo.

cd bin\Debug
start VRise.exe

echo Application started. Check the console window.
echo.
pause
