@echo off

net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -Command "Start-Process '%~f0' -Verb runAs"
    exit /b
)

start "" /wait regsvr32 /u /s "D:/POEM/PoemClient/PoemView/ComView64.ocx"
start "" /wait regsvr32 /u /s "D:/POEM/PoemClient/PoemView/PoemView64.ocx"

start "" /wait regsvr32 /u /s "D:/POEM/PoemClient/PoemView/ComView32.ocx"
start "" /wait regsvr32 /u /s "D:/POEM/PoemClient/PoemView/PoemView32.ocx"
