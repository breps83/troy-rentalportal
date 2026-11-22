<#
.SYNOPSIS
    Runs the Playwright E2E tests, recording an annotated video and a trace of each test.

.EXAMPLE
    ./tests/RentalPortal.E2E.Tests/run-e2e.ps1
    ./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Filter "Requirement=FR 3.b"
    ./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Filter "FullyQualifiedName~ReviewTests" -Headed
    ./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -NoVideo
    ./tests/RentalPortal.E2E.Tests/run-e2e.ps1 -Open      # open the report when done
#>
param(
    # A dotnet test filter, for example "Requirement=FR 3.b" or "FullyQualifiedName~ReviewTests".
    [string]$Filter = "",

    # Skip videos (traces are skipped too). Faster; same as running from Test Explorer.
    [switch]$NoVideo,

    # Show the browser window while the tests run.
    [switch]$Headed,

    # Milliseconds to pause on each browser action. Defaults to 600 with video, 0 without.
    [int]$SlowMo = -1,

    # Open this run's report (index.html in its output folder) in the default browser when it finishes.
    [switch]$Open
)

$ErrorActionPreference = "Stop"
$project = $PSScriptRoot
$root = Resolve-Path (Join-Path $project "../..")

# Build to a temp folder, not bin/: a running copy of the app locks src/RentalPortal.Web/bin.
$build = Join-Path ([System.IO.Path]::GetTempPath()) "RentalPortal.E2E.Tests"

Write-Host "Building the E2E tests to $build ..."
dotnet build $project --nologo -v q -o $build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Installs Chromium for this Playwright version. Quick when it's already installed.
$playwright = Join-Path $build "playwright.ps1"
& pwsh -NoProfile -File $playwright install chromium
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$stamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$output = Join-Path $root "TestResults/e2e/$stamp"

$env:E2E_OUTPUT = $output
$env:E2E_VIDEO = if ($NoVideo) { "0" } else { "1" }
$env:E2E_HEADED = if ($Headed) { "1" } else { "0" }
if ($SlowMo -ge 0) { $env:E2E_SLOWMO = "$SlowMo" } else { Remove-Item Env:E2E_SLOWMO -ErrorAction SilentlyContinue }

$arguments = @("test", (Join-Path $build "RentalPortal.E2E.Tests.dll"), "--logger", "trx;LogFileName=e2e.trx",
    "--results-directory", $output)
if ($Filter) { $arguments += @("--filter", $Filter) }

dotnet @arguments
$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "Results: $output"
if (-not $NoVideo) {
    Write-Host "  videos/     one annotated .webm per test"
    Write-Host "  traces/     open with: pwsh $playwright show-trace <file.zip>"
}
Write-Host "  e2e.trx     test results (pass/fail, errors, step output)"
Write-Host "  index.html  report: every test, grouped by requirement, with its video"

& (Join-Path $project "write-e2e-report.ps1") -RunFolder $output

if ($Open) {
    # This run's own report (latest/ only changes for runs with video).
    $report = Join-Path $output "index.html"
    if (Test-Path $report) { Start-Process $report }
}

exit $exitCode
