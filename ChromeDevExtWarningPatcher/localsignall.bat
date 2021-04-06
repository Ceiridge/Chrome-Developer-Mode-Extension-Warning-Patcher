REM Copy a helpful README
copy /b/v/y localreadme.txt bin\Release\net5.0-windows\README.txt

REM Also sign the release
cd bin\Release
..\..\..\signall.bat
