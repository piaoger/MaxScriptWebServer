
REM set AUTODESK_3DSMAX_INSTALLDIR
REM set AUTODESK_3DSMAX_START_SILENTLY=true
REM set AUTODESK_3DSMAX_START_SILENTLY=false

set THIS_FOLDER_TAIL=%~dp0
set THIS_FOLDER=%THIS_FOLDER_TAIL:~0,-1%

set path=%path%;"C:\Program Files\3ds Max 2014\";"C:\Program Files\Autodesk\3ds Max 2014\";%AUTODESK_3DSMAX_INSTALLDIR%

taskkill /f /im 3dsmax.exe

REM  Fix MAXScript Script Controller Exception
rd /s /q %USERPROFILE%\AppData\Local\Autodesk\3dsMax
rd /s /q %USERPROFILE%\AppData\Roaming\Autodesk\3DSMAX

if "V%AUTODESK_3DSMAX_START_SILENT%"=="Vfalse" (
    start 3dsmax.exe /Language=ENU  -U MAXScript %THIS_FOLDER%\runwebserver.ms
) else (
    start 3dsmax.exe /Language=ENU -silent -U MAXScript %THIS_FOLDER%\runwebserver.ms
)