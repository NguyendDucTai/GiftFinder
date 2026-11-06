@echo off
chcp 65001 >nul
echo =============================================================
echo    NHAP SIEU TOC SAN PHAM TIKTOK SHOP VAO DATABASE
echo =============================================================
echo.

curl.exe -s http://127.0.0.1:9222/json/version >nul 2>&1
if %errorlevel% neq 0 (
    echo [THONG BAO] Microsoft Edge Debug chua mo. Dang tu dong khoi dong Edge...
    start "" cmd /c start_edge_debug.bat
    timeout /t 4 /nobreak >nul
) else (
    echo [OK] Da ket noi Microsoft Edge Debug tai cong 9222!
)

echo.
echo [*] Dang nap san pham tu file tiktokshop_links.txt...
dotnet run --project GiftFinder.API --import-file tiktokshop_links.txt
echo.
echo =============================================================
echo [*] HOAN TAT! Nhan phim bat ky de thoat...
echo =============================================================
pause >nul
