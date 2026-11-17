@echo off
cd /d "%~dp0"
dotnet run --project tests/PhotoStudio.Verify -c Release -- evidence-sql
pause
