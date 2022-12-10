REM Copy a helpful README
copy /b/v/y localreadme.txt bin\Release\net6.0-windows10.0.17763.0\README.txt

REM Also sign the release
cd bin\Release
..\..\..\signall.bat
