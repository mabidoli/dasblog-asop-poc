<#
.SYNOPSIS
    ASOP step 3 gate: run the legacy test suite and the feed
    characterization tests, exactly as .github/workflows/legacy.yml does.

.DESCRIPTION
    Uses the NUnit 2.6.4 console runner (downloaded from nuget.org, matching
    the vendored lib/nunit.framework.dll these test projects compile
    against), NOT vstest.console.exe - there is no NUnit-2-compatible
    VSTest adapter configured in this repo, and the CI workflow's own
    "iterate until green" history (asop/runs/feed/v1/EVIDENCE.md,
    iterations 5-14) is all against this runner. Swapping to vstest would
    be new, unverified surface; this script mirrors what's actually proven.

    /noshadow is required - without it, nunit-console shadow-copies the
    test assembly to a temp directory, and the feed characterization
    tests' repo-root path resolution (Assembly.GetExecutingAssembly().Location)
    breaks. /exclude:RequiresLiveServer skips the two tests that need a
    live IIS deployment (out of scope - see SLICE-MAP.md).

.EXAMPLE
    pwsh scripts/gates/02-legacy-test.ps1
#>
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Push-Location $repoRoot
try {
    $runnerDir = Join-Path $repoRoot "nunit.runners"
    $runnerExe = Join-Path $runnerDir "tools/nunit-console.exe"

    if (-not (Test-Path $runnerExe)) {
        Write-Host "Downloading NUnit 2.6.4 console runner..."
        $zipPath = Join-Path $repoRoot "nunit.runners.zip"
        Invoke-WebRequest -Uri "https://www.nuget.org/api/v2/package/NUnit.Runners/2.6.4" -OutFile $zipPath
        Expand-Archive -Path $zipPath -DestinationPath $runnerDir -Force
    }

    $dlls = Get-ChildItem -Path source -Recurse -Include *.Test.dll, *.test.dll |
        Where-Object { $_.FullName -match '\\Test\\bin\\' } |
        Select-Object -ExpandProperty FullName -Unique

    if (-not $dlls) {
        Write-Error "No test assemblies found under source/**/Test/bin/ - run 01-legacy-build.ps1 first."
        exit 1
    }

    Write-Host "Test assemblies:"
    $dlls | ForEach-Object { Write-Host " - $_" }

    & $runnerExe $dlls /result:TestResult.xml /nologo /exclude:RequiresLiveServer /noshadow

    if ($LASTEXITCODE -ne 0) {
        Write-Error "nunit-console exited $LASTEXITCODE - see TestResult.xml"
        exit $LASTEXITCODE
    }
    Write-Host "Gate PASSED: legacy test suite (incl. feed characterization tests)" -ForegroundColor Green
}
finally {
    Pop-Location
}
