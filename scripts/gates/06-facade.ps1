<#
.SYNOPSIS
    ASOP step 5 gate (facade-and-route-traffic): "CI job running the facade
    integration test suite is green at this commit, for both the
    legacy-path and new-path assertions."

.DESCRIPTION
    See ../../modernization/feed/FACADE.md: this slice took option (b) (no
    in-process routing between .NET Framework and .NET 10 - different
    runtimes, no interop bridge). The facade CONTRACT is proven by the
    legacy path (02) and the modern path (04) both asserting against the
    same golden files independently. This script just runs both in
    sequence and reports one combined result, rather than asking you to
    remember to run two scripts for one gate.

.EXAMPLE
    pwsh scripts/gates/06-facade.ps1
#>
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

& (Join-Path $PSScriptRoot "02-legacy-test.ps1")
$legacyExit = $LASTEXITCODE

& (Join-Path $PSScriptRoot "04-golden-diff.ps1")
$modernExit = $LASTEXITCODE

if ($legacyExit -ne 0 -or $modernExit -ne 0) {
    Write-Error "Facade gate FAILED: legacy-path exit=$legacyExit, new-path exit=$modernExit"
    exit 1
}
Write-Host "Gate PASSED: facade contract holds on both paths (legacy-path + new-path)" -ForegroundColor Green
