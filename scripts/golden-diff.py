#!/usr/bin/env python3
"""ASOP step 4 gate (asop/strangler-slice-a-feature/v1.yaml): "the SAME
characterization tests pass on the new build, plus golden-file/output diff
= 0".

Design note (see asop/README.md): rather than re-implementing a third,
independent legacy-vs-modern text diff, this script leans on the fact that
BOTH sides already assert against the SAME committed golden files under
modernization/<slice>/golden/ - the legacy characterization tests
(source/.../Test/FeedCharacterization/) and the .NET 10 characterization
tests (src-modern/<Slice>.Tests/) each do a byte-for-byte AreEqual/Equal
against those files. legacy==golden (legacy.yml) AND modern==golden (this
script's dotnet test run) together prove legacy==modern by transitivity -
a stronger property than a one-off pairwise diff, because it's re-checked
by BOTH CI gates independently rather than computed once here and trusted.

This script is the "modern" half: it runs `dotnet test` for the given
project and reports pass/fail. It does NOT re-run the legacy side (that's
legacy.yml's job, on windows-latest, which this Mac cannot build) - it
trusts legacy.yml's own result, named explicitly rather than silently
assumed. See asop/runs/feed/v1/EVIDENCE.md for both sides' actual run
evidence.

Usage:
    golden-diff.py <path-to-dotnet-test-project-or-sln> [-- <extra dotnet test args>]

Exit code 0 iff `dotnet test` reports zero failures.
"""
import subprocess
import sys


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__)
        return 2

    target = argv[1]
    extra_args = argv[2:]
    if extra_args and extra_args[0] == "--":
        extra_args = extra_args[1:]

    cmd = ["dotnet", "test", target, "--nologo"] + extra_args
    print(f"$ {' '.join(cmd)}")
    result = subprocess.run(cmd)

    if result.returncode != 0:
        print(
            f"\nFAIL: `dotnet test {target}` exited {result.returncode}. "
            f"This is the modern-side half of the golden-file diff gate - "
            f"a failure here means the .NET 10 port diverged from "
            f"modernization/<slice>/golden/*.xml (the SAME files the "
            f"legacy characterization tests assert against). Re-run "
            f"`dotnet test {target}` locally to see which fixture and "
            f"what the actual output was.",
            file=sys.stderr,
        )
        return 1

    print(
        f"\nOK: {target} passed against the committed golden files. "
        f"Combined with a green legacy.yml run at this commit (see "
        f"asop/runs/<slice>/v1/EVIDENCE.md), legacy and modern output are "
        f"proven byte-identical by transitivity through the shared golden "
        f"files."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
