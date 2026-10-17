@echo off
chcp 65001 >nul
echo =============================================================
echo     KHOI CHAY MICROSOFT EDGE CHE DO DEBUG CDP (PORT 9222)
echo =============================================================
echo.

set "EDGE_EXE="
if exist "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" (
    set "EDGE_EXE=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
) else if exist "C:\Program Files\Microsoft\Edge\Application\msedge.exe" (
    set "EDGE_EXE=C:\Program Files\Microsoft\Edge\Application\msedge.exe"
)

if "%EDGE_EXE%"=="" (
    echo [LOI] Khong tim thay trinh duyet Microsoft Edge tren may tinh!
    pause
    exit /b 1
)

set "DEBUG_DIR=%LOCALAPPDATA%\Microsoft\Edge\User Data Debug"
if not exist "%DEBUG_DIR%" mkdir "%DEBUG_DIR%"

echo [1/3] Dang khoi dong Edge tai cong Remote Debugging 9222...
echo [2/3] Thu muc Profile rieng biet: %DEBUG_DIR%
echo [3/3] Trinh duyet mo trang Shopee Viet Nam de giu phien dang nhap...
echo.

start "" "%EDGE_EXE%" --remote-debugging-port=9222 --remote-allow-origins=* --user-data-dir="%DEBUG_DIR%" https://shopee.vn

echo =============================================================
echo -> MICROSOFT EDGE DEBUG DANG CHAY TAI PORT 9222!
echo -> Ban co the giu cua so nay mo va chay:
echo       dotnet run --project GiftFinder.API --sync-prices
echo    he thong se tu dong cao gia that qua Edge ma khong bi chan WAF!
echo =============================================================
echo.
