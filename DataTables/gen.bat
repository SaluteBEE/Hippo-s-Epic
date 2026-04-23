@echo off
setlocal

set LUBAN_DLL=..\Tools\Luban\Luban\Luban.dll
set CONF_ROOT=.
set GEN_DIR=..\Assets\Scripts\Gen

echo === Backup non-generated files ===
if exist "%GEN_DIR%\DataTableManager.cs" copy /y "%GEN_DIR%\DataTableManager.cs" "%TEMP%\DataTableManager.cs" >nul
if exist "%GEN_DIR%\LubanData.Gen.asmdef" copy /y "%GEN_DIR%\LubanData.Gen.asmdef" "%TEMP%\LubanData.Gen.asmdef" >nul

echo === Luban generate binary code + binary data ===
dotnet %LUBAN_DLL% -t client -c cs-bin -d bin --conf %CONF_ROOT%\luban.conf -x outputCodeDir=%GEN_DIR% -x outputDataDir=..\Assets\StreamingAssets\Gen\bin

if %errorlevel% neq 0 (
    echo Binary code generation FAILED!
    pause
    exit /b 1
)

echo === Restore non-generated files ===
if exist "%TEMP%\DataTableManager.cs" copy /y "%TEMP%\DataTableManager.cs" "%GEN_DIR%\DataTableManager.cs" >nul
if exist "%TEMP%\LubanData.Gen.asmdef" copy /y "%TEMP%\LubanData.Gen.asmdef" "%GEN_DIR%\LubanData.Gen.asmdef" >nul

echo.
echo === Luban generate json data for debugging ===
dotnet %LUBAN_DLL% -t client -d json --conf %CONF_ROOT%\luban.conf -x outputDataDir=..\Assets\StreamingAssets\Gen\json

if %errorlevel% neq 0 (
    echo JSON data generation FAILED!
    pause
    exit /b 1
)

echo.
echo === Done ===
pause
