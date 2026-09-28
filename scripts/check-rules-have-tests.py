#!/usr/bin/env python3
"""ASOP step 2 gate (asop/strangler-slice-a-feature/v1.yaml): every
IN-SCOPE rule in a slice's RULES.md must be referenced by at least one
characterization test. A rule nobody wrote a test for is unverified, not
extracted.

Usage:
    check-rules-have-tests.py <RULES.md> <test-dir> [<test-dir> ...]

<RULES.md>  the rules catalog. In-scope rules are lines matching
            "**RULE-<slice>-<N>**" (bold, no letter suffix). Deferred rules
            ("**RULE-<slice>-D<N>**") are read but never required to have a
            test - they're deferred on purpose, and RULES.md says so.

<test-dir>  one or more directories to search (recursively) for source
            files that reference a rule ID as a literal string, e.g. a
            comment tagging which rules a test method exercises. The ASOP's
            own gate text says "modernization/<slice>/tests-legacy"; for
            this repo the actual characterization tests live inside the
            legacy solution's own test project tree instead (they have to,
            to compile and run against the legacy build) - see
            asop/runs/feed/v1/EVIDENCE.md for that substitution, disclosed
            per asop/README.md's "how a run is actually recorded" section.

Exit code 0 iff every in-scope rule ID is referenced at least once.
"""
import re
import sys
from pathlib import Path

IN_SCOPE_RE = re.compile(r"\*\*(RULE-[A-Za-z0-9]+-\d+)\*\*")
DEFERRED_RE = re.compile(r"\*\*(RULE-[A-Za-z0-9]+-D\d+)\*\*")

SEARCHABLE_SUFFIXES = {
    ".cs", ".py", ".md", ".yaml", ".yml", ".ts", ".js", ".fs", ".txt",
}


def extract_rule_ids(rules_md: Path) -> tuple[list[str], list[str]]:
    text = rules_md.read_text(encoding="utf-8")
    deferred = set(DEFERRED_RE.findall(text))
    in_scope = [rid for rid in IN_SCOPE_RE.findall(text) if rid not in deferred]
    # De-dupe, keep first-seen order for readable output.
    seen = []
    for rid in in_scope:
        if rid not in seen:
            seen.append(rid)
    return seen, sorted(deferred)


def collect_test_text(test_dirs: list[Path]) -> str:
    chunks = []
    for d in test_dirs:
        if not d.exists():
            continue
        for p in d.rglob("*"):
            if p.is_file() and p.suffix in SEARCHABLE_SUFFIXES:
                try:
                    chunks.append(p.read_text(encoding="utf-8", errors="ignore"))
                except OSError:
                    continue
    return "\n".join(chunks)


def main(argv: list[str]) -> int:
    if len(argv) < 3:
        print(__doc__)
        return 2

    rules_md = Path(argv[1])
    test_dirs = [Path(a) for a in argv[2:]]

    if not rules_md.exists():
        print(f"error: {rules_md} does not exist", file=sys.stderr)
        return 2

    in_scope, deferred = extract_rule_ids(rules_md)
    if not in_scope:
        print(f"error: no in-scope RULE-* ids found in {rules_md}", file=sys.stderr)
        return 2

    haystack = collect_test_text(test_dirs)

    missing = [rid for rid in in_scope if rid not in haystack]

    print(f"{rules_md}: {len(in_scope)} in-scope rule(s), {len(deferred)} deferred")
    for rid in in_scope:
        mark = "MISSING" if rid in missing else "ok"
        print(f"  [{mark}] {rid}")

    if missing:
        print(
            f"\nFAIL: {len(missing)} in-scope rule(s) have no referencing "
            f"test under {', '.join(str(d) for d in test_dirs)}: "
            f"{', '.join(missing)}",
            file=sys.stderr,
        )
        return 1

    print(f"\nOK: every in-scope rule is referenced by a test under {', '.join(str(d) for d in test_dirs)}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
