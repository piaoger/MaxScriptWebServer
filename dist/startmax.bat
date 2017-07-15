
set THIS_FOLDER_TAIL=%~dp0
set THIS_FOLDER=%THIS_FOLDER_TAIL:~0,-1%

set path=%path%;"C:\Program Files\3ds Max 2014\";"C:\Program Files\Autodesk\3ds Max 2014\";%AUTODESK_3DSMAX_INSTALLDIR%

taskkill /f /im 3dsmax.exe

cd %THIS_FOLDER%
start 3dsmax.exe -U MAXScript runwebserver.ms