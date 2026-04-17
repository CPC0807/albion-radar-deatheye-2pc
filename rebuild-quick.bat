@echo off
echo ========================================
echo Quick Rebuild - Protocol 18 Fix
echo ========================================
echo.

set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

echo Building...
%MSBUILD% DEATHEYE.sln /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal /nologo

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo BUILD SUCCESS!
echo.
echo Ready to test:
echo 1. Run Albion Online
echo 2. Run bin\Debug\VRise.exe
echo 3. Watch for: [DebugHandler] Event: N
echo.
pause
