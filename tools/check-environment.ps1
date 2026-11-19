[CmdletBinding()]
param(
    [switch]$InstallMissing,
    [switch]$StartMySql
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$unityVersionFile = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
$mysqlRoot = 'C:\Program Files\MySQL\MySQL Server 8.4\bin'
$mysqlData = Join-Path $env:LOCALAPPDATA 'CExam/mysql-data'
$mysqlLog = Join-Path $env:LOCALAPPDATA 'CExam/mysql.log'

function Test-Command([string]$Name) {
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Install-WingetPackage([string]$Id) {
    if (-not (Test-Command 'winget')) {
        throw 'winget is required to install missing packages.'
    }
    & winget install --id $Id -e --accept-source-agreements --accept-package-agreements --silent
}

function Add-PathIfPresent([string]$Path) {
    if ((Test-Path $Path) -and (($env:Path -split ';') -notcontains $Path)) {
        $env:Path = "$Path;$env:Path"
    }
}

Write-Output "Project: $projectRoot"
if (Test-Path $unityVersionFile) {
    Write-Output "Unity: $((Get-Content $unityVersionFile | Select-Object -First 1) -replace 'm_EditorVersion: ', '')"
} else {
    Write-Warning 'Unity project version file is missing.'
}

if (-not (Test-Command 'dotnet')) {
    if ($InstallMissing) { Install-WingetPackage 'Microsoft.DotNet.SDK.10' } else { Write-Warning '.NET SDK is missing.' }
}

$dockerExe = (Get-Command 'docker' -ErrorAction SilentlyContinue).Source
if (-not $dockerExe) {
    $dockerExe = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
    Add-PathIfPresent (Split-Path $dockerExe)
}
if (-not (Test-Path $dockerExe)) {
    if ($InstallMissing) {
        Install-WingetPackage 'Docker.DockerDesktop'
        Add-PathIfPresent (Split-Path $dockerExe)
    } else { Write-Warning 'Docker Desktop is missing.' }
}

$mysqlExe = (Get-Command 'mysql' -ErrorAction SilentlyContinue).Source
if (-not $mysqlExe) {
    $mysqlExe = Join-Path $mysqlRoot 'mysql.exe'
    Add-PathIfPresent $mysqlRoot
}
if (-not (Test-Path $mysqlExe)) {
    if ($InstallMissing) {
        Install-WingetPackage 'Oracle.MySQL'
        $mysqlExe = Join-Path $mysqlRoot 'mysql.exe'
        Add-PathIfPresent $mysqlRoot
    } else { Write-Warning 'MySQL is missing.' }
}

Write-Output "dotnet: $(if (Test-Command 'dotnet') { (dotnet --version).Trim() } else { 'MISSING' })"
Write-Output "docker: $(if (Test-Path $dockerExe) { (& $dockerExe --version).Trim() } else { 'MISSING' })"
Write-Output "mysql: $(if (Test-Path $mysqlExe) { (& $mysqlExe --version).Trim() } else { 'MISSING' })"

if (Test-Path $dockerExe) {
    $dockerStatus = (& $dockerExe desktop status 2>$null | Out-String)
    if ($dockerStatus -match 'Status\s+running') {
        Write-Output 'Docker engine: READY'
    } else {
        Write-Warning 'Docker is installed but the engine is unavailable. Check Docker Desktop, WSL2, Virtual Machine Platform, and administrator/restart requirements.'
    }
}

if (Test-Path $mysqlExe) {
    $mysqlService = Get-Service | Where-Object { $_.DisplayName -match 'MySQL' -or $_.Name -match 'mysql' } | Select-Object -First 1
    if ($mysqlService) {
        Write-Output "MySQL service: $($mysqlService.Status)"
    } else {
        Write-Output 'MySQL service: NOT REGISTERED (a user-owned development instance can still be started)'
    }
}

if ($StartMySql) {
    $mysqldExe = Join-Path $mysqlRoot 'mysqld.exe'
    if (-not (Test-Path $mysqldExe)) { throw "mysqld.exe not found at $mysqldExe" }
    New-Item -ItemType Directory -Force $mysqlData | Out-Null
    if (-not (Test-Path (Join-Path $mysqlData 'ibdata1'))) {
        & $mysqldExe --initialize-insecure --datadir=$mysqlData --console
    }
    $running = Get-Process mysqld -ErrorAction SilentlyContinue
    if (-not $running) {
        Start-Process -FilePath $mysqldExe -ArgumentList @("--datadir=$mysqlData", '--port=3306', '--bind-address=127.0.0.1', '--mysqlx=0', "--log-error=$mysqlLog", '--console') -WindowStyle Hidden | Out-Null
        Start-Sleep -Seconds 5
    }
    & $mysqlExe --protocol=tcp --host=127.0.0.1 --port=3306 --user=root -e "CREATE DATABASE IF NOT EXISTS c_exam; CREATE USER IF NOT EXISTS 'exam_app'@'localhost' IDENTIFIED BY 'change-me'; GRANT ALL PRIVILEGES ON c_exam.* TO 'exam_app'@'localhost'; FLUSH PRIVILEGES;"
    Write-Output 'MySQL development instance: READY (127.0.0.1:3306/c_exam)'
}

