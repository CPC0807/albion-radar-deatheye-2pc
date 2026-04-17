@echo off
echo Cleaning and rebuilding DEATHEYE project...
echo.

REM Set MSBuild path
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

REM Clean
echo [Step 1/2] Cleaning...
%MSBUILD% DEATHEYE.sln /t:Clean /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal

REM Rebuild
echo.
echo [Step 2/2] Rebuilding...
%MSBUILD% DEATHEYE.sln /t:Rebuild /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal

echo.
echo Build complete!
pause
