@echo off
chcp 65001 >nul
echo ╔═══════════════════════════════════════════════════╗
echo ║   Git Installation Checker                        ║
echo ╚═══════════════════════════════════════════════════╝
echo.

set ERROR_COUNT=0

echo [1] Checking if Git is installed...
echo ────────────────────────────────────────────────────
git --version >nul 2>&1
if %ERRORLEVEL%==0 (
    echo ✓ Git is installed
    git --version
) else (
    echo ✗ Git is NOT installed or not in PATH
    echo.
    echo 📥 Download Git from:
    echo    https://git-scm.com/download/win
    echo.
    echo ⚠️  During installation, make sure to select:
    echo    "Git from the command line and also from 3rd-party software"
    set /a ERROR_COUNT+=1
    goto python_check
)

echo.
echo [2] Checking Git path...
echo ────────────────────────────────────────────────────
where git
echo.

echo [3] Testing Git functionality...
echo ────────────────────────────────────────────────────
git config --list --global | findstr "user.name" >nul 2>&1
if %ERRORLEVEL%==0 (
    echo ✓ Git configuration found
    git config --list --global | findstr "user"
) else (
    echo ! Git configuration not set (not critical)
    echo   You can set it with:
    echo   git config --global user.name "Your Name"
    echo   git config --global user.email "your@email.com"
)

echo.
echo [4] Checking ao-bin-dumps directory...
echo ────────────────────────────────────────────────────
if exist ao-bin-dumps (
    echo ✓ ao-bin-dumps directory exists

    if exist ao-bin-dumps\.git (
        echo ✓ ao-bin-dumps is a Git repository

        cd ao-bin-dumps
        git status >nul 2>&1
        if %ERRORLEVEL%==0 (
            echo ✓ Git repository is valid
            echo.
            echo   Repository info:
            git log -1 --format="   Last commit: %h - %s (%cr)"
        ) else (
            echo ✗ Git repository may be corrupted
            set /a ERROR_COUNT+=1
        )
        cd ..
    ) else (
        echo ✗ ao-bin-dumps is NOT a Git repository
        echo.
        echo 🔧 To fix, delete ao-bin-dumps and run:
        echo    git clone --depth 1 https://github.com/ao-data/ao-bin-dumps.git
        set /a ERROR_COUNT+=1
    )
) else (
    echo ! ao-bin-dumps directory not found
    echo.
    echo 💡 To create it, run:
    echo    git clone --depth 1 https://github.com/ao-data/ao-bin-dumps.git
)

:python_check
echo.
echo [5] Checking Python installation...
echo ────────────────────────────────────────────────────
python --version >nul 2>&1
if %ERRORLEVEL%==0 (
    echo ✓ Python is installed
    python --version
) else (
    echo ✗ Python is NOT installed or not in PATH
    echo.
    echo 📥 Download Python from:
    echo    https://www.python.org/downloads/
    echo.
    echo ⚠️  During installation, make sure to check:
    echo    ☑ Add Python to PATH
    set /a ERROR_COUNT+=1
)

echo.
echo [6] Checking ITEMS directory...
echo ────────────────────────────────────────────────────
if exist ITEMS (
    echo ✓ ITEMS directory exists

    if exist ITEMS\T1_TRASH.png (
        echo ✓ Test file found (T1_TRASH.png)

        for /f %%A in ('dir /b ITEMS\*.png 2^>nul ^| find /c ".png"') do (
            echo   Total items: %%A images
        )
    ) else (
        echo ! Test file missing (T1_TRASH.png)
        echo   Items directory may be incomplete
    )
) else (
    echo ! ITEMS directory not found
    echo.
    echo 💡 To create it, run:
    echo    python download_missing_items.py
)

echo.
echo [7] Checking download script...
echo ────────────────────────────────────────────────────
if exist download_missing_items.py (
    echo ✓ download_missing_items.py exists
) else (
    echo ✗ download_missing_items.py NOT found
    set /a ERROR_COUNT+=1
)

echo.
echo.
echo ╔═══════════════════════════════════════════════════╗
echo ║   DIAGNOSIS SUMMARY                               ║
echo ╚═══════════════════════════════════════════════════╝
echo.

if %ERROR_COUNT%==0 (
    echo ✅ All checks passed!
    echo.
    echo Your system is ready to use the Assets Updater feature.
    echo.
    echo 📝 Next steps:
    echo    1. Start VRise.exe
    echo    2. Go to Config page
    echo    3. Click "Check for Updates"
    echo.
) else (
    echo ⚠️  Found %ERROR_COUNT% issue(s)
    echo.
    echo 🔧 Required fixes are listed above
    echo.
    echo 📚 For detailed help, see:
    echo    - FIX_WIN32EXCEPTION.md
    echo    - ASSETS_UPDATER_FEATURE.md
    echo.
)

echo Press any key to exit...
pause >nul
