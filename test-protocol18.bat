@echo off
echo ========================================
echo Protocol 18 Test Build
echo ========================================
echo.

set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

echo [1/2] Building...
%MSBUILD% DEATHEYE.sln /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal /nologo

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo [2/2] BUILD SUCCESS!
echo.
echo ========================================
echo Next: Run Albion Online, then VRise.exe
echo ========================================
echo.
echo Expected console output:
echo   [Protocol18Bridge] Event NNN with NNN parameters
echo   [Protocol18Bridge] Request NNN with NNN parameters
echo   [Protocol18Bridge] Response NNN with NNN parameters
echo.
echo If you see these messages, Protocol 18 parsing works!
echo If you see NO messages, check network adapter and port.
echo.
pause
