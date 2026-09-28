<#
.SYNOPSIS
    ASOP step 4 gate: the .NET 10 port matches modernization/<slice>/golden/
    byte-for-byte (via dotnet test).

.DESCRIPTION
    Thin wrapper around scripts/golden-diff.py, which runs `dotnet test`
    for the given project. See that script's docstring for why there's no
    separate legacy-vs-modern diff step: both this gate and
    02-legacy-test.ps1 assert against the SAME golden files independently,
    which proves legacy==modern by transitivity when both are green.

.PARAMETER Project
    Path to the .NET test project. Defaults to the feed slice's.

.EXAMPLE
    pwsh scripts/gates/04-golden-diff.ps1
#>
param(
    [string]$Project = "src-modern/DasBlog.Feed.Tests/DasBlog.Feed.Tests.csproj"
)
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Push-Location $repoRoot
try {
    if (-not (Test-Path $Project)) {
        Write-Error "No $Project yet - this gate fails clean until ASOP step 4 (implement-on-dotnet-10) writes it."
        exit 1
    }

    # uv run python3, not a bare python/python3 call - see 03's comment.
    uv run python3 scripts/golden-diff.py $Project

    if ($LASTEXITCODE -ne 0) {
        Write-Error "golden-diff.py exited $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    Write-Host "Gate PASSED: .NET 10 port matches golden files byte-for-byte" -ForegroundColor Green
}
finally {
    Pop-Location
}
