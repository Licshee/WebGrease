@echo off
echo This batch file copies the last built assemblies to the TmxSdk Project.
echo expected to be called like: $(SolutionDir)TmsSdkAfterBuildTask.bat "$(SolutionDir)" "$(TargetDir)" "$(SolutionDir)..\..\..\Workspace2\MSNMetro\Main\MetroSDK\" 1.4.0
echo (WebGreaseSolutionDir, WebGreaseTargetDir, TmxSdkSolutionPath, CurrentWebGreaseVersionInTmxSdk)
echo (%1, %2, %3, %4)
echo --------------------------------------------------

set WEBGREASEROOT=%1
set BINDIR=%2
set TARGETPROJECTROOT=%3
set VERSION=%4
set PACKAGEROOT=%TARGETPROJECTROOT%\packages

cd %1

echo Deleting WGTemp folders

if exist "%TARGETPROJECTROOT%\MetroSDK\WGTemp"  rmdir /s /q "%TARGETPROJECTROOT%\MetroSDK\WGTemp"
if exist "%TARGETPROJECTROOT%\MetroSDKApp\WGTemp" rmdir /s /q "%TARGETPROJECTROOT%\MetroSDKApp\WGTemp"

if exist "%TARGETPROJECTROOT%\MetroSDK\WGTemp" exit 1
if exist "%TARGETPROJECTROOT%\MetroSDKApp\WGTemp" exit 1

echo Copying assemblies from "%BINDIR%" to packages "%PACKAGEROOT%"

call checkout.bat "%PACKAGEROOT%\WebGrease.%VERSION%\lib\WebGrease.dll"
xcopy  "%BINDIR%\WebGrease.dll"       "%PACKAGEROOT%\WebGrease.%VERSION%\lib" /Y /F

call checkout.bat "%PACKAGEROOT%\WebGrease.%VERSION%\lib\Newtonsoft.Json.dll"
xcopy  "%BINDIR%\Newtonsoft.Json.dll"       "%PACKAGEROOT%\WebGrease.%VERSION%\lib" /Y /F

call checkout.bat "%PACKAGEROOT%\WebGrease.%VERSION%\lib\Antlr3.Runtime.dll"
xcopy  "%BINDIR%\Antlr3.Runtime.dll"       "%PACKAGEROOT%\WebGrease.%VERSION%\lib" /Y /F

call checkout.bat "%PACKAGEROOT%\WebGrease.Build.%VERSION%\tools\WebGrease.Build.dll"
xcopy  "%BINDIR%\WebGrease.Build.dll" "%PACKAGEROOT%\WebGrease.Build.%VERSION%\tools" /Y /F
	
call checkout.bat "%PACKAGEROOT%\WebGrease.Preprocessing.Include.%VERSION%\tools\WebGrease.Preprocessing.Include.dll"
xcopy  "%BINDIR%\WebGrease.Preprocessing.Include.dll" "%PACKAGEROOT%\WebGrease.Preprocessing.Include.%VERSION%\tools" /Y /F
call checkout.bat "%PACKAGEROOT%\WebGrease.Plugins\WebGrease.Preprocessing.Include.dll"
xcopy  "%BINDIR%\WebGrease.Preprocessing.Include.dll" "%PACKAGEROOT%\WebGrease.Plugins" /Y /F

call checkout.bat "%PACKAGEROOT%\WebGrease.Preprocessing.Sass.%VERSION%\tools\WebGrease.Preprocessing.Sass.dll"
xcopy  "%BINDIR%\WebGrease.Preprocessing.Sass.dll" "%PACKAGEROOT%\WebGrease.Preprocessing.Sass.%VERSION%\tools" /Y /F
call checkout.bat "%PACKAGEROOT%\WebGrease.Plugins\WebGrease.Preprocessing.Sass.dll"
xcopy  "%BINDIR%\WebGrease.Preprocessing.Sass.dll" "%PACKAGEROOT%\WebGrease.Plugins" /Y /F