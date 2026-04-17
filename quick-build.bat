@echo off
echo ========================================
echo Quick Build - Protocol 18 Implementation
echo ========================================
echo.

REM Set MSBuild path
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

if not exist %MSBUILD% (
    echo ERROR: MSBuild not found at expected location
    echo Please update MSBUILD path in this script
    pause
    exit /b 1
)

echo Building Debug configuration...
echo.

%MSBUILD% DEATHEYE.sln /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal /nologo

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo BUILD SUCCESSFUL!
    echo ========================================
    echo.
    echo Output: bin\Debug\VRise.exe
    echo.
    echo Next steps:
    echo 1. Run Albion Online
    echo 2. Run bin\Debug\VRise.exe
    echo 3. Check console output
    echo.
) else (
    echo.
    echo ========================================
    echo BUILD FAILED
    echo ========================================
    echo.
    echo Please check the errors above.
    echo Common issues:
    echo - Missing dependencies: Run 'nuget restore DEATHEYE.sln'
    echo - Syntax errors: Check the error messages
    echo - Type mismatches: Fixed in latest update
    echo.
)

pause
