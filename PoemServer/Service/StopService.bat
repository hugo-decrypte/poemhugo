@echo off
:: Request admin privileges
fltmc >nul 2>&1 || (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo Stopping and uninstalling PoemServer services...

:: Define wrapper and config file paths
set WRAPPER="%~dp0Instances/WinSW.exe"
set CONFIG1="%~dp0Instances/PoemServer1.xml"
set CONFIG2="%~dp0Instances/PoemServer2.xml"
set CONFIG3="%~dp0Instances/PoemServer3.xml"

:: Stop and uninstall PoemServer1
echo Stopping PoemServer1...
net stop PoemServer1 >nul 2>&1
echo Uninstalling PoemServer1...
%WRAPPER% uninstall %CONFIG1%

:: Stop and uninstall PoemServer2
echo Stopping PoemServer2...
net stop PoemServer2 >nul 2>&1
echo Uninstalling PoemServer2...
%WRAPPER% uninstall %CONFIG2%

:: Stop and uninstall PoemServer3
echo Stopping PoemServer3...
net stop PoemServer3 >nul 2>&1
echo Uninstalling PoemServer3...
%WRAPPER% uninstall %CONFIG3%

echo All PoemServer services stopped and uninstalled.
pause
