@echo off
REM Exit on error
setlocal enabledelayedexpansion

set content="<meta http-equiv="REFRESH" content="0;URL=documentation/html/index.html">"

cd ../

REM Create doxygen documentation
doxygen Doxyfile

REM Create documentation page that refers to doxygen's documentation index page
echo %content% > documentation.html
cd ci

PAUSE