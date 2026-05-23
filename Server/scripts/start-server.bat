@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

cd /d "%~dp0.."

:: 检查 MySQL 是否运行
echo 检查 MySQL...
netstat -ano | findstr ":3306" >nul
if %errorlevel% neq 0 (
    echo MySQL 未运行，正在启动...

    set "MYSQL_BASE=%LOCALAPPDATA%\LockStep\mysql"
    set "MYSQL_ROOT=!MYSQL_BASE!\mysql-8.4.9-winx64"
    set "MYSQLD=!MYSQL_ROOT!\bin\mysqld.exe"
    set "MYSQL_CONFIG=!MYSQL_BASE!\my.ini"

    if not exist "!MYSQLD!" (
        echo 错误: 未找到 MySQL: !MYSQLD!
        pause
        exit /b 1
    )

    start "" /b "!MYSQLD!" --defaults-file="!MYSQL_CONFIG!"
    timeout /t 6 /nobreak >nul

    netstat -ano | findstr ":3306" >nul
    if !errorlevel! neq 0 (
        echo 错误: MySQL 启动失败
        pause
        exit /b 1
    )
    echo MySQL 已启动
) else (
    echo MySQL 已在运行
)

:: 查找 MSBuild
echo 正在构建服务器...
set "MSBUILD="

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
    for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"`) do (
        set "MSBUILD=%%i"
    )
)

if not defined MSBUILD (
    for %%p in (
        "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
        "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
        "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
        "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
        "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    ) do (
        if exist %%p (
            set "MSBUILD=%%p"
            goto :found_msbuild
        )
    )
)

:found_msbuild
if not defined MSBUILD (
    echo 错误: 未找到 MSBuild.exe，请安装 Visual Studio
    pause
    exit /b 1
)

echo 使用 MSBuild: %MSBUILD%
"%MSBUILD%" LockStepDemo.sln /p:Configuration=Debug /v:minimal

if %errorlevel% neq 0 (
    echo 错误: 构建失败
    pause
    exit /b 1
)

:: 启动服务器
set "SERVER_EXE=LockStepDemo\bin\Debug\LockStepDemo.exe"
if not exist "%SERVER_EXE%" (
    echo 错误: 未找到服务器可执行文件: %SERVER_EXE%
    pause
    exit /b 1
)

cd /d "%~dp0..\LockStepDemo\bin\Debug"
echo 正在启动服务器...
echo 按 'q' 键停止服务器
echo.
"%SERVER_EXE%"

pause
