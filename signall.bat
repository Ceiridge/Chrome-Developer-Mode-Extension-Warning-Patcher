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
CALL :NormalizePath %CodeSignFile% 
SET CodeSignFile=%RETVAL%
ECHO Found pfx at: %CodeSignFile%

SET FileLoc=%cd%
CALL :Sign
FOR /D %%i IN (%cd%\*) DO (
	SET FileLoc=%%i
	CALL :Sign
)

ECHO Done
REM Make sure that the batch has an error code of 0
EXIT /b 0

:Fail
ECHO Failed to find pfx
EXIT /b 0

:Sign
REM signtool required in PATH
ECHO Signing in %FileLoc%
START /wait cmd.exe /c %CodeSignFile%\..\sign.bat
REM Another batch file is required to ignore errors somehow
GOTO :EOF

:NormalizePath
SET RETVAL=%~f1
EXIT /B
