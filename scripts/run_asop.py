#!/usr/bin/env python3
"""Walk an ASOP's steps and (optionally) execute them.

A THIN runner, deliberately not the harness's own ASOP store
(agentco_harness.asop_store) — see harness/ (git submodule) and
RUNBOOK-WINDOWS.md's "Install the Agentic Co Harness on Windows" section
for why: agentco_harness.asop_store (and agentco_harness.beads, which
cli.py imports unconditionally at module load) `import fcntl` at module
level, and fcntl does not exist on native Windows Python. The harness CLI
(`agentic-co ...`) cannot even be imported there, let alone drive a run —
confirmed by reading the source, not assumed. Inside WSL2 the harness's
own `agentic-co sop run` would be the more correct tool; this script is
what runs on native Windows PowerShell in the meantime, and says so.

What this script actually does, per step, in ASOP order:

  1. Print the step's full text (purpose, entry_check, inputs,
     definition_of_done, validation, common_mistakes) — this is the
     "instructions" a human OR an executor needs before starting.
  2. If the step's gate is `human`: STOP and wait for an interactive
     confirmation. This script does not simulate a human gate — seed
     asop/runs/feed/v1/ADJUDICATION.md item 1 (this POC's own executor
     proceeding past an unanswered human gate) is exactly the mistake this
     behavior exists to not repeat.
  3. If the step's gate is `deterministic` and --execute was passed:
     dispatch the step to `claude -p` (Claude Code CLI, non-interactive)
     with the step's text as the prompt, in the repo root, then run the
     step's gate script from scripts/gates/ and record the exit code.
     Without --execute (the default), the step is described but not run —
     use --dry-run explicitly, or just omit --execute, to preview a plan
     without touching the repo or spending any model budget.
  4. Append one JSON record per step to
     asop/runs/<slice>/v<version>/RUN-LOG.jsonl: step, name, role,
     executor, gate command, exit code, timestamp. This is the run record
     asop/README.md's "how a run is actually recorded" describes — evidence,
     not a claim.

Usage:
    run_asop.py --asop asop/strangler-slice-a-feature/v1.yaml --slice feed [--execute] [--from-step N] [--to-step N]
    run_asop.py --asop asop/strangler-slice-a-feature/v1.yaml --slice feed --dry-run
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

try:
    import yaml
except ImportError:
    print(
        "error: PyYAML is required. Run this script via uv so it's "
        "installed automatically:\n"
        "  uv run --with pyyaml python3 scripts/run_asop.py ...\n"
        "or `pip install pyyaml` into your own environment first.",
        file=sys.stderr,
    )
    sys.exit(2)

REPO_ROOT = Path(__file__).resolve().parent.parent

# Maps each step's gate to the local gate script that actually checks it -
# a run of THIS script, not a claim the step's own `check:` text in the
# YAML always maps 1:1 to a script name (some, like the human gates, have
# no script at all by design).
STEP_GATE_SCRIPTS: dict[int, str | None] = {
    1: None,  # human gate - no script, see step 2 below
    2: "scripts/gates/03-check-rules-have-tests.ps1",
    3: "scripts/gates/02-legacy-test.ps1",
    4: "scripts/gates/04-golden-diff.ps1",
    5: "scripts/gates/06-facade.ps1",
    6: None,  # human gate
}


def load_asop(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    # Strip nothing special - the body IS the YAML; identity fields
    # (asop_id/version/status/author) live in a leading '#'-comment block
    # per ASOP.md §3.1 and this repo's own asop/README.md convention, and
    # YAML comments are already ignored by the parser.
    return yaml.safe_load(text)


def run_gate(step_num: int, dry_run: bool) -> tuple[str | None, int | None]:
    script = STEP_GATE_SCRIPTS.get(step_num)
    if script is None:
        return None, None
    if dry_run:
        return script, None
    cmd = ["pwsh", "-File", str(REPO_ROOT / script)]
    print(f"$ {' '.join(cmd)}")
    result = subprocess.run(cmd, cwd=REPO_ROOT)
    return script, result.returncode


def dispatch_to_claude(step_num: int, step: dict, execute: bool) -> tuple[str, int | None]:
    prompt_parts = [
        f"ASOP step {step_num} — {step['name']} (role: {step.get('role', '?')})",
        "",
        f"Purpose: {step.get('purpose', '')}",
        f"Entry check: {step.get('entry_check', '')}",
        f"Inputs: {step.get('inputs', '')}",
        f"Definition of done: {step.get('definition_of_done', '')}",
        f"Validation: {step.get('validation', '')}",
    ]
    if step.get("common_mistakes"):
        prompt_parts.append("Common mistakes to avoid:")
        prompt_parts.extend(f"- {m}" for m in step["common_mistakes"])
    prompt = "\n".join(prompt_parts)

    if not execute:
        return prompt, None

    cmd = ["claude", "-p", prompt]
    print(f"$ claude -p <step {step_num} instructions, {len(prompt)} chars>")
    result = subprocess.run(cmd, cwd=REPO_ROOT)
    return prompt, result.returncode


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--asop", required=True, type=Path, help="Path to the ASOP YAML file")
    parser.add_argument("--slice", required=True, help="Slice name, e.g. feed")
    parser.add_argument("--version", type=int, default=1, help="ASOP version being run (default 1)")
    parser.add_argument("--execute", action="store_true", help="Actually dispatch steps to `claude -p` and run gates. Without this, steps are described only.")
    parser.add_argument("--dry-run", action="store_true", help="Print the plan (steps + gate scripts) and exit. Overrides --execute.")
    parser.add_argument("--from-step", type=int, default=1)
    parser.add_argument("--to-step", type=int, default=None)
    args = parser.parse_args()

    asop = load_asop(args.asop)
    steps = asop["steps"]
    to_step = args.to_step or len(steps)

    run_dir = REPO_ROOT / "asop" / "runs" / args.slice / f"v{args.version}"
    run_dir.mkdir(parents=True, exist_ok=True)
    log_path = run_dir / "RUN-LOG.jsonl"

    execute = args.execute and not args.dry_run

    print(f"ASOP: {asop['title']} (from {args.asop})")
    print(f"Slice: {args.slice}  |  execute={execute}  dry_run={args.dry_run}")
    print(f"Run log: {log_path}")
    print("=" * 72)

    for position, step in enumerate(steps, start=1):
        # Step numbers are POSITIONAL, not a literal `step:` field - per
        # ASOP.md §3.2/§3.4 and this repo's own asop/sop.py validate_step:
        # "the position is the number; omit the key." v1.yaml follows that
        # convention, so this script must too.
        n = step.get("step", position)
        if n < args.from_step or n > to_step:
            continue

        print(f"\n--- Step {n}: {step['name']} (role: {step.get('role', '?')}) ---")
        print(f"purpose: {step.get('purpose', '').strip()}")
        gate = step.get("gate", {})
        gate_kind = gate.get("kind", "?")
        print(f"gate: kind={gate_kind}")

        record = {
            "step": n,
            "name": step["name"],
            "role": step.get("role"),
            "gate_kind": gate_kind,
            "at": datetime.now(timezone.utc).isoformat(),
        }

        if gate_kind == "human":
            verifier = gate.get("verifier", "a human")
            print(f"\n*** HUMAN GATE — verifier: {verifier} ***")
            print(f"check: {gate.get('check', '').strip()}")
            if args.dry_run:
                record["executor"] = None
                record["result"] = "dry-run: not executed"
            else:
                print(
                    "\nThis run STOPS here until a human answers this gate — "
                    "see asop/runs/feed/v1/ADJUDICATION.md item 1 for why "
                    "this script refuses to simulate that answer."
                )
                answer = input(f"Type 'approved' if {verifier} approves this step, anything else to stop: ")
                record["human_answer"] = answer
                if answer.strip().lower() != "approved":
                    record["result"] = "stopped: human gate not approved"
                    _append_log(log_path, record)
                    print("Stopping run (human gate not approved).")
                    return 1
                record["result"] = "approved"
            _append_log(log_path, record)
            continue

        # deterministic (or judged, treated the same here - no judge route
        # is wired in this thin runner; a judged gate just runs its check
        # like a deterministic one and the result should be read as
        # self-attested, same disclosed limitation as asop/README.md)
        prompt, claude_exit = dispatch_to_claude(n, step, execute)
        record["executor"] = "claude -p" if execute else None
        record["claude_exit_code"] = claude_exit

        gate_script, gate_exit = run_gate(n, args.dry_run or not execute)
        record["gate_script"] = gate_script
        record["gate_exit_code"] = gate_exit
        if args.dry_run:
            record["result"] = "dry-run: not executed"
        elif not execute:
            record["result"] = "described only (pass --execute to run)"
        else:
            record["result"] = "gate PASSED" if gate_exit == 0 else f"gate FAILED (exit {gate_exit})"
            print(record["result"])

        _append_log(log_path, record)

        if execute and gate_exit not in (None, 0):
            print(f"\nStopping run: step {n}'s gate failed (exit {gate_exit}).")
            return 1

    print("\n" + "=" * 72)
    print(f"Done. Run log: {log_path}")
    return 0


def _append_log(log_path: Path, record: dict) -> None:
    with log_path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(record) + "\n")


if __name__ == "__main__":
    sys.exit(main())
