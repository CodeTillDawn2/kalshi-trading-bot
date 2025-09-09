@echo off
echo Publishing BacklashBot project (appsettings.local.json will NOT be copied)...
setlocal

:: Determine PowerShell executable
set "POWERSHELL_EXE=powershell.exe"
where pwsh.exe >nul 2>nul
if %ERRORLEVEL% == 0 (
    set "POWERSHELL_EXE=pwsh.exe"
)
echo INFO: Using PowerShell: %POWERSHELL_EXE%

:: Configuration
set "PROJECT_PATH=C:\Users\Peter\Documents\GitHub\kalshi-trading-bot\BacklashBot\BacklashBot.csproj"
set "OUTPUT_PATH=C:\Deploy\BacklashBot"
set "TARGET_FRAMEWORK=net8.0"
set "ZIP_BASENAME=BacklashBot"
set "THIS_SCRIPT_NAME=%~nx0"

:: Clean build artifacts only
echo Cleaning build artifacts...
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul

:: Ensure output path exists
if not exist "%OUTPUT_PATH%" (
    echo INFO: Creating output directory: %OUTPUT_PATH%
    mkdir "%OUTPUT_PATH%"
)

:: Clean project
echo Cleaning project...
dotnet clean "%PROJECT_PATH%" -c Release

:: Build project
echo Building project...
dotnet build "%PROJECT_PATH%" -c Release

:: Publish with verbose logging
echo Publishing project...
dotnet publish "%PROJECT_PATH%" -c Release -o "%OUTPUT_PATH%" -f "%TARGET_FRAMEWORK%" --no-self-contained --no-restore --verbosity detailed > publish.log
if errorlevel 1 (
    echo ERROR: dotnet publish failed. Check publish.log.
    goto :EndScript
)

:: Create zip file with timestamp (excluding appsettings.json, appsettings.local.json, appsettings.local.backup, etc.)
echo Creating zip file...
set "PS_ZIP_COMMAND=$ErrorActionPreference='Stop';$SoutputPath='%OUTPUT_PATH%';$Stimestamp=Get-Date -Format 'yyyyMMdd_HHmmss';$SzipBaseName='%ZIP_BASENAME%';$SthisScriptName='%THIS_SCRIPT_NAME%';$SzipName=$SzipBaseName+'_'+$Stimestamp+'.zip';$SdestPath=Join-Path -Path $SoutputPath -ChildPath $SzipName;$SitemsToZip=Get-ChildItem -Path $SoutputPath -Exclude 'appsettings.json','appsettings.local.json','appsettings.local.backup','*.zip','Logs','publish.log',$SthisScriptName;if($SitemsToZip){try{Compress-Archive -Path $SitemsToZip.FullName -DestinationPath $SdestPath -Force;Write-Host 'Created zip: '+$SdestPath}catch{Write-Host 'ERROR (Zipping): '+$_.Exception.Message;exit 1}}else{Write-Warning 'No items to zip after exclusions'}"

%POWERSHELL_EXE% -NoProfile -ExecutionPolicy Bypass -Command "%PS_ZIP_COMMAND%"
if errorlevel 1 (
    echo ERROR: Zip creation failed. Check PowerShell output.
    goto :EndScript
)
echo Zip creation completed.

:EndScript
echo Script finished.
endlocal
pause