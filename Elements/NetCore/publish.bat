@echo off
	
    	setlocal enableextensions disabledelayedexpansion
    	set "newDir=..\..\"
    	call :resolve "%newDir%" resolvedDir
	
	set SolutionDir=%resolvedDir%
    	set SolutionName=ZenDotNetCoreComplete
	
	rmdir %SolutionDir%\Implementations /S /Q
	if exist "%SolutionDir%\Implementations" rmdir "%SolutionDir%\Implementations" /S /Q
	
	dotnet build --configuration Release "%cd%\ZenAssetReceiver\ZenAssetReceiver.csproj"
	dotnet build --configuration Release "%cd%\ZenAssetTransmitter\ZenAssetTransmitter.csproj"
	dotnet build --configuration Release "%cd%\ZenCsScript\ZenCsScript.csproj"
	dotnet build --configuration Release "%cd%\ZenDebug\ZenDebug.csproj"
	dotnet build --configuration Release "%cd%\ZenElementActionCaller\ZenElementActionCaller.csproj"
	dotnet build --configuration Release "%cd%\ZenElementsExecuter\ZenElementsExecuter.csproj"
	dotnet build --configuration Release "%cd%\ZenExcelReader\ZenExcelReader.csproj"
	dotnet build --configuration Release "%cd%\ZenGwSystemInfo\ZenGwSystemInfo.csproj"
	dotnet build --configuration Release "%cd%\ZenHttpRequest\ZenHttpRequest.csproj"
	dotnet build --configuration Release "%cd%\ZenLicenceChecker\ZenLicenceChecker.csproj"
	dotnet build --configuration Release "%cd%\ZenMail\ZenMail.csproj"
	dotnet build --configuration Release "%cd%\ZenMySql\ZenMySql.csproj"
	dotnet build --configuration Release "%cd%\ZenSqlServer\ZenSqlServer.csproj"
	dotnet build --configuration Release "%cd%\ZenWebServer\ZenWebServer.csproj"
	
    endlocal
    goto :EOF

:resolve file/folder returnVarName
    set "%~2=%~f1"
    goto :EOF