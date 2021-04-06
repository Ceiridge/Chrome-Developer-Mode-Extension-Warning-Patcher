@echo off
signtool.exe sign /f %CodeSignFile% /as /seal /d "Executable of the Chrome Developer Mode Extension Warning Patcher" /du "https://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher" /tr http://freetsa.org/tsr %FileLoc%\*.dll %FileLoc%\*.exe
exit /b 0
