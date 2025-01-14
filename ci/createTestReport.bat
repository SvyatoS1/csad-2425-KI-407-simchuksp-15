@echo off
setlocal enabledelayedexpansion

:: Configuration
set "ArduinoProjectPath=..\test_server"
set "Board=arduino:avr:uno"
set "RequiredCore=arduino:avr"

echo Starting test report generation...
echo.

:: Check prerequisites
call :check_prerequisites
if !errorlevel! neq 0 exit /b 1

:: Find COM port
call :find_com_port
if !errorlevel! neq 0 exit /b 1

:: Build and upload
call :build_and_upload
if !errorlevel! neq 0 exit /b 1

:: Run tests
call :run_tests
if !errorlevel! neq 0 exit /b 1

echo.
echo Test process completed successfully!
echo Press any key to exit...
pause >nul
exit /b 0

:check_prerequisites
echo Checking prerequisites...

:: Check for arduino-cli
where arduino-cli >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: arduino-cli is not installed
    echo Please install arduino-cli and try again.
    pause
    exit /b 1
)

:: Check Python installation
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python and add it to your PATH.
    pause
    exit /b 1
)

:: Check pip installation
pip --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Installing pip...
    python -m ensurepip --upgrade
    if %errorlevel% neq 0 (
        echo ERROR: Failed to install pip
        echo Failed to install pip. Please install it manually.
        pause
        exit /b 1
    )
)

:: Check required Arduino core
for /f "tokens=*" %%i in ('arduino-cli core list') do (
    echo %%i | findstr /c:"%RequiredCore%" >nul
    if !errorlevel! equ 0 (
        echo %RequiredCore% core is installed
        exit /b 0
    )
)

:: Install required core if not found
echo Installing %RequiredCore% core...
echo This may take a few minutes...
arduino-cli config init
arduino-cli core update-index
arduino-cli core install %RequiredCore%
if %errorlevel% neq 0 (
    echo ERROR: Failed to install Arduino core
    echo Failed to install Arduino core. Please try again or install manually.
    pause
    exit /b 1
)

exit /b 0

:find_com_port
echo Searching for Arduino board...

set "comPortFound=false"
for /f "tokens=1,2 delims= " %%i in ('arduino-cli board list') do (
    echo %%i | findstr "COM[0-9]" >nul
    if !errorlevel! equ 0 (
        set "comPortNumber=%%i"
        set "comPortFound=true"
        echo Found device on port: !comPortNumber!
        exit /b 0
    )
)

if "%comPortFound%"=="false" (
    echo ERROR: No Arduino board found
    echo Please connect your Arduino board and try again.
    pause
    exit /b 1
)

exit /b 0

:build_and_upload
echo Building project...
cd "%ArduinoProjectPath%"

:: Clean build directory
if exist "build" rd /s /q "build"

:: Compile sketch
echo Compiling sketch...
arduino-cli compile --fqbn %Board% .
if %errorlevel% neq 0 (
    echo ERROR: Compilation failed
    cd ..\ci
    echo Check the error messages above.
    pause
    exit /b 1
)

echo Uploading to board...
arduino-cli upload -p %comPortNumber% --fqbn %Board% .
if %errorlevel% neq 0 (
    echo ERROR: Upload failed
    cd ..\ci
    echo Make sure the board is properly connected.
    pause
    exit /b 1
)

cd ..\ci
exit /b 0

:run_tests
echo Installing Python dependencies...
pip install -r pyRequirements.txt
if %errorlevel% neq 0 (
    echo ERROR: Failed to install Python dependencies
    pause
    exit /b 1
)

echo Running tests...
python readSerial.py %comPortNumber%
if %errorlevel% neq 0 (
    echo ERROR: Test execution failed
    echo Check the error messages above.
    pause
    exit /b 1
)

exit /b 0