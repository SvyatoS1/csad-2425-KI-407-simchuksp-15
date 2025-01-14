@echo off
setlocal enabledelayedexpansion
set ArduinoUNOProjectPath=..\server
set Board=arduino:avr:uno
set requiredCore=arduino:avr

where arduino-cli >nul 2>&1 || (
    echo arduino-cli is not installed. Exiting...
    PAUSE
    exit /b 1
)

:: Get list of connected boards with debug output
echo Detecting Arduino boards...
echo.
echo Available ports and boards:
arduino-cli board list
echo.

:: Try to find Arduino Uno port with simpler parsing
set "comPort="
for /f "tokens=1,5,6" %%a in ('arduino-cli board list ^| findstr "arduino:avr:uno"') do (
    set "comPort=%%a"
    echo Found Arduino Uno on port !comPort!
    goto CONTINUE
)

:: If we haven't found it yet, try a different approach
if not defined comPort (
    for /f "tokens=1" %%a in ('arduino-cli board list ^| findstr "Arduino Uno"') do (
        set "comPort=%%a"
        echo Found Arduino Uno on port !comPort!
        goto CONTINUE
    )
)

if not defined comPort (
    echo No Arduino Uno found. Exiting...
    exit /b 1
)

:CONTINUE
echo.
echo Using port: %comPort%
echo.

:: Check for required core
for /f "tokens=*" %%i in ('arduino-cli core list') do (
    echo %%i | findstr /c:"%requiredCore%" >nul && (
        echo %requiredCore% core checked.
        goto BUILD
    )
)

echo Core %requiredCore% is NOT installed. Installing it now...
arduino-cli config init
arduino-cli config set board_manager.additional_urls "https://downloads.arduino.cc/packages/package_index.json"
arduino-cli core update-index
arduino-cli core install %requiredCore%

:BUILD
cd %ArduinoUNOProjectPath%
arduino-cli compile --fqbn %Board% .
if errorlevel 1 goto ERROR
arduino-cli upload -p %comPort% --fqbn %Board% .
if errorlevel 1 goto ERROR
cd ..\ci
echo Server build process completed successfully.
goto END

:ERROR
echo Build process failed.
exit /b 1

:END
endlocal
PAUSE