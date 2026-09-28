<#
.SYNOPSIS
    Build and test the .NET 10 port on its own (no golden-file comparison
    semantics - see 04-golden-diff.ps1 for the actual ASOP step 4 gate).
    Useful as a fast local iteration loop while writing modern code, since
    it's a plain `dotnet build` + `dotnet test` with normal (not golden-diff)
    output.

.EXAMPLE
    pwsh scripts/gates/05-modern-build-test.ps1
#>
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Push-Location $repoRoot
try {
    dotnet build src-modern/DasBlog.Feed/DasBlog.Feed.csproj --nologo
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet build failed"; exit $LASTEXITCODE }

    dotnet test src-modern/DasBlog.Feed.Tests/DasBlog.Feed.Tests.csproj --nologo
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet test failed"; exit $LASTEXITCODE }

    Write-Host "Gate PASSED: .NET 10 port builds and its own test suite passes" -ForegroundColor Green
}
finally {
    Pop-Location
}
