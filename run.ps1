Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "      AcxiomCRM - MERN Stack Sales Management" -ForegroundColor Cyan
Write-Host "      MongoDB 8.3 + Express.js + React + Node.js" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Launching AcxiomCRM MERN Web Server on port 5000..." -ForegroundColor Green
Write-Host "Open your browser at: http://localhost:5000" -ForegroundColor Green
Write-Host ""
Write-Host "Verified Demo Credentials:" -ForegroundColor Yellow
Write-Host "  Admin:   admin@acxiomcrm.local   / Admin@123456!" -ForegroundColor Yellow
Write-Host "  Manager: manager@acxiomcrm.local / Demo@123456!" -ForegroundColor Yellow
Write-Host "  Sales:   sales1@acxiomcrm.local  / Demo@123456!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C in this terminal to stop the server." -ForegroundColor DarkGray
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

node server/server.js
