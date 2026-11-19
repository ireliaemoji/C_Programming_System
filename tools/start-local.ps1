[CmdletBinding()]
param(
    [switch]$Stop,
    [int]$ApiPort = 5000,
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $projectRoot '.env'
$stateDirectory = Join-Path $projectRoot '.local'
$pidFile = Join-Path $stateDirectory 'api.pid'
$apiLog = Join-Path $stateDirectory 'api.log'
$apiErrorLog = Join-Path $stateDirectory 'api.error.log'

function Read-DotEnv([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing $Path. Copy .env.example to .env and set local passwords."
    }

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>.*)\s*$') {
            $value = $Matches.value.Trim()
            if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            $values[$Matches.key] = $value
        }
    }
    return $values
}

function Invoke-Docker([string[]]$Arguments) {
    & docker @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "docker $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Test-DockerEngine {
    $job = Start-Job -ScriptBlock { & docker info --format '{{.ServerVersion}}' *> $null; $LASTEXITCODE }
    if (-not (Wait-Job -Job $job -Timeout 15)) {
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
        return $false
    }
    $exitCode = Receive-Job -Job $job
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    return [int]$exitCode -eq 0
}

function Test-PortInUse([int]$Port) {
    return $null -ne (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1)
}

if ($Stop) {
    if (Test-Path -LiteralPath $pidFile) {
        $apiProcess = Get-Process -Id ([int](Get-Content -LiteralPath $pidFile)) -ErrorAction SilentlyContinue
        if ($apiProcess) {
            Stop-Process -Id $apiProcess.Id
            Write-Output "Stopped API process $($apiProcess.Id)."
        } else {
            Write-Output 'API process is already stopped.'
        }
        Remove-Item -LiteralPath $pidFile -Force
    } else {
        Write-Output 'No API process recorded by this script.'
    }
    Invoke-Docker @('compose', 'down')
    exit 0
}

$values = Read-DotEnv $envFile
foreach ($required in @('MYSQL_ROOT_PASSWORD', 'MYSQL_PASSWORD')) {
    if (-not $values.ContainsKey($required) -or [string]::IsNullOrWhiteSpace($values[$required]) -or $values[$required] -like 'replace-with-*') {
        throw "$required is missing or still uses the example value in .env."
    }
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI is missing. Install/start Docker Desktop, then rerun tools/check-environment.ps1.'
}

if (-not (Test-DockerEngine)) { throw 'Docker engine is unavailable. Start Docker Desktop and rerun this script.' }

$env:MYSQL_ROOT_PASSWORD = $values.MYSQL_ROOT_PASSWORD
$env:MYSQL_PASSWORD = $values.MYSQL_PASSWORD
if ($values.ContainsKey('MYSQL_HOST_PORT') -and $values.MYSQL_HOST_PORT) {
    $mysqlHostPort = [int]$values.MYSQL_HOST_PORT
} else {
    $mysqlHostPort = 3306
}

Write-Output "Starting project MySQL container on 127.0.0.1:$mysqlHostPort..."
$existingMysql = & docker compose ps --status running --services 2>$null
if (Test-PortInUse $mysqlHostPort -and $existingMysql -notcontains 'mysql') {
    throw "MySQL host port $mysqlHostPort is already in use by another process. Set MYSQL_HOST_PORT in .env to an unused loopback port; no process was stopped."
}
Invoke-Docker @('compose', 'up', '-d', 'mysql') | Out-Null

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    $check = & docker compose exec -T mysql sh -c 'mysql --protocol=socket -u"$MYSQL_USER" -p"$MYSQL_PASSWORD" "$MYSQL_DATABASE" -e "SELECT 1"' 2>&1
    if ($LASTEXITCODE -eq 0) { break }
    if ((Get-Date) -ge $deadline) { throw 'MySQL container did not accept an authenticated SELECT 1 before the timeout.' }
    Start-Sleep -Seconds 2
} while ($true)
Write-Output 'MySQL container: READY (authenticated SELECT 1).'

if (Test-PortInUse $ApiPort) {
    try {
        $health = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$ApiPort/health" -TimeoutSec 5
        Write-Output "API already listens on 127.0.0.1:$ApiPort (HTTP $($health.StatusCode)); leaving the existing process untouched."
        exit 0
    } catch {
        throw "Port $ApiPort is already in use by another process and its /health endpoint is not ready. Stop it manually or choose -ApiPort."
    }
}

New-Item -ItemType Directory -Force $stateDirectory | Out-Null
$env:ConnectionStrings__Default = "Server=127.0.0.1;Port=$mysqlHostPort;Database=c_exam;User=exam_app;Password=$($values.MYSQL_PASSWORD);"
$env:ASPNETCORE_URLS = "http://127.0.0.1:$ApiPort"
$apiProcess = Start-Process -FilePath 'dotnet' -WorkingDirectory $projectRoot -ArgumentList @('run', '--project', 'Server/CExam.Api', '--no-launch-profile') -RedirectStandardOutput $apiLog -RedirectStandardError $apiErrorLog -PassThru -WindowStyle Hidden
$apiProcess.Id | Set-Content -LiteralPath $pidFile

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    try {
        $health = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$ApiPort/health" -TimeoutSec 3
        if ($health.StatusCode -eq 200) {
            Write-Output "API: READY at http://127.0.0.1:$ApiPort (PID $($apiProcess.Id))."
            exit 0
        }
    } catch {
        # The API may still be compiling or starting.
    }
    if ($apiProcess.HasExited) {
        throw "API exited during startup. See $apiErrorLog."
    }
    if ((Get-Date) -ge $deadline) { throw "API did not return HTTP 200 before the timeout. See $apiLog and $apiErrorLog." }
    Start-Sleep -Seconds 2
} while ($true)
