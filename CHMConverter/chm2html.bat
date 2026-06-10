@echo off
setlocal enabledelayedexpansion

:: ============================================================
::  CHM to HTML Converter
::  Usage: chm2html.bat <CHM_file_or_dir> [output_dir]
::  Uses hh.exe -decompile (short paths to avoid quoting issues)
:: ============================================================

set "HH=%SystemRoot%\hh.exe"
set "LOG_FILE=%~dp0chm2html_log.txt"
set "SUCCESS=0"
set "FAIL=0"
set "START_TIME=%time%"

set "INPUT=%~1"
set "OUTPUT=%~2"

if "%INPUT%"=="" (
    echo Usage: %~nx0 ^<CHM_file_or_dir^> [output_dir]
    echo Examples:
    echo   %~nx0 "D:\docs\manual.chm"
    echo   %~nx0 "D:\docs\manual.chm" "D:\output"
    echo   %~nx0 "D:\chm_files"
    pause
    exit /b 0
)

if not exist "%HH%" (
    echo [ERR] hh.exe not found
    pause
    exit /b 1
)

if "%OUTPUT%"=="" set "OUTPUT=%~dp0html_output"
if not exist "%OUTPUT%" mkdir "%OUTPUT%"

:: Convert to short paths (no spaces, hh.exe handles them correctly)
for %%i in ("%OUTPUT%") do set "OUTPUT=%%~si"
for %%i in ("%INPUT%")  do set "INPUT=%%~si"

echo ======================================== > "%LOG_FILE%"
echo Input: %INPUT% >> "%LOG_FILE%"
echo Output: %OUTPUT% >> "%LOG_FILE%"
echo ======================================== >> "%LOG_FILE%"

echo ========================================
echo   CHM to HTML
echo   Output: %OUTPUT%
echo ========================================
echo.

:: Collect files
set "TMPFILE=%TEMP%\chm_list_%RANDOM%.tmp"
if exist "%TMPFILE%" del /f /q "%TMPFILE%"

if exist "%INPUT%\" (
    echo [INFO] Scanning: %INPUT%
    for /r "%INPUT%" %%f in (*.chm) do echo %%~sf>> "%TMPFILE%"
) else (
    echo %~s1> "%TMPFILE%"
)

set "TOTAL=0"
for /f "usebackq" %%a in ("%TMPFILE%") do set /a TOTAL+=1

if %TOTAL%==0 (
    echo [WARN] No .chm files found
    del /f /q "%TMPFILE%"
    pause
    exit /b 0
)

echo [INFO] %TOTAL% file(s) to convert
echo.

:: Convert each file
set "IDX=0"
for /f "usebackq delims=" %%f in ("%TMPFILE%") do (
    set /a IDX+=1
    set "CHM=%%f"
    set "NAME=%%~nf"
    set "OUTDIR=%OUTPUT%\%%~nf"

    echo [!IDX!/%TOTAL%] %%~nxf ...

    if exist "!OUTDIR!" rd /s /q "!OUTDIR!"
    mkdir "!OUTDIR!" >nul 2>&1

    :: Call hh.exe with short paths (NO quotes in arguments!)
    "%HH%" -decompile "!OUTDIR!" "!CHM!" >nul 2>&1

    set "FCNT=0"
    for /f %%c in ('dir /s /b "!OUTDIR!" 2^>nul ^| find /c /v ""') do set "FCNT=%%c"

    if !FCNT! GTR 0 (
        echo    [OK] !FCNT! files
        echo [%time%] [OK] !NAME! - !FCNT! files >> "%LOG_FILE%"
        set /a SUCCESS+=1
    ) else (
        echo    [ERR] No output
        echo [%time%] [ERR] !NAME! - no output >> "%LOG_FILE%"
        rd /s /q "!OUTDIR!" >nul 2>&1
        set /a FAIL+=1
    )
)

del /f /q "%TMPFILE%"
set "END_TIME=%time%"

echo.
echo ========================================
echo   Done: OK=%SUCCESS%  FAIL=%FAIL%
echo ========================================

echo. >> "%LOG_FILE%"
echo Done: OK=%SUCCESS%  FAIL=%FAIL% >> "%LOG_FILE%"

if %SUCCESS% GTR 0 explorer "%OUTPUT%"

echo [INFO] Log: %LOG_FILE%
pause
endlocal
exit /b 0
