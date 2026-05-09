@echo off
title SIMPE Dashboard
echo =========================================
echo       Iniciando SIMPE Dashboard...
echo =========================================
echo.

cd /d "%~dp0SIMPE.Dashboard"

:: Abrir el navegador por defecto
start http://localhost:5240

:: Ejecutar el proyecto
dotnet run

pause
