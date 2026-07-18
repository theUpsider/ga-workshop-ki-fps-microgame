<#
.SYNOPSIS
    Asks a running Unity Editor instance (via AgentTestBridge) to run Edit Mode tests
    and waits for the result.

.DESCRIPTION
    Unity.FPS.Game.Editor.AgentTestBridge polls Temp/agent-test-request.txt while the
    Editor is open. Writing a test name (or an empty file for "all tests") into that
    file triggers a TestRunnerApi run; the bridge writes progress/results to
    Temp/agent-test-result.json.

    This script writes the request, polls the result file, prints a summary, and
    exits with a code coding agents can branch on:
      0 = tests ran and all passed
      1 = tests ran and at least one failed
      2 = timed out waiting for a result (Editor likely not open, or compiling)
      3 = the bridge reported an error (e.g. could not read the request file)

.PARAMETER TestName
    Optional fully qualified test name/namespace to run (matches Filter.testNames).
    Omit to run the full Edit Mode suite.

.PARAMETER TimeoutSeconds
    Max time to wait for a "finished" result before giving up. Default 300.

.PARAMETER PollIntervalSeconds
    Delay between result-file checks. Default 2.

.EXAMPLE
    powershell -NoProfile -File Tools/agent-tests/Invoke-AgentTests.ps1

.EXAMPLE
    powershell -NoProfile -File Tools/agent-tests/Invoke-AgentTests.ps1 -TestName "Unity.FPS.Tests.BarrierTests"
#>
param(
    [string]$TestName = "",
    [int]$TimeoutSeconds = 300,
    [int]$PollIntervalSeconds = 2
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$tempDir = Join-Path $repoRoot "Temp"
$requestPath = Join-Path $tempDir "agent-test-request.txt"
$resultPath = Join-Path $tempDir "agent-test-result.json"

if (-not (Test-Path $tempDir)) {
    Write-Host "Temp/ existiert nicht ($tempDir). Ist das Unity-Projekt im Editor geoeffnet?" -ForegroundColor Yellow
    exit 2
}

if (Test-Path $resultPath) {
    Remove-Item $resultPath -Force
}

Set-Content -Path $requestPath -Value $TestName -NoNewline

$label = if ($TestName) { $TestName } else { "alle Edit Mode Tests" }
Write-Host "Testlauf angefordert: $label"
Write-Host "Warte auf Unity Editor (AgentTestBridge)..."

$elapsed = 0
while ($elapsed -lt $TimeoutSeconds) {
    if (Test-Path $resultPath) {
        $raw = Get-Content -Path $resultPath -Raw -ErrorAction SilentlyContinue
        if ($raw) {
            try {
                $result = $raw | ConvertFrom-Json
            }
            catch {
                $result = $null
            }

            if ($result) {
                switch ($result.status) {
                    "running" {
                        # still in progress, keep polling
                    }
                    "error" {
                        Write-Host "AgentTestBridge Fehler: $($result.message)" -ForegroundColor Red
                        exit 3
                    }
                    "finished" {
                        $state = $result.state
                        $pass = $result.passCount
                        $fail = $result.failCount

                        Write-Host ""
                        Write-Host "Testlauf abgeschlossen: $state"
                        Write-Host "  Bestanden: $pass"
                        Write-Host "  Fehlgeschlagen: $fail"

                        exit ($(if ($fail -gt 0) { 1 } else { 0 }))
                    }
                }
            }
        }
    }

    Start-Sleep -Seconds $PollIntervalSeconds
    $elapsed += $PollIntervalSeconds
}

Write-Host "Timeout nach $TimeoutSeconds Sekunden. Kein Ergebnis von AgentTestBridge erhalten." -ForegroundColor Yellow
Write-Host "Pruefe, ob der Unity Editor fuer dieses Projekt geoeffnet ist und nicht kompiliert/blockiert."
exit 2
