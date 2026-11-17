@echo off
cd /d "%~dp0"
dotnet msbuild PhotoStudio.sln -t:Restore,Build -p:Configuration=Release -m:1
pause
