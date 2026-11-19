[CmdletBinding()]
param([switch]$InstallMissing)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$unityVersionFile = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
$projectFile = Join-Path $projectRoot 'Server/CExam.Api/CExam.Api.csproj'
$failures = [System.Collections.Generic.List[string]]::new()

function Test-Command([string]$Name) { return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue) }

function Install-WingetPackage([string]$Id) {
    if (-not (Test-Command 'winget')) { throw 'winget is required to install missing packages.' }
    & winget install --id $Id -e --accept-source-agreements --accept-package-agreements --silent
    if ($LASTEXITCODE -ne 0) { throw "winget failed to install $Id (exit code $LASTEXITCODE)." }
}

function Find-Docker {
    $command = Get-Command docker -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $default = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
    if (Test-Path -LiteralPath $default) { return $default }
    return $null
}

function Find-Unity([string]$ExpectedVersion) {
    $candidates = @(
        (Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor\*\Editor\Unity.exe'),
        (Join-Path ${env:ProgramFiles} 'Unity\Editor\Unity.exe'),
        (Join-Path ${env:LOCALAPPDATA} 'Unity\Hub\Editor\*\Editor\Unity.exe')
    )
    foreach ($candidate in $candidates) {
        $match = Get-ChildItem -Path $candidate -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($match -and $match.VersionInfo.ProductVersion -like "$ExpectedVersion*") { return $match }
    }
    return $null
}

function Test-DockerEngine([string]$DockerPath) {
    $job = Start-Job -ScriptBlock { param($path) & $path info --format '{{.ServerVersion}}' *> $null; $LASTEXITCODE } -ArgumentList $DockerPath
    if (-not (Wait-Job -Job $job -Timeout 15)) {
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
        return $false
    }
    $exitCode = Receive-Job -Job $job
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    return [int]$exitCode -eq 0
}

Write-Output "Project: $projectRoot"
if (-not (Test-Path -LiteralPath $unityVersionFile)) {
    $failures.Add('Unity version file is missing.')
} else {
    $expectedUnity = ((Get-Content -LiteralPath $unityVersionFile | Select-Object -First 1) -replace '^m_EditorVersion:\s*', '').Trim()
    $unity = Find-Unity $expectedUnity
    if ($unity) { Write-Output "Unity: $($unity.VersionInfo.ProductVersion) ($($unity.FullName))" }
    else {
        Write-Output "Unity: MISSING (required $expectedUnity)"
        $failures.Add("Unity $expectedUnity is not installed or discoverable; install it through Unity Hub or complete it manually.")
    }
}

$targetFramework = ((Select-String -LiteralPath $projectFile -Pattern '<TargetFramework>([^<]+)</TargetFramework>').Matches | Select-Object -First 1).Groups[1].Value
$requiredDotnetMajor = [int]($targetFramework -replace '^net', '' -replace '\..*$', '')
if (-not (Test-Command 'dotnet') -and $InstallMissing) { Install-WingetPackage 'Microsoft.DotNet.SDK.10' }
if (Test-Command 'dotnet') {
    $dotnetVersion = (& dotnet --version 2>$null).Trim()
    Write-Output ".NET SDK: $dotnetVersion"
    if ([int]($dotnetVersion.Split('.')[0]) -lt $requiredDotnetMajor) { $failures.Add(".NET SDK $requiredDotnetMajor or newer is required for $targetFramework.") }
} else { $failures.Add(".NET SDK for $targetFramework is missing.") }

$dockerExe = Find-Docker
if (-not $dockerExe -and $InstallMissing) { Install-WingetPackage 'Docker.DockerDesktop'; $dockerExe = Find-Docker }
if (-not $dockerExe) {
    $failures.Add('Docker CLI is missing; install Docker Desktop manually or use -InstallMissing.')
} else {
    Write-Output "Docker CLI: $((& $dockerExe --version 2>$null | Out-String).Trim())"
    $composeVersion = (& $dockerExe compose version 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { $failures.Add('Docker Compose plugin is missing.') }
    else { Write-Output "Docker Compose: $composeVersion" }
    if (Test-DockerEngine $dockerExe) {
        Write-Output 'Docker engine: READY'
        $services = & $dockerExe compose ps --status running --services 2>$null
        if ($services -contains 'mysql') { Write-Output 'Project MySQL container: RUNNING (existing state preserved)' }
        else { Write-Output 'Project MySQL container: NOT RUNNING (start with tools/start-local.ps1)' }
    } else {
        $failures.Add('Docker CLI is installed but the engine is unavailable; start Docker Desktop. A Windows restart or administrator action may be required.')
    }
}

if ($failures.Count -gt 0) { Write-Error (($failures | ForEach-Object { "- $_" }) -join [Environment]::NewLine); exit 1 }
Write-Output 'Environment: READY'

