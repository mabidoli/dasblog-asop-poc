<#
.SYNOPSIS
    Walk asop/strangler-slice-a-feature/v1.yaml's steps, on Windows.

.DESCRIPTION
    Thin PowerShell wrapper around scripts/run_asop.py (the actual logic -
    see that file's docstring for why this is a standalone script and not
    a call into the harness submodule's own ASOP store:
    agentco_harness.asop_store imports `fcntl` unconditionally, which does
    not exist on native Windows Python - see RUNBOOK-WINDOWS.md).

    Runs the Python script via `uv run --with pyyaml`, which installs
    PyYAML into an ephemeral environment automatically - no manual venv
    setup needed, as long as `uv` is on PATH (see RUNBOOK-WINDOWS.md's
    prerequisites section).

.PARAMETER Slice
    Slice name under modernization/ and asop/runs/. Default: feed.

.PARAMETER Asop
    Path to the ASOP YAML. Default: asop/strangler-slice-a-feature/v1.yaml.

.PARAMETER Execute
    Actually dispatch steps to `claude -p` and run each step's gate
    script. Without this switch, steps are described only (safe to run
    with no side effects other than a RUN-LOG.jsonl of "described only"
    entries).

.PARAMETER DryRun
    Print the plan only - no RUN-LOG.jsonl side effects at all. Overrides
    -Execute.

.EXAMPLE
    # Preview the plan, no side effects:
    pwsh scripts/run-asop.ps1 -DryRun

.EXAMPLE
    # Describe each step without executing (writes a RUN-LOG.jsonl of
    # "described only" entries, does not call claude or any gate):
    pwsh scripts/run-asop.ps1

.EXAMPLE
    # The real thing - dispatches each step to Claude Code, runs its gate,
    # pauses at human gates for YOU to type 'approved':
    pwsh scripts/run-asop.ps1 -Execute
#>
param(
    [string]$Slice = "feed",
    [string]$Asop = "asop/strangler-slice-a-feature/v1.yaml",
    [switch]$Execute,
    [switch]$DryRun,
    [int]$FromStep = 1,
    [int]$ToStep
)
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot
try {
    $pyArgs = @(
        "run", "--with", "pyyaml", "python3", "scripts/run_asop.py",
        "--asop", $Asop,
        "--slice", $Slice,
        "--from-step", $FromStep
    )
    if ($ToStep) { $pyArgs += @("--to-step", $ToStep) }
    if ($Execute) { $pyArgs += "--execute" }
    if ($DryRun) { $pyArgs += "--dry-run" }

    uv @pyArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
