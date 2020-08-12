@ECHO off
SET CodeSignFile=codesign.pfx
SET /A FindTries=0

REM Recursively search for the pfx file
:RetryFind
IF EXIST %CodeSignFile% GOTO SignAll
SET CodeSignFile=..\%CodeSignFile%

SET /A FindTries=FindTries+1
IF %FindTries% GEQ 10 GOTO Fail
GOTO RetryFind

:SignAll
CALL :Sign
FOR /D %%i IN (%cd%\*) DO (cd "%%i" & CALL :Sign)

ECHO Done
REM Make sure that the batch has an error code of 0
EXIT /b 0

:Fail
ECHO Failed to find pfx
EXIT /b 0

:Sign
REM signtool required in PATH
ECHO Signing in %cd%
signtool.exe sign /f %CodeSignFile% /as /seal /d "Executable of the Chrome Developer Mode Extension Warning Patcher" /du "https://github.com/Ceiridge/Chrome-Developer-Mode-Extension-Warning-Patcher" /tr http://timestamp.globalsign.com/scripts/timstamp.dll *.dll *.exe
GOTO :EOF
