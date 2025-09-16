@echo off
echo ===========================================
echo TradingGUI Deployment Script
echo ===========================================
echo Publishing TradingGUI project (appsettings.local.json will NOT be copied)...
echo.
echo Starting deployment process...
setlocal enabledelayedexpansion

:: Determine PowerShell executable
echo Checking for PowerShell...
set "POWERSHELL_EXE=powershell.exe"
where pwsh.exe >nul 2>nul
if %ERRORLEVEL% == 0 (
    set "POWERSHELL_EXE=pwsh.exe"
)
echo INFO: Using PowerShell: %POWERSHELL_EXE%
echo.

:: Configuration
set "PROJECT_NAME=TradingGUI"
set "PROJECT_PATH=TradingGUI\TradingGUI.csproj"
set "PROJECT_DIR=TradingGUI"
set "OUTPUT_PATH=C:\Deploy\TradingGUI"
set "TARGET_FRAMEWORK=net8.0"
set "ZIP_BASENAME=TradingGUI"
set "THIS_SCRIPT_NAME=%~nx0"
set "NETWORK_ZIP_PATH=\\DESKTOP-ITC50UT\SmokehouseCandlestickImport"
set "FALLBACK_FOLDER=%OUTPUT_PATH%"

:: Create timestamp for backup
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set TIMESTAMP=%datetime:~0,8%_%datetime:~8,6%

echo Deployment started at %TIMESTAMP%
echo.

:: Check if project file exists
echo Checking if project file exists: %PROJECT_PATH%
if not exist "%PROJECT_PATH%" (
    echo ERROR: Project file not found at %PROJECT_PATH%
    echo.
    echo Press any key to continue...
    pause >nul
    exit /b 1
)
echo Project file found.
echo.

:: Clean build artifacts only
echo Cleaning build artifacts...
rmdir /s /q "TradingGUI\bin" 2>nul
rmdir /s /q "TradingGUI\obj" 2>nul
echo Build artifacts cleaned.
echo.

:: Ensure output path exists
echo Checking output directory: %OUTPUT_PATH%
if not exist "%OUTPUT_PATH%" (
    echo INFO: Creating output directory: %OUTPUT_PATH%
    mkdir "%OUTPUT_PATH%"
    if errorlevel 1 (
        echo ERROR: Failed to create output directory
        echo.
        echo Press any key to continue...
        pause >nul
        exit /b 1
    )
)
echo Output directory ready.
echo.

echo.
echo ===========================================
echo Building and Publishing %PROJECT_NAME%
echo ===========================================

:: Clean project
echo Cleaning project...
dotnet clean "%PROJECT_PATH%" -c Release
if errorlevel 1 (
    echo ERROR: dotnet clean failed!
    echo.
    echo Press any key to continue...
    pause >nul
    exit /b 1
)
echo Project cleaned successfully.
echo.

:: Restore packages
echo Restoring NuGet packages...
dotnet restore "%PROJECT_PATH%"
if errorlevel 1 (
    echo ERROR: dotnet restore failed!
    echo.
    echo Press any key to continue...
    pause >nul
    exit /b 1
)
echo Packages restored successfully.
echo.

:: Build project
echo Building project in Release mode...
dotnet build "%PROJECT_PATH%" -c Release
if errorlevel 1 (
    echo ERROR: Build failed!
    echo.
    echo Press any key to continue...
    pause >nul
    exit /b 1
)
echo Build completed successfully.
echo.

:: Publish with verbose logging
echo Publishing project...
dotnet publish "%PROJECT_PATH%" -c Release -o "%OUTPUT_PATH%" -f "%TARGET_FRAMEWORK%" --no-self-contained --no-restore --verbosity detailed > "%OUTPUT_PATH%\publish.log"
if errorlevel 1 (
    echo ERROR: Publish failed. Check %OUTPUT_PATH%\publish.log.
    echo.
    echo Press any key to continue...
    pause >nul
    exit /b 1
)
echo Publish completed successfully.
echo.

:: Determine zip destination path (UNC-safe, no parens)
echo Determining zip destination...
set "ZIP_DEST_PATH=%OUTPUT_PATH%"

echo Checking network path: %NETWORK_ZIP_PATH%
pushd "%NETWORK_ZIP_PATH%" >nul 2>&1
if errorlevel 1 goto NoShare

:: success
set "ZIP_DEST_PATH=%NETWORK_ZIP_PATH%"
echo INFO: Network path available, using: %NETWORK_ZIP_PATH%
popd >nul 2>&1
goto AfterShare

:NoShare
echo WARNING: Network path not available (%NETWORK_ZIP_PATH%), using local path: %OUTPUT_PATH%
popd >nul 2>&1

:AfterShare
echo.

:: Create zip file with timestamp (excluding appsettings.json, appsettings.local.json, appsettings.local.backup, etc.)
echo Creating zip file...

:: Create a temporary PowerShell script for better reliability
set "TEMP_PS_SCRIPT=%TEMP%\deploy_zip_%TIMESTAMP%.ps1"
echo $ErrorActionPreference = 'Stop' > "%TEMP_PS_SCRIPT%"
echo $outputPath = '%PROJECT_DIR%' >> "%TEMP_PS_SCRIPT%"
echo $zipDestPath = '%FALLBACK_FOLDER%' >> "%TEMP_PS_SCRIPT%"
echo $finalPath = '%ZIP_DEST_PATH%' >> "%TEMP_PS_SCRIPT%"
echo $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss' >> "%TEMP_PS_SCRIPT%"
echo $zipBaseName = '%ZIP_BASENAME%' >> "%TEMP_PS_SCRIPT%"
echo $thisScriptName = '%THIS_SCRIPT_NAME%' >> "%TEMP_PS_SCRIPT%"
echo $zipName = $zipBaseName + '_' + $timestamp + '.zip' >> "%TEMP_PS_SCRIPT%"
echo $itemsToZip = Get-ChildItem -Path $outputPath -Exclude 'bin','obj','.vs','*.user','*.suo','*.tmp','*.log','publish.log','appsettings.local.json','appsettings.local.backup','Logs',$thisScriptName >> "%TEMP_PS_SCRIPT%"
echo if ($itemsToZip) { >> "%TEMP_PS_SCRIPT%"
echo     try { >> "%TEMP_PS_SCRIPT%"
echo         Compress-Archive -Path $itemsToZip.FullName -DestinationPath $destPath -Force >> "%TEMP_PS_SCRIPT%"
echo         Write-Host "Created zip locally: $destPath" >> "%TEMP_PS_SCRIPT%"
echo         if ($zipDestPath -ne $finalPath) { >> "%TEMP_PS_SCRIPT%"
echo             $finalDestPath = Join-Path -Path $finalPath -ChildPath $zipName >> "%TEMP_PS_SCRIPT%"
echo             Move-Item -Path $destPath -Destination $finalDestPath -Force >> "%TEMP_PS_SCRIPT%"
echo             Write-Host "Moved zip to: $finalDestPath" >> "%TEMP_PS_SCRIPT%"
echo             Remove-Item $destPath -Force >> "%TEMP_PS_SCRIPT%"
echo             Write-Host "Cleaned up local zip: $destPath" >> "%TEMP_PS_SCRIPT%"
echo         } >> "%TEMP_PS_SCRIPT%"
echo     } catch { >> "%TEMP_PS_SCRIPT%"
echo         Write-Host "ERROR (Zipping): $($_.Exception.Message)" >> "%TEMP_PS_SCRIPT%"
echo         exit 1 >> "%TEMP_PS_SCRIPT%"
echo     } >> "%TEMP_PS_SCRIPT%"
echo } else { >> "%TEMP_PS_SCRIPT%"
echo     Write-Warning 'No items to zip after exclusions' >> "%TEMP_PS_SCRIPT%"
echo } >> "%TEMP_PS_SCRIPT%"

%POWERSHELL_EXE% -NoProfile -ExecutionPolicy Bypass -File "%TEMP_PS_SCRIPT%"
if errorlevel 1 (
    echo ERROR: Zip creation failed. Check PowerShell output.
    del "%TEMP_PS_SCRIPT%" 2>nul
    echo.
    echo Press any key to continue...
    pause >nul
    exit /b 1
)

:: Clean up temporary script
del "%TEMP_PS_SCRIPT%" 2>nul
echo Zip creation completed successfully.
echo.

:: Create a simple run script
echo Creating run script...
echo @echo off > "%OUTPUT_PATH%\run.bat"
echo echo Starting %PROJECT_NAME%... >> "%OUTPUT_PATH%\run.bat"
echo %PROJECT_NAME%.exe >> "%OUTPUT_PATH%\run.bat"
echo pause >> "%OUTPUT_PATH%\run.bat"
echo Run script created in deployment folder.
echo.

echo ===========================================
echo Deployment completed successfully!
echo ===========================================
echo.
echo Deployment Summary:
echo - Project: %PROJECT_NAME%
echo - Output: %OUTPUT_PATH%
echo - Zip destination: %ZIP_DEST_PATH%
echo.
echo Press any key to exit...
pause >nul

endlocal