@echo off
:: Request admin privileges
:: Relaunch as admin if needed
fltmc >nul 2>&1 || (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

:: Go to script directory
cd /d "%~dp0"

:: Define wrapper and config paths
set WRAPPER="%~dp0Instances\WinSW.exe"
set CONFIG1="%~dp0Instances\PoemServer1.xml"
set CONFIG2="%~dp0Instances\PoemServer2.xml"
set CONFIG3="%~dp0Instances\PoemServer3.xml"

:: Define service names
set SERVICE1=PoemServer1
set SERVICE2=PoemServer2
set SERVICE3=PoemServer3

:MENU
cls
echo ========================================
echo         PoemServer Service Manager
echo ========================================
echo.
echo   1. Install and Start All Services
echo   2. Stop and Uninstall All Services
echo   3. Start All Services
echo   4. Stop All Services
echo   5. Status All Services
echo   6. Exit
echo.
set /p choice=Select an option [1-6]: 

if "%choice%"=="1" goto INSTALL_START
if "%choice%"=="2" goto STOP_UNINSTALL
if "%choice%"=="3" goto START
if "%choice%"=="4" goto STOP
if "%choice%"=="5" goto STATUS
if "%choice%"=="6" goto END

echo Invalid selection. Please try again.
pause
goto MENU

:INSTALL_START
echo Installing and starting all PoemServer services...
%WRAPPER% install %CONFIG1%
%WRAPPER% install %CONFIG2%
%WRAPPER% install %CONFIG3%

echo Starting services...
net start %SERVICE1%
net start %SERVICE2%
net start %SERVICE3%

echo All services installed and started successfully.
pause
goto MENU

:STOP_UNINSTALL
echo Stopping services...
net stop %SERVICE1%
net stop %SERVICE2%
net stop %SERVICE3%

echo Uninstalling services...
%WRAPPER% uninstall %CONFIG1%
%WRAPPER% uninstall %CONFIG2%
%WRAPPER% uninstall %CONFIG3%

echo All services stopped and uninstalled successfully.
pause
goto MENU

:START
echo Starting all services...
net start %SERVICE1%
net start %SERVICE2%
net start %SERVICE3%

echo All services started successfully.
pause
goto MENU

:STOP
echo Stopping all services...
net stop %SERVICE1%
net stop %SERVICE2%
net stop %SERVICE3%

echo All services stopped successfully.
pause
goto MENU

:STATUS
echo Status:
%WRAPPER% status %CONFIG1%
%WRAPPER% status %CONFIG2%
%WRAPPER% status %CONFIG3%
pause
goto MENU

:END
echo Exiting.
exit /b
