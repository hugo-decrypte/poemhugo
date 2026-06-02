@echo off

rem Kill all poemserver forcefully
taskkill /f /im poemserver64.exe
taskkill /f /im poem64.exe

rem Exit the script
exit
