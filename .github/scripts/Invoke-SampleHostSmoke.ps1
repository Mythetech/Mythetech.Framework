# Runs SampleHost.Desktop once in Hermes smoke mode and fails unless the shared verdict rules pass it.
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $AppPath,
    [Parameter(Mandatory)] [string] $VerdictScript,
    [string] $OutputDir = 'smoke-output',
    [int] $TimeoutSeconds = 90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $VerdictScript

$dir = (New-Item -ItemType Directory -Force -Path $OutputDir).FullName
$stdout = Join-Path $dir 'app-stdout.log'
$stderr = Join-Path $dir 'app-stderr.log'
$result = Join-Path $dir 'result.json'

$env:HERMES_SMOKE_TEST = '1'
$env:HERMES_SMOKE_TEST_TIMEOUT = "$TimeoutSeconds"
$env:HERMES_SMOKE_TEST_RESULT = $result

$app = (Resolve-Path -LiteralPath $AppPath).Path
$process = Start-Process -FilePath $app -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
# Reading Handle now keeps ExitCode available after the process ends on Windows.
$null = $process.Handle
$timedOut = -not $process.WaitForExit(($TimeoutSeconds + 30) * 1000)
if ($timedOut) {
    $process.Kill($true)
    $null = $process.WaitForExit(10000)
}

$log = @(Get-Content -LiteralPath $stdout -ErrorAction SilentlyContinue)
$json = if (Test-Path -LiteralPath $result) { [string](Get-Content -LiteralPath $result -Raw) } else { '' }
$exitCode = if ($timedOut) { $null } else { $process.ExitCode }
$verdict = Get-SmokeVerdict -LogLines $log -ResultJson $json -Mode verdict -ExitCode $exitCode -TimedOut $timedOut

# The app's own console logging is in the uploaded artifact; the smoke lines are enough here.
$log | Where-Object { $_ -like 'HERMES_*' } | ForEach-Object { Write-Output "  $_" }
Write-Output "verdict: passed=$($verdict.Passed) reason=$($verdict.Reason)"

if (-not $verdict.Passed) {
    Write-Output "::error::SampleHost.Desktop smoke run failed: $($verdict.Reason)"
    exit 1
}
