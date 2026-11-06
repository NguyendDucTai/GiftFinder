@echo off
chcp 65001 >nul
cd /d "%~dp0"
if exist "backend\GiftFinder.API" cd /d "%~dp0backend"

title Dong bo tat ca san pham TikTok Shop - GiftFinder
echo =============================================================
echo    DONG BO TOAN BO SAN PHAM TIKTOK SHOP [GIA, SAO, DANH GIA]
echo =============================================================
echo.

curl.exe -s http://127.0.0.1:9222/json/version >nul 2>&1
if %errorlevel% neq 0 (
    echo [THONG BAO] Microsoft Edge Debug cong 9222 chua mo.
    echo Dang khoi dong Microsoft Edge Debug...
    call start_edge_debug.bat
    timeout /t 4 /nobreak >nul
)

curl.exe -s http://127.0.0.1:9222/json/version >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo [CANH BAO] Khong the ket noi toi Edge tai cong 9222!
    echo Vui long chay file start_edge_debug.bat truoc roi thu lai.
    echo.
    pause
    exit /b 1
)

echo [OK] Da ket noi thanh cong toi Microsoft Edge Debug tai cong 9222!
echo.
echo [*] Dang quet va dong bo toan bo san pham TikTok Shop trong Database Local...
echo [*] Neu TikTok bat keo xac minh Security Check, hay keo tren cua so Edge nhe.
echo.

dotnet run --project GiftFinder.API --no-build --sync-prices --source TikTokShop

echo.
echo =============================================================
echo [*] DONG BO HOAN TAT! Nhan phim bat ky de thoat...
echo =============================================================
pause
