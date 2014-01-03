@echo off
if "%1"=="" goto printSyntax
nuget setapikey %1
nuget push SkyDrive.FileWatcher.%2.nupkg

goto end

:printSyntax
echo Usage:
echo publish 4003d786-cc37-4004-bfdf-c4f3e8ef9b3a 0.1

:end
