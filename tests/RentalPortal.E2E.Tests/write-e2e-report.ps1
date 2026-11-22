<#
.SYNOPSIS
    Writes index.html for an E2E run: every test's video, grouped by requirement, failures first.
    Also writes TestResults/e2e/latest/index.html, which points at the same run.

.EXAMPLE
    ./tests/RentalPortal.E2E.Tests/write-e2e-report.ps1 -RunFolder TestResults/e2e/2026-11-22_16-20-18
#>
param(
    # The run's output folder, containing e2e.trx, videos/, and traces/.
    [Parameter(Mandatory)]
    [string]$RunFolder
)

$ErrorActionPreference = "Stop"
$RunFolder = (Resolve-Path $RunFolder).Path
$trxPath = Join-Path $RunFolder "e2e.trx"

if (-not (Test-Path $trxPath)) {
    Write-Warning "No e2e.trx in $RunFolder; skipping the report."
    return
}

[xml]$trx = Get-Content $trxPath -Raw
$ns = @{ t = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010" }

function Encode([string]$text) { [System.Net.WebUtility]::HtmlEncode($text) }

# One record per test: name, outcome, duration, steps, requirement IDs, error, video and trace file names.
$tests = foreach ($node in (Select-Xml -Xml $trx -XPath "//t:UnitTestResult" -Namespace $ns).Node) {
    $stdout = ""
    $output = $node.Output
    if ($output -and $output.StdOut) { $stdout = $output.StdOut }

    $steps = @()
    $requirements = @()
    $video = $null
    $trace = $null

    foreach ($line in ($stdout -split "`r?`n")) {
        if ($line -match '^\[(?<id>[^\]]+)\] (?<text>.+)$') {
            $steps += [pscustomobject]@{ Id = $Matches.id; Text = $Matches.text }
            if ($requirements -notcontains $Matches.id) { $requirements += $Matches.id }
        }
        elseif ($line -match '^Video: (?<path>.+)$') { $video = Split-Path $Matches.path.Trim() -Leaf }
        elseif ($line -match '^Trace: (?<path>.+)$') { $trace = Split-Path $Matches.path.Trim() -Leaf }
    }

    $errorText = ""
    if ($output -and $output.ErrorInfo) { $errorText = [string]$output.ErrorInfo.Message }

    if ($requirements.Count -eq 0) { $requirements = @("Other") }

    $name = $node.testName -replace '^RentalPortal\.E2E\.Tests\.Tests\.', ''

    [pscustomobject]@{
        Name         = $name
        Outcome      = $node.outcome
        Seconds      = [math]::Round(([TimeSpan]$node.duration).TotalSeconds, 1)
        Steps        = $steps
        Requirements = $requirements
        Error        = $errorText
        Video        = $video
        Trace        = $trace
    }
}

$failed = @($tests | Where-Object { $_.Outcome -ne "Passed" })
$passed = @($tests | Where-Object { $_.Outcome -eq "Passed" })
$runName = Split-Path $RunFolder -Leaf

# Order: Screens, Roles, TR ..., FR ..., anything else.
function RequirementSortKey([string]$id) {
    if ($id -eq "Screens") { return "0" }
    if ($id -eq "Roles") { return "1" }
    if ($id -like "TR *") { return "2 $id" }
    if ($id -like "FR *") { return "3 $id" }
    return "4 $id"
}

$requirementIds = $tests | ForEach-Object { $_.Requirements } | Select-Object -Unique |
    Sort-Object { RequirementSortKey $_ }

function TestCard($test, [string]$prefix) {
    $ok = $test.Outcome -eq "Passed"
    $badge = if ($ok) { '<span class="badge pass">Passed</span>' } else { '<span class="badge fail">' + (Encode $test.Outcome) + '</span>' }

    $icon = if ($ok) { "&#9989;" } else { "&#10060;" }
    $cardClass = if ($ok) { "passed" } else { "failed" }

    $html = "<article class=`"card $cardClass`">"
    $html += "<header><h3>$icon $(Encode $test.Name)</h3>$badge<span class=`"muted`">$($test.Seconds) s</span></header>"

    if ($test.Error) {
        # Playwright errors end with a long page snapshot; show the first lines and fold the rest.
        $lines = $test.Error.Trim() -split "`r?`n"
        $html += "<pre class=`"error`">$(Encode (($lines | Select-Object -First 4) -join "`n"))</pre>"

        if ($lines.Count -gt 4) {
            $html += "<details><summary>Full error</summary><pre class=`"error`">$(Encode $test.Error.Trim())</pre></details>"
        }
    }

    if ($test.Steps.Count -gt 0) {
        $html += "<ol class=`"steps`">"
        foreach ($step in $test.Steps) {
            $html += "<li><span class=`"id`">$(Encode $step.Id)</span> $(Encode $step.Text)</li>"
        }
        $html += "</ol>"
    }

    if ($test.Video) {
        $html += "<video controls preload=`"metadata`" src=`"$prefix" + "videos/$($test.Video)`"></video>"
    }
    else {
        $html += "<p class=`"muted`">No video for this test.</p>"
    }

    if ($test.Trace) {
        $html += "<p class=`"muted`">Trace: <code>$(Encode (Join-Path $RunFolder "traces\$($test.Trace)"))</code></p>"
    }

    $html += "</article>"
    return $html
}

function BuildPage([string]$prefix) {
    $summaryClass = if ($failed.Count -eq 0) { "pass" } else { "fail" }

    $body = "<h1>E2E run $runName</h1>"
    $body += "<p class=`"summary $summaryClass`">$($passed.Count) passed, $($failed.Count) failed, $($tests.Count) total</p>"

    $body += "<nav><strong>Requirements:</strong> "
    foreach ($id in $requirementIds) {
        $anchor = "req-" + ($id -replace '[^A-Za-z0-9]', '-')
        $count = @($tests | Where-Object { $_.Requirements -contains $id }).Count
        $hasFailure = @($failed | Where-Object { $_.Requirements -contains $id }).Count -gt 0
        $mark = if ($hasFailure) { "&#10060;" } else { "&#9989;" }
        $body += "<a href=`"#$anchor`">$mark $(Encode $id) ($count)</a> "
    }
    $body += "</nav>"

    if ($failed.Count -gt 0) {
        $body += "<section><h2>&#10060; Failed ($($failed.Count))</h2>"
        foreach ($test in $failed) { $body += TestCard $test $prefix }
        $body += "</section>"
    }

    foreach ($id in $requirementIds) {
        $anchor = "req-" + ($id -replace '[^A-Za-z0-9]', '-')
        $hasFailure = @($failed | Where-Object { $_.Requirements -contains $id }).Count -gt 0
        $mark = if ($hasFailure) { "&#10060;" } else { "&#9989;" }
        $body += "<section id=`"$anchor`"><h2>$mark $(Encode $id)</h2>"
        foreach ($test in ($tests | Where-Object { $_.Requirements -contains $id })) {
            $body += TestCard $test $prefix
        }
        $body += "</section>"
    }

    return @"
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>E2E run $runName</title>
<style>
:root { --bg: #f6f7f9; --card: #fff; --text: #1d2125; --muted: #5f6b76; --border: #d9dee3;
        --pass: #1a7f37; --fail: #c62828; --accent: #0d6efd; }
@media (prefers-color-scheme: dark) {
  :root { --bg: #16191d; --card: #1f2328; --text: #e6e8ea; --muted: #9aa5b1; --border: #343a40;
          --pass: #3fb950; --fail: #f85149; --accent: #4c9aff; }
}
body { margin: 0; padding: 24px 16px; background: var(--bg); color: var(--text);
       font: 15px/1.5 system-ui, sans-serif; }
main { max-width: 1100px; margin: 0 auto; }
h1 { margin: 0 0 4px; font-size: 24px; }
h2 { margin: 32px 0 12px; font-size: 19px; border-bottom: 1px solid var(--border); padding-bottom: 4px; }
h3 { margin: 0; font-size: 15px; word-break: break-word; }
nav { display: flex; flex-wrap: wrap; gap: 6px 12px; margin: 12px 0; }
nav a { color: var(--accent); text-decoration: none; white-space: nowrap; }
.summary { font-weight: 600; font-size: 17px; margin: 0; }
.summary.pass { color: var(--pass); } .summary.fail { color: var(--fail); }
.card { background: var(--card); border: 1px solid var(--border); border-radius: 8px; padding: 12px 14px;
        margin: 0 0 14px; }
.card.passed { border-left: 5px solid var(--pass); }
.card.failed { border-color: var(--fail); border-left: 5px solid var(--fail); }
.card header { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; }
.badge { font-size: 12px; font-weight: 600; padding: 1px 8px; border-radius: 10px; color: #fff; }
.badge.pass { background: var(--pass); } .badge.fail { background: var(--fail); }
.muted { color: var(--muted); font-size: 13px; }
.steps { margin: 8px 0; padding-left: 22px; }
.steps .id { font-weight: 600; color: var(--accent); }
.error { white-space: pre-wrap; background: rgba(200, 40, 40, .08); border-left: 3px solid var(--fail);
         padding: 8px; margin: 8px 0; font-size: 13px; overflow-x: auto; }
video { display: block; width: 100%; max-width: 760px; max-height: 60vh; background: #000; border-radius: 6px; margin-top: 6px; }
code { word-break: break-all; }
details summary { cursor: pointer; color: var(--accent); font-size: 13px; }
</style>
</head>
<body><main>
$body
</main></body>
</html>
"@
}

$runIndex = Join-Path $RunFolder "index.html"
Set-Content -Path $runIndex -Value (BuildPage "") -Encoding utf8

# A fixed, short path to the most recent report with videos. Runs without video (-NoVideo, Test
# Explorer) leave it alone, so it always has something to watch.
if (-not ($tests | Where-Object { $_.Video })) {
    Write-Host "Report: $runIndex (no videos; latest/index.html not changed)"
    return
}

$latestFolder = Join-Path (Split-Path $RunFolder -Parent) "latest"
New-Item -ItemType Directory -Force -Path $latestFolder | Out-Null
$latestIndex = Join-Path $latestFolder "index.html"
Set-Content -Path $latestIndex -Value (BuildPage "../$runName/") -Encoding utf8

Write-Host "Report: $latestIndex"
