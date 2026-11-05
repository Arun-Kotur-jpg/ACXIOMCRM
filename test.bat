@echo off
title AcxiomCRM - Running Automated Tests...
echo ========================================================
echo       AcxiomCRM - Running Test Suite (xUnit)
echo ========================================================
echo.

set DOTNET_ROOT=C:\tmp\dotnet-sdk
set PATH=C:\tmp\dotnet-sdk;%PATH%

dotnet test AcxiomCRM.Tests\AcxiomCRM.Tests.csproj
echo.
pause

