# asop/ — the modernization procedure and its run records

This directory holds the one hand-authored ASOP this POC runs against every
slice, and the evidence from each run. It does **not** hold a generated ASOP
per codebase — see `linkedin/content/ASOP-MODERNIZATION-REVIEW.md` (in the
`personal-brand` repo) for why: our own τ²-bench eval found that generating
a fresh procedure per codebase is the expensive, unreliable part, while the
step machinery itself is close to free. One procedure, versioned, run
slice by slice, revised from real divergence.

## What's here

- `strangler-slice-a-feature/v1.yaml` — the procedure. Six steps: map the
  slice, extract business rules, characterize the legacy behaviour, implement
  on .NET 10, build a facade, review and open a PR. Conforms to
  [ASOP.md v3.4](https://github.com/mabidoli/asop)'s record and step shape
  (§3.1–§3.2), validated against `asop.sop.validate_asop` from that repo:

  ```
  $ uv run --with pyyaml python3 -c "
  import yaml
  from asop.sop import validate_asop
  doc = yaml.safe_load(open('asop/strangler-slice-a-feature/v1.yaml').read())
  print(validate_asop(doc))
  "
  VALID — 6 steps, roles: mapper, extractor, legacy_tester, modernizer, integrator
  ```

  (run from a checkout of `asop` itself, since that's where the `asop`
  Python package lives — this repo depends on it only as a validator, not
  at runtime)

- `runs/<slice>/v<N>/` — one directory per run of one ASOP version against
  one slice. Each holds:
  - `EVIDENCE.md` — per step: who/what executed it, the gate's actual
    check command or PR link, and the CI run URL or commit SHA that backs
    the claim. No step is marked passed here without a link that would
    fail if the claim were false.
  - `ADJUDICATION.md` — written after the run, per ASOP.md §6.1/§6.3: every
    divergence between what a step said to do and what actually happened,
    judged good or bad, with a proposal for the next version where
    warranted.

## How a run is actually recorded (the honest part)

ASOP.md describes a plane that files a run as a tree of beads, one per step,
each pinned to `(asop_id, version, step)`, with attestations and
adjudications checked against a declared registry (§5, §6.1). **This POC has
no such plane.** There is no AgentCo-style harness wired to `strangler-slice-a-feature/v1.yaml`
that files beads, tracks park clocks, or enforces the adjudicator-≠-executor
rule automatically. What actually happens is:

1. I (the executing agent) read the step from `v1.yaml` and do the work.
2. For a `deterministic` gate, I run the actual command in the gate's
   `check`/`checks` field (or the closest concrete equivalent — some check
   strings use `<slice>`, `<sha>`, `<legacy_ci_workflow>` etc. as literal
   placeholders for values this run substitutes; there's no templating
   engine, I substitute them by hand) and paste its real output/exit code
   and the CI run URL into `EVIDENCE.md`.
3. For a `human` gate, the step waits for mabidoli's actual review — I do
   not mark it passed on his behalf.
4. `ADJUDICATION.md` is written by me after the run, which is a real
   deviation from §6.1 ("the adjudicator must not be the executor") — flagged
   here rather than silently glossed over. The genuinely independent check in
   this POC is the gate itself (CI is green or it isn't; the diff is zero or
   it isn't), which is a stronger property than self-adjudication would be on
   its own, but it is not the same thing as an independent adjudicator, and
   `ADJUDICATION.md` should be read as a self-report backed by re-checkable
   evidence, not as an authenticated third-party judgement.

## Status of v1

Marked `status: draft` in `v1.yaml`'s header comment (identity fields aren't
part of the validated body — see ASOP.md §3.1). ASOP.md is explicit that
activation is a human act, and this v1 was authored by an agent — so rather
than self-activate, it stays `draft` and this POC's runs against it are
disclosed as manual executions against a draft procedure, not a claim that
some governance gate was satisfied. mabidoli reviewing this file (and, per
step 6, the resulting PR) is the real human checkpoint standing in for
activation here.
