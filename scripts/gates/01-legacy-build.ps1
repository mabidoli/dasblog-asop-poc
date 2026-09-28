<#
.SYNOPSIS
    ASOP step 3 (characterize-the-legacy-behaviour) prerequisite: build the
    legacy .NET Framework solution. Mirrors .github/workflows/legacy.yml's
    "Build source/DasBlog.sln" step, for a machine that already has the
    right SDKs installed (see ../../RUNBOOK-WINDOWS.md) instead of relying
    on windows-latest.

.DESCRIPTION
    No NuGet restore step: every third-party dependency in this solution is
    a vendored DLL under lib/, referenced by HintPath - same as the CI
    workflow.

    TargetFrameworkVersion is v4.5.2/v4.8 across the projects that need it
    (bumped during this POC's Phase 0 - see asop/runs/feed/v1/EVIDENCE.md
    for why v4.0/v4.5 don't work even on a fresh Windows install: the
    matching REFERENCE ASSEMBLIES, not just the runtime, have to be
    present, and Visual Studio's .NET Framework 4.8 targeting pack is what
    RUNBOOK-WINDOWS.md tells you to install).

.EXAMPLE
    pwsh scripts/gates/01-legacy-build.ps1
#>
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Push-Location $repoRoot
try {
    msbuild "source/DasBlog.sln" `
        /p:Configuration=Debug `
        /p:Platform="Any CPU" `
        /m:1 `
        /nologo `
        /v:minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Error "msbuild exited $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    Write-Host "Gate PASSED: legacy build (source/DasBlog.sln)" -ForegroundColor Green
}
finally {
    Pop-Location
}
