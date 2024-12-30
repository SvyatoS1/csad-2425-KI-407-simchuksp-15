@echo off
REM Exit on error
setlocal enabledelayedexpansion

set ArduinoProjectPath=..\test_server
set Board=arduino:avr:uno
set requiredCore=arduino:avr

REM Check if arduino-cli is installed
where arduino-cli >nul 2>&1
if %errorlevel% neq 0 (
    echo arduino-cli is not installed. Exiting...
    exit /b
)

echo arduino-cli checked.

REM Check for available COM port
for /f "tokens=1,2 delims= " %%i in ('arduino-cli board list') do (
    echo %%i | findstr "COM[0-9]" >nul
    if !errorlevel! equ 0 (
        set comPortNumber=%%i
        echo Found device on port: !comPortNumber!
        goto COM_GOOD
    )
)

echo Connected board not found.
exit /b

:COM_GOOD
REM Check if the required core is installed
for /f "tokens=*" %%i in ('arduino-cli core list') do (
    set output=%%i
    echo !output! | findstr /c:"%requiredCore%" >nul
    if !errorlevel! equ 0 (
	echo %requiredCore% core checked.
        goto BUILD
    )
)

echo Core %requiredCore% is NOT installed. Installing it now...
arduino-cli config init
arduino-cli core update-index
arduino-cli core install %requiredCore%

:BUILD
echo Building Tests...
cd %ArduinoProjectPath%
arduino-cli compile --fqbn %Board% .

echo Uploading bytecode to the board on port %comPortNumber%...
arduino-cli upload -p %comPortNumber% --fqbn %Board% .

cd ..\ci

python --version >nul 2>&1
if errorlevel 1 (
    echo Python is not installed or not in PATH. Please install Python and try again.
    exit /b
)

pip --version >nul 2>&1
if errorlevel 1 (
    echo pip is not installed. Installing pip...
    python -m ensurepip --upgrade
    if errorlevel 1 (
        echo Failed to install pip. Please install pip and try again.
        exit /b
    )
)

REM Install python dependencies and read serial output from board by python script.
pip install -r pyRequirements.txt
python readSerial.py %comPortNumber%
echo Test process completed successfully.
endlocal
PAUSE
