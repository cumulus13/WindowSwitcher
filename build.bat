@echo off

for /f "tokens=* delims=" %%a in ('date /t') do set current_date=%%a
for /f "tokens=* delims=" %%b in ('time /t') do set current_time=%%b

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Build successful: WindowSwitcher.exe
    sendgrowl.exe "Dotnet" build "SUCCESS %current_date% %current_time%" "[%current_date% %current_time%] ✅ Build successful: WindowSwitcher.exe" -i "c:\TOOLS\EXE\dotnet.png" -s -H 127.0.0.1
    echo.
    copy /y "c:\PROJECTS\dotnet\WindowSwitcher\bin\Release\net8.0-windows\win-x64\publish\WindowSwitcher.exe" "c:\PROJECTS\dotnet\WindowSwitcher\WindowSwitcher.exe"

) else (
    echo.
    echo ❌ Build failed!
    sendgrowl.exe "GoBuilder" build "FAILED %current_date% %current_time%" "[%current_date% %current_time%] ❌ Build failed!: WindowSwitcher.exe" -i "c:\TOOLS\EXE\dotnet.png" -s -H 127.0.0.1
    exit /b 1
)

