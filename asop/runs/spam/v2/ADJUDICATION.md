# Adjudication — strangler-slice-a-feature v2, slice "spam"

Per ASOP.md §6.1/§6.3. Self-adjudicated (same disclosed limitation as
`asop/README.md` and v1's own `ADJUDICATION.md`).

## Good divergences (v2's own fixes held)

1. **PARK-AND-CONTINUE worked as designed, this time disclosed rather than
   silent.** Step 1's human gate (mabidoli, SLICE-MAP.md review) is open;
   this run proceeded per v2's explicit park-and-continue text, with every
   downstream write-back and this file marked as depending on step 1's
   eventual close (see `EVIDENCE.md` step 1). Compare to v1's item 1 — the
   difference is entirely that this time it's a named, bounded, disclosed
   choice instead of something an executor just did.
2. **The test-reliance rule (v2's step 3 common_mistakes addition) wasn't
   needed to catch anything new this run** — no gate-proof-style false
   green happened in the SAME way v1's did. It's not evidence the rule is
   unnecessary (a text warning changes what an executor checks, not what
   they'd have missed without checking) — noted as a clean pass, not
   claimed as proof.
3. **"Iterate locally before CI" (v2's step 3/4 purpose addition) applied
   naturally to step 4** — the .NET 10 port took exactly 1 CI attempt
   (`36498987392`), same pattern as v1's slice 1. Step 3 (the legacy side)
   still cost 4 CI iterations (`EVIDENCE.md`'s table) since no local .NET
   Framework exists on this machine — v2's own text anticipates this
   exactly ("when it isn't [locally installable]... budget for that").

## Bad divergences (new this run)

4. **A green CI job that didn't do the thing it was supposed to do.**
   Iteration 3 of step 3's capture run (`EVIDENCE.md`) reported success on
   every step, including "Show captured golden files" and "Upload captured
   golden files" — but a path-calculation bug meant NO spam golden files
   were actually captured; the job only found the pre-existing feed ones
   and happily reported that as success. This is NOT the same as v1's
   AppTest.cs incident (that was a test silently not compiled in; this was
   a workflow silently not producing the artifact it claimed to). Same
   family of mistake — "green is not evidence of the specific thing you
   think it proves" — one level up the stack.

   **Proposal for v3**: extend the test-reliance common_mistakes item (or
   add a sibling one) to cover WORKFLOW-level claims, not just test-level
   ones: a capture/build workflow that reports success should assert
   something CONCRETE about its own output (e.g., "the file this run was
   supposed to produce has a modification time newer than this run's
   start", or "the artifact contains N files, not just >=1") rather than
   relying on `if-no-files-found: error` against a glob that pre-existing
   files can also satisfy.

## Outcome

Slice 2 (Akismet spam-check request mapping) completed steps 1-6 with real
CI evidence for every deterministic gate, including a genuine legacy bug
(RULE-spam-10) found by characterization and faithfully reproduced rather
than silently fixed.

**Update 2026-09-28T23:57:50Z**: both human gates (step 1, step 6) are now
closed — mabidoli merged PR #2
(<https://github.com/mabidoli/dasblog-asop-poc/pull/2>) with no separate
written review comments. Recorded honestly in `EVIDENCE.md`: the merge is
the approval signal for both gates. Every PROVISIONAL marking from
PARK-AND-CONTINUE is lifted as of this timestamp, and no boundary error
was raised against `SLICE-MAP.md`.

One structural note distinct from v1: choosing slice 2 required reading
BOTH candidates' actual code, not just their names — the review doc's
framing ("comment spam filter" vs "trackback/pingback parsing") undersold
how differently constrained trackback, pingback, and the Akismet mapping
actually are (live network dependency vs. ASP.NET-hosting-environment
dependency vs. neither). Worth naming as a lesson for slice-picking in
general, not a v3 ASOP-text change — the ASOP's own step 1
(`map-the-slice`) already asks for exactly this kind of check before
committing to a boundary; it just wasn't done AT THE CANDIDATE-SELECTION
stage in the original review doc, only after a candidate was already
picked in both slices so far.
