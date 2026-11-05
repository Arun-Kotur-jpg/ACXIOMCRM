@echo off
title AcxiomCRM - MERN Stack Enterprise CRM
echo ========================================================
echo       AcxiomCRM - MERN Stack Sales Management
echo       MongoDB 8.3 + Express.js + React + Node.js
echo ========================================================
echo.

echo Launching AcxiomCRM MERN Application...
echo.
echo Open your browser at:
echo   http://localhost:5000
echo.
echo Verified Demo Credentials:
echo   Admin:   admin@acxiomcrm.local   / Admin@123456!
echo   Manager: manager@acxiomcrm.local / Demo@123456!
echo   Sales:   sales1@acxiomcrm.local  / Demo@123456!
echo.
echo (1-Click Quick-Fill buttons are available on the login page)
echo Press Ctrl+C in this terminal to stop the server.
echo ========================================================
echo.

node server/server.js
pause
