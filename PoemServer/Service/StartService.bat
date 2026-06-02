@echo off
:: Request admin privileges
:: This re-launches the script as admin if not already
fltmc >nul 2>&1 || (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo Installing and starting PoemServer services...

:: Define wrapper and config file names
set WRAPPER="%~dp0Instances/WinSW.exe"
set CONFIG1="%~dp0Instances/PoemServer1.xml"
set CONFIG2="%~dp0Instances/PoemServer2.xml"
set CONFIG3="%~dp0Instances/PoemServer3.xml"

:: Install and start PoemServer1
echo Installing PoemServer1...
%WRAPPER% install %CONFIG1%
echo Starting PoemServer1...
net start PoemServer1

:: Install and start PoemServer2
echo Installing PoemServer2...
%WRAPPER% install %CONFIG2%
echo Starting PoemServer2...
net start PoemServer2

:: Install and start PoemServer3
echo Installing PoemServer3...
%WRAPPER% install %CONFIG3%
echo Starting PoemServer3...
net start PoemServer3

echo All PoemServer services installed and started.
pause
