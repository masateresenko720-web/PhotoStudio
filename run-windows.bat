@echo off
cd /d "%~dp0"
dotnet run --project src/PhotoStudio.Desktop -c Release
pause
