# Plan — ASOP modernization POC

Public proof-of-concept for ASOP-driven legacy modernization:
`shanselman/dasblog` (.NET Framework) → .NET 10, one Strangler-Fig slice
at a time, every step gated by CI (or local) evidence. Background:
`~/Code/personal-brand/linkedin/content/ASOP-MODERNIZATION-REVIEW.md`.
AgentCo bead: `ac-9089d486`.

## Phases

- **Phase 0 — oracle infrastructure.** Fork, `legacy.yml` (build+test the
  untouched legacy solution on windows-latest), a gate-proof (deliberate
  red run, reverted). **Done** — see `asop/runs/feed/v1/EVIDENCE.md`'s
  opening section. 8 CI iterations.
- **Phase 1 — ASOP v1.** Hand-author `asop/strangler-slice-a-feature/v1.yaml`
  against the `asop` contract (`~/Code/asop`, `ASOP.md` v3.4). **Done**,
  validated with `asop.sop.validate_asop`.
- **Phase 2 — execute v1 on slice 1** (RSS/Atom feed generation, scoped to
  "plain entry" RSS 2.0 — see `modernization/feed/SLICE-MAP.md` for the
  exact boundary). **Done** through step 6 (PR opened, unmerged):
  `SLICE-MAP.md` → `RULES.md` (19 rules) → legacy characterization tests
  (green, 6 more CI iterations) → .NET 10 port (green, 1 CI iteration,
  local SDK) → facade contract (option b, documented) → PR #1. Full
  evidence: `asop/runs/feed/v1/EVIDENCE.md`; self-revision read:
  `ADJUDICATION.md`.
- **Phase 2.5 — Windows packaging** (this document's own reason for
  existing). `harness/` submodule, `scripts/gates/*.ps1` (local
  equivalents of every deterministic CI gate), `scripts/run_asop.py` +
  `scripts/run-asop.ps1` (a thin, harness-independent step-walker — see
  `RUNBOOK-WINDOWS.md` for why it doesn't use
  `agentco_harness.asop_store`), `RUNBOOK-WINDOWS.md`, this file. **Done**,
  reported, awaiting a go before Phase 3.
- **Phase 3 — self-revision loop, v1→v4.** NOT executed yet. See "Iteration
  loop" below for the shape and "Budget" for the stop condition. Waiting
  on mabidoli's go per the team lead's last instruction.
- **Phase 4 (stretch) — a second slice**, to test whether v(final)
  generalizes beyond feed generation. Candidates from the original review:
  comment spam filter, trackback/pingback parsing (both named in the
  original task brief as "small" second-slice candidates). Not started.

## Iteration loop (v1 → v4)

Each version:

1. Run the ASOP end to end (all 6 steps) against the target slice.
2. Write `asop/runs/<slice>/v<N>/EVIDENCE.md` (every gate, its actual
   command/output, real run URLs or local exit codes — never a claim
   without a link or a transcript).
3. Write `asop/runs/<slice>/v<N>/ADJUDICATION.md`: every divergence
   between what a step said to do and what happened, judged good or bad,
   with evidence. Self-adjudicated in this POC (see `asop/README.md`'s
   disclosed limitation — no independent adjudicator), so read as a
   self-report backed by re-checkable gate evidence, not an authenticated
   third-party judgement.
4. Where the evidence supports it, propose `v<N+1>.yaml`: text fixes to
   existing steps first (cheaper, lower-risk), structural changes (a step
   split, added, or reordered) only when the SAME divergence recurs at the
   SAME boundary across more than one run (ASOP.md §6.3's own bar).
5. Re-validate the new version against `asop.sop.validate_asop` before
   running it.

Stop the loop **before v4** if a version runs the slice end-to-end with
zero BAD divergences (per its own `ADJUDICATION.md`) and every
deterministic gate green — that's "done," not "keep going for its own
sake." Stop **at v4** regardless of outcome (the original task brief's own
budget) and report honestly if v4 still has open bad divergences.

## Metrics to record, per version

Table shape used in `asop/runs/feed/v1/ADJUDICATION.md` — carry it forward
for every version so they're comparable:

| Version | CI/local iterations to green (per step) | Bad divergences | Gate failures caught | Human interventions | Wall time |
|---|---|---|---|---|---|
| v1 | Phase 0: 8, step 3: +6, step 4: +1 | 2 (see ADJUDICATION.md) | 2 gate-proof/build-order dead ends caught before landing | 0 (both human gates still open) | one continuous session |
| v2 | — | — | — | — | — |
| v3 | — | — | — | — | — |
| v4 | — | — | — | — | — |

"The procedure got better per slice, with numbers" (the review doc's own
bar for what makes this different from "the agent migrated a blog") needs
at least two rows filled in to mean anything — v1 alone is a baseline, not
a trend.

## Budget

- Per-version run: no hard ceiling set yet: v1 took roughly 20 CI
  iterations total (build oracle + characterization tests) plus 1 for the
  .NET 10 port — use that as the planning baseline for v2, and flag to
  mabidoli if a version blows past ~2x that without a clear reason (a
  genuinely harder slice, not just more of the same class of bug).
  the review doc's original ask.
- Second slice (Phase 4): budgeted at "small" by the original review
  (comment spam filter or trackback/pingback parsing) — should NOT need
  another 20-iteration oracle-infrastructure phase, since `legacy.yml`
  and the gate scripts already exist; the cost there should be almost
  entirely steps 1-2 (mapping + rules) and step 4 (the new slice's own
  .NET 10 logic), not build plumbing.
- Whole POC: bounded by mabidoli's own review cadence more than by CI
  minutes — every version's step 6 is a human gate, and the loop
  shouldn't produce v(N+1) faster than v(N) can actually be reviewed.
