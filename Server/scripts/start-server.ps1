param(
    [string]$ServerRoot
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ServerRoot)) {
    $serverRoot = (Resolve-Path -LiteralPath (Join-Path $scriptRoot '..')).Path
} else {
    $serverRoot = (Resolve-Path -LiteralPath $ServerRoot).Path
}

Set-Location -LiteralPath $serverRoot

$mysqlBase = Join-Path $env:LOCALAPPDATA 'LockStep\mysql'
$mysqlRoot = Join-Path $mysqlBase 'mysql-8.4.9-winx64'
$mysqlConfig = Join-Path $mysqlBase 'my.ini'
$mysqld = Join-Path $mysqlRoot 'bin\mysqld.exe'

if (-not (Get-NetTCPConnection -LocalPort 3306 -ErrorAction SilentlyContinue)) {
    if (-not (Test-Path -LiteralPath $mysqld)) {
        throw "Local MySQL not found: $mysqld"
    }

    Write-Host 'Starting MySQL...'
    Start-Process -FilePath $mysqld -ArgumentList "--defaults-file=$mysqlConfig" -WorkingDirectory $mysqlRoot -WindowStyle Hidden
    Start-Sleep -Seconds 6

    if (-not (Get-NetTCPConnection -LocalPort 3306 -ErrorAction SilentlyContinue)) {
        throw 'MySQL failed to listen on port 3306.'
    }
} else {
    Write-Host 'MySQL is already running.'
}

$msbuild = $null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}

if (-not $msbuild) {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        $msbuild = $command.Source
    }
}

if (-not $msbuild) {
    $candidates = @(
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
    )

    $msbuild = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if (-not $msbuild) {
    throw 'MSBuild.exe not found. Please install Visual Studio or Build Tools with MSBuild.'
}

Write-Host 'Building server...'
& $msbuild .\LockStepDemo.sln /p:Configuration=Debug
if ($LASTEXITCODE -ne 0) {
    throw "Server build failed. MSBuild exit code: $LASTEXITCODE"
}

$serverExe = Join-Path $serverRoot 'LockStepDemo\bin\Debug\LockStepDemo.exe'
if (-not (Test-Path -LiteralPath $serverExe)) {
    throw "Server executable not found after build: $serverExe"
}

Set-Location -LiteralPath (Split-Path -Parent $serverExe)
Write-Host 'Starting server...'
& $serverExe
