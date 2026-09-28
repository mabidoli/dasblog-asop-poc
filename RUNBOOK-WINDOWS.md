# Windows runbook

Everything you need to clone this repo onto a Windows machine and run ASOP
v1 (`asop/strangler-slice-a-feature/v1.yaml`) against slice 1 (RSS 2.0
feed generation, `modernization/feed/`) locally — no GitHub Actions
required, though `.github/workflows/legacy.yml`/`modern.yml` stay as a
secondary cross-check if you want it.

**Read this whole document before running anything** — in particular the
"Native Windows Python cannot run the Harness CLI" box in the harness
section below. It's the single most important fact here.

## Clone

```powershell
git clone --recursive https://github.com/mabidoli/dasblog-asop-poc.git
cd dasblog-asop-poc
```

`--recursive` matters: `harness/` is a git submodule
(https://github.com/agentic-co/agentic-co-harness, pinned to a commit —
see `.gitmodules`), and a plain `git clone` leaves it empty. If you already
cloned without `--recursive`:

```powershell
git submodule update --init --recursive
```

## Prerequisites

All commands are PowerShell (`pwsh`, not `powershell.exe` — install
PowerShell 7 first if you're on an older Windows image; it's what the
`.github/workflows/*.yml` runners use too, so behavior matches).

```powershell
# Git (if not already present)
winget install --id Git.Git -e

# Visual Studio 2022 Build Tools, with the .NET Framework 4.8 targeting
# pack AND the legacy MSBuild toolset - this repo's csproj files are
# VS2010-era format, TargetFrameworkVersion v4.5.2/v4.8 (see
# asop/runs/feed/v1/EVIDENCE.md for exactly why v4.0/v4.5 as originally
# written do NOT work: the matching reference assemblies aren't installed
# by default even on a fresh Windows image, and had to be bumped).
winget install --id Microsoft.VisualStudio.2022.BuildTools -e --override `
  "--quiet --wait --add Microsoft.VisualStudio.Workload.MSBuildTools --add Microsoft.Net.Component.4.8.TargetingPack --add Microsoft.Net.Component.4.8.SDK"

# .NET 10 SDK (for src-modern/)
winget install --id Microsoft.DotNet.SDK.10 -e
# or, if that winget id isn't published yet on your machine's winget source:
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
.\dotnet-install.ps1 -Channel 10.0
# adds to $env:USERPROFILE\.dotnet - add that to PATH for this session:
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"

# Python 3.11+ and uv (for scripts/*.py and scripts/run-asop.ps1)
winget install --id astral-sh.uv -e
# uv can also install Python itself if you don't have 3.11+:
uv python install 3.12

# Node + Claude Code CLI
winget install --id OpenJS.NodeJS.LTS -e
npm install -g @anthropic-ai/claude-code
claude login   # interactive OAuth - do this once
```

Verify:

```powershell
git --version
msbuild -version          # from a "Developer PowerShell for VS 2022" prompt, or after Build Tools' env setup
dotnet --version           # should print 10.0.x
uv --version
python3 --version          # or `uv run python3 --version`
node --version
claude --version
```

`msbuild` specifically needs the **Developer PowerShell for VS 2022**
shortcut (Start Menu, installed with Build Tools) or you need to run
`Import-Module` on its `VsDevCmd.bat`/`Enter-VsDevShell` — a plain
PowerShell window does not have `msbuild` on PATH by default.

## Install the Agentic Co Harness on Windows

> **Native Windows Python cannot run the Harness CLI at all.** Confirmed
> by reading the source, not assumed: `agentco_harness/beads.py`,
> `asop_store.py`, `natural_key.py`, `schedules.py`, and `recurring.py` all
> `import fcntl` unconditionally at module top level, and `cli.py`
> (the `agentic-co` console script's entry point) imports `beads` and
> `asop_store` unconditionally too. `fcntl` is POSIX-only — it does not
> exist in Windows's standard library, on CPython or any other
> implementation. The result: `agentic-co --help`, `agentic-co doctor`,
> every single subcommand, fails at IMPORT time with
> `ModuleNotFoundError: No module named 'fcntl'`, before any of its own
> logic runs. This is not a missing-dependency problem `pip install`
> fixes — it's a hard platform gap in the harness itself as of the
> pinned commit (`c9451c1`, see `.gitmodules`).
>
> **Workaround: WSL2.** Everything below this box works inside WSL2
> (Windows Subsystem for Linux — a real Linux kernel, so `fcntl` exists).
> The gate scripts (`scripts/gates/*.ps1`, msbuild, the NUnit/dotnet
> toolchains) need native Windows regardless (`.NET Framework` and
> Visual Studio Build Tools aren't available in WSL2's Linux userspace);
> the harness's own CLI, if you want to actually use `agentic-co sop run`
> instead of this repo's thin `scripts/run_asop.py`, needs WSL2 instead.
> These are two different environments for two different jobs on the same
> machine — not a contradiction, just the actual shape of what each tool
> needs.

### Install (inside WSL2, or any real POSIX shell)

```bash
# from the repo root, inside WSL2:
cd harness   # the submodule

# Runtime + dev deps (tests, the DSPy test double):
uv venv
uv pip install -e ".[dev]"

# Or runtime-only, no in-process model needed:
uv pip install -e "."

# Or as a standalone CLI tool, not tied to this repo's venv:
uv tool install .
# from the submodule's pinned commit directly (works from the parent
# repo's clone URL + subdirectory too, no local checkout needed):
uv tool install "git+https://github.com/mabidoli/dasblog-asop-poc.git#subdirectory=harness"
```

Console script is **`agentic-co`**, not `harness` — an earlier planning
assumption elsewhere named it `harness`; `pyproject.toml`'s
`[project.scripts]` says `agentic-co = "agentco_harness.cli:main"`,
confirmed by reading it directly.

### Init a local node for this repo

```bash
# inside WSL2, from the repo root:
agentic-co init
```

Creates `config.yaml` + `tasks.jsonl` in the current directory — the
harness's "node" concept is per-directory, not global. `init` is additive
(an existing `config.yaml` is never silently overwritten; pass `--force`
to reset it on purpose).

### Verify

```bash
agentic-co --help
agentic-co doctor            # preflight, classified by consequence, exit 0/1/2
agentic-co doctor --json     # machine-readable

# A pytest subset (the full suite needs the fake-LM test double and takes
# longer than a quick sanity check needs):
cd harness
uv run pytest -q tests/test_doctor.py tests/test_config.py tests/test_extension_seams.py
```

### Config/state location

Everything lives relative to wherever you ran `agentic-co init` —
`config.yaml`, `tasks.jsonl` (the append-only bead store), `asops.jsonl`
(procedures, if you use `agentic-co sop create/activate/run` instead of
this repo's `scripts/run_asop.py`), `recurring.jsonl` if you used
`--portfolio`. Nothing is written outside that directory tree, and nothing
is global/per-user — a second `agentic-co init` in a different directory
is a completely separate node.

### Other POSIX-only surface, beyond `fcntl`

- `scripts/two-machine/agentco-pull-forced-command.sh`,
  `scripts/eval/run_arms.sh`, `scripts/eval/queue_retail_v6_powered.sh` —
  bash scripts, not relevant to running an ASOP locally, only to the
  two-machine SSH lane and the eval harness. Unsupported natively on
  Windows; run them inside WSL2 if you ever need them.
- No `launchd` dependency found in `agentco_harness/` itself (it appears
  only in test names/docstrings describing a *different* LifeOS
  component's incident history, not something this package invokes) — not
  a blocker.
- Path handling in `agentco_harness/*.py` uses `pathlib.Path` throughout
  where checked, which is cross-platform-correct; the `fcntl` import is
  the actual, sole hard blocker found in this pass, not a broader
  path-separator problem.

## Running ASOP v1 against slice 1

### Option A — this repo's thin runner (works natively on Windows, no WSL2)

```powershell
# Preview the plan, no side effects:
pwsh scripts/run-asop.ps1 -DryRun

# Describe each step without executing (writes asop/runs/feed/v1/RUN-LOG.jsonl
# of "described only" entries; calls nothing):
pwsh scripts/run-asop.ps1

# The real thing: dispatches each step to `claude -p`, runs that step's
# gate script (scripts/gates/*.ps1), pauses at human gates (steps 1 and 6)
# for YOU to type 'approved':
pwsh scripts/run-asop.ps1 -Execute
```

See `scripts/run_asop.py`'s own docstring for exactly what it does and
does not do — it is deliberately thin (does not use the harness's
`agentco_harness.asop_store`, for the `fcntl` reason above) and says so.

### Option B — the harness's own ASOP store (WSL2 only)

```bash
cd harness
agentic-co sop create ../asop/strangler-slice-a-feature/v1.yaml
agentic-co sop activate strangler-slice-a-feature 1
agentic-co sop run strangler-slice-a-feature \
  --input repo=.. --input slice=feed \
  --input legacy_ci_workflow=legacy.yml --input modern_ci_workflow=modern.yml \
  --bind mapper=claude --bind extractor=claude --bind legacy_tester=claude \
  --bind modernizer=claude --bind integrator=claude
```

Untested by this session (packaging work only, per instructions — see
`PLAN.md`'s stop criteria). If you try this path, the gate `check:`
commands in `v1.yaml` that shell out to `gh run list --workflow=...` won't
have anything to check locally unless you also push to GitHub and let
`.github/workflows/legacy.yml`/`modern.yml` run — Option A's
`scripts/gates/*.ps1` are the local-only equivalents for exactly this
reason.

### Individual gates, standalone

```powershell
pwsh scripts/gates/01-legacy-build.ps1          # msbuild source/DasBlog.sln
pwsh scripts/gates/02-legacy-test.ps1           # NUnit 2.6.4, /exclude:RequiresLiveServer /noshadow
pwsh scripts/gates/03-check-rules-have-tests.ps1  # step 2's gate
pwsh scripts/gates/04-golden-diff.ps1           # step 4's gate (dotnet test vs golden files)
pwsh scripts/gates/05-modern-build-test.ps1     # plain dotnet build+test, no golden semantics
pwsh scripts/gates/06-facade.ps1                # step 5's gate: runs 02 + 04 together
```

### Where results land

- `asop/runs/feed/v1/RUN-LOG.jsonl` — one JSON line per step from
  `scripts/run-asop.ps1`/`run_asop.py`.
- `asop/runs/feed/v1/EVIDENCE.md`, `ADJUDICATION.md` — the narrative
  record from this session's own CI-based run of v1 (see below — this
  already happened once, on GitHub Actions, before this Windows packaging
  work; see the PR).
- `TestResult.xml` (repo root) — NUnit's own XML report from
  `02-legacy-test.ps1`.
- PR: <https://github.com/mabidoli/dasblog-asop-poc/pull/1> — the existing
  step-6 evidence PR from the CI-based run. A Windows-based re-run doesn't
  need a second PR unless you're producing a genuinely new version (v2+).

## How to adjudicate and produce v2

1. Read `asop/runs/feed/v1/ADJUDICATION.md` — it already has two concrete
   proposals from this session's run (soften step 2's gate path text; make
   step 1's human-gate-blocks-execution rule explicit, or give it
   park-and-continue semantics). Confirm or revise them against whatever
   your own run surfaces.
2. Copy `asop/strangler-slice-a-feature/v1.yaml` to `v2.yaml`, apply the
   changes `ADJUDICATION.md` proposes (and any new ones from your run —
   a step split, a step added, a reordering, per ASOP.md §6.3).
3. Re-validate against the contract before running it:
   ```bash
   cd harness   # or wherever the `asop` package is importable from
   uv run --with pyyaml python3 -c "
   import yaml
   from asop.sop import validate_asop
   doc = yaml.safe_load(open('../asop/strangler-slice-a-feature/v2.yaml').read())
   print(validate_asop(doc))
   "
   ```
   (Note: this validates against the `asop` contract package, which is
   separate from the harness's own `agentco_harness` package — see
   `asop/README.md` for where `asop.sop` actually lives if it's not
   bundled in this submodule.)
4. Run v2 the same way as v1 (Option A or B above), pointing
   `scripts/run-asop.ps1 -Asop asop\strangler-slice-a-feature\v2.yaml` at
   the new file.
5. Write `asop/runs/feed/v2/EVIDENCE.md` and `ADJUDICATION.md` the same
   shape as v1's.

## Stop criteria

- **A version is "done" enough to stop iterating** when a run completes
  all 6 steps with zero BAD divergences (per `ADJUDICATION.md`'s own
  good/bad split) and every deterministic gate green — OR after v4,
  whichever comes first (per the original task brief's own budget).
- **Stop and escalate to mabidoli**, don't keep iterating past it, if: a
  gate script needs a Windows component this runbook doesn't cover and
  installing it isn't a `winget`/`uv` one-liner; a step's `check:` needs
  something this thin runner structurally can't do (e.g. a genuinely
  judged gate needing a distinct judge route — this runner has no judge
  route, see `run_asop.py`'s docstring); or the human gates keep getting
  answered "not approved" for the same reason twice — that's a v1/v2
  problem, not a "try again" problem.
