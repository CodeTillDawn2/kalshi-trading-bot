@echo off
echo ===========================================
echo KalshiBotOverseer Deployment Script
echo ===========================================
echo Publishing KalshiBotOverseer project (appsettings.local.json will NOT be copied)...
setlocal

REM Determine PowerShell executable
set "POWERSHELL_EXE=powershell.exe"
where pwsh.exe >nul 2>nul
if %ERRORLEVEL% == 0 (
    set "POWERSHELL_EXE=pwsh.exe"
)
echo INFO: Using PowerShell: %POWERSHELL_EXE%

REM Set variables
set PROJECT_NAME=KalshiBotOverseer
set PROJECT_PATH=KalshiBotOverseer\KalshiBotOverseer.csproj
set DEPLOY_FOLDER=C:\Deploy\Overseer
set PUBLISH_PROFILE=Release
set TARGET_FRAMEWORK=net8.0
set ZIP_BASENAME=KalshiBotOverseer
set THIS_SCRIPT_NAME=%~nx0

REM Create timestamp for backup
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set TIMESTAMP=%datetime:~0,8%_%datetime:~8,6%

echo Deployment started at %TIMESTAMP%
echo.

REM Check if project file exists
if not exist "%PROJECT_PATH%" (
    echo ERROR: Project file not found at %PROJECT_PATH%
    pause
    exit /b 1
)

REM Create deployment directory if it doesn't exist
if not exist "%DEPLOY_FOLDER%" (
    echo Creating deployment directory: %DEPLOY_FOLDER%
    mkdir "%DEPLOY_FOLDER%"
)

REM Backup existing deployment if it exists
if exist "%DEPLOY_FOLDER%\*" (
    echo Backing up existing deployment...
    if not exist "%DEPLOY_FOLDER%_backup" mkdir "%DEPLOY_FOLDER%_backup"
    move "%DEPLOY_FOLDER%" "%DEPLOY_FOLDER%_backup\%TIMESTAMP%" 2>nul
    mkdir "%DEPLOY_FOLDER%"
)

echo.
echo ===========================================
echo Building and Publishing %PROJECT_NAME%
echo ===========================================

REM Clean build artifacts only
echo Cleaning build artifacts...
rmdir /s /q "%PROJECT_PATH%\..\bin" 2>nul
rmdir /s /q "%PROJECT_PATH%\..\obj" 2>nul

REM Clean the project
echo Cleaning project...
dotnet clean "%PROJECT_PATH%" -c %PUBLISH_PROFILE%

REM Restore packages
echo Restoring NuGet packages...
dotnet restore "%PROJECT_PATH%"

REM Build the project
echo Building project in Release mode...
dotnet build "%PROJECT_PATH%" -c %PUBLISH_PROFILE%

if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

REM Publish the project with verbose logging
echo Publishing project...
dotnet publish "%PROJECT_PATH%" -c %PUBLISH_PROFILE% -o "%DEPLOY_FOLDER%" -f "%TARGET_FRAMEWORK%" --no-self-contained --no-restore --verbosity detailed > "%DEPLOY_FOLDER%\publish.log"

if %ERRORLEVEL% neq 0 (
    echo ERROR: Publish failed. Check %DEPLOY_FOLDER%\publish.log.
    goto :EndScript
)

REM Create zip file with timestamp (excluding appsettings.json, appsettings.local.json, appsettings.local.backup, etc.)
echo Creating zip file...
set "PS_ZIP_COMMAND=$ErrorActionPreference='Stop';$SoutputPath='%DEPLOY_FOLDER%';$Stimestamp=Get-Date -Format 'yyyyMMdd_HHmmss';$SzipBaseName='%ZIP_BASENAME%';$SthisScriptName='%THIS_SCRIPT_NAME%';$SzipName=$SzipBaseName+'_'+$Stimestamp+'.zip';$SdestPath=Join-Path -Path $SoutputPath -ChildPath $SzipName;$SitemsToZip=Get-ChildItem -Path $SoutputPath -Exclude 'appsettings.json','appsettings.local.json','appsettings.local.backup','*.zip','Logs','publish.log',$SthisScriptName;if($SitemsToZip){try{Compress-Archive -Path $SitemsToZip.FullName -DestinationPath $SdestPath -Force;Write-Host 'Created zip: '+$SdestPath}catch{Write-Host 'ERROR (Zipping): '+$_.Exception.Message;exit 1}}else{Write-Warning 'No items to zip after exclusions'}"

%POWERSHELL_EXE% -NoProfile -ExecutionPolicy Bypass -Command "%PS_ZIP_COMMAND%"
if errorlevel 1 (
    echo ERROR: Zip creation failed. Check PowerShell output.
    goto :EndScript
)
echo Zip creation completed.

REM Create a simple run script
echo @echo off > "%DEPLOY_FOLDER%\run.bat"
echo echo Starting %PROJECT_NAME%... >> "%DEPLOY_FOLDER%\run.bat"
echo %PROJECT_NAME%.exe >> "%DEPLOY_FOLDER%\run.bat"
echo pause >> "%DEPLOY_FOLDER%\run.bat"

echo Created run.bat script in deployment folder.

:EndScript
echo Script finished.
endlocal
pause