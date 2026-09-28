<#
.SYNOPSIS
    ASOP step 2 gate: every in-scope rule in modernization/<slice>/RULES.md
    is referenced by at least one characterization test.

.DESCRIPTION
    Thin wrapper around scripts/check-rules-have-tests.py (the actual gate
    logic lives there, cross-platform already - no PowerShell reimplementation).
    Requires Python 3 + no third-party packages (stdlib only).

.PARAMETER Slice
    Slice name under modernization/. Defaults to "feed".

.EXAMPLE
    pwsh scripts/gates/03-check-rules-have-tests.ps1
    pwsh scripts/gates/03-check-rules-have-tests.ps1 -Slice feed
#>
param(
    [string]$Slice = "feed"
)
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Push-Location $repoRoot
try {
    $rulesPath = "modernization/$Slice/RULES.md"
    if (-not (Test-Path $rulesPath)) {
        Write-Error "No $rulesPath yet - this gate fails clean until ASOP step 2 (extract-business-rules) writes it."
        exit 1
    }

    # `uv run python3` rather than a bare `python`/`python3` call: uv finds
    # or installs a matching interpreter itself, so this doesn't depend on
    # how (or whether) Python is already on PATH - see RUNBOOK-WINDOWS.md's
    # prerequisites.
    uv run python3 scripts/check-rules-have-tests.py $rulesPath `
        "source/newtelligence.DasBlog.Web.Services/Test/FeedCharacterization" `
        "src-modern/DasBlog.Feed.Tests"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "check-rules-have-tests.py exited $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    Write-Host "Gate PASSED: every in-scope rule has a referencing test" -ForegroundColor Green
}
finally {
    Pop-Location
}
