@echo off
setlocal

set LUBAN_DLL=..\Tools\Luban\Luban\Luban.dll
set CONF_ROOT=.
set GEN_DIR=..\Assets\Scripts\Gen

echo === Backup non-generated files ===
if exist "%GEN_DIR%\DataTableManager.cs" copy /y "%GEN_DIR%\DataTableManager.cs" "%TEMP%\DataTableManager.cs" >nul
if exist "%GEN_DIR%\LubanData.Gen.asmdef" copy /y "%GEN_DIR%\LubanData.Gen.asmdef" "%TEMP%\LubanData.Gen.asmdef" >nul
if exist "%GEN_DIR%\DialogManager.cs" copy /y "%GEN_DIR%\DialogManager.cs" "%TEMP%\DialogManager.cs" >nul
if exist "%GEN_DIR%\SpeakerSide.cs" copy /y "%GEN_DIR%\SpeakerSide.cs" "%TEMP%\SpeakerSide.cs" >nul
if exist "%GEN_DIR%\OptionInfo.cs" copy /y "%GEN_DIR%\OptionInfo.cs" "%TEMP%\OptionInfo.cs" >nul

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
if exist "%TEMP%\DialogManager.cs" copy /y "%TEMP%\DialogManager.cs" "%GEN_DIR%\DialogManager.cs" >nul
if exist "%TEMP%\SpeakerSide.cs" copy /y "%TEMP%\SpeakerSide.cs" "%GEN_DIR%\SpeakerSide.cs" >nul
if exist "%TEMP%\OptionInfo.cs" copy /y "%TEMP%\OptionInfo.cs" "%GEN_DIR%\OptionInfo.cs" >nul

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
