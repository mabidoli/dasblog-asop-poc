# Changelog — strangler-slice-a-feature

Per ASOP.md §6.3 (`propose`): good adjudications become a step's
`proposals`, bad ones become `common_mistakes`; structural changes need the
same divergence recurring at the same boundary more than once. v2 applies
exactly the three fixes `asop/runs/feed/v1/ADJUDICATION.md` proposed — no
more, no less. v1 is left untouched (a version, once run against, is not
edited — ASOP.md §4).

## v1 → v2

### (a) Human-gate semantics: PARK-AND-CONTINUE

v1's adjudication (item 1) named two options and left the choice open:
park-and-continue with downstream outputs marked provisional, or splitting
the gate into `judged` + async `human`. **v2 takes park-and-continue.**

Why, over the split: splitting step 1 into a judged rubric check plus a
separate human-sign-off step would add a step (6 → 7, still inside
`MAX_STEPS`, but structural rather than textual) and needs a real judge
route this POC has never built or proven (no distinct judged executor is
wired anywhere in this repo — `run_asop.py`'s docstring says as much).
Park-and-continue needed no new step, no new role, and no new
infrastructure: it's a textual rule (steps 1's `write_back`, step 2's
`entry_check`, the ASOP's own top-level `purpose`, and step 6's
`definition_of_done`) that describes, in the ASOP itself, the exact thing
v1's executor actually did — proceed past an open gate — except now
BOUNDED (write-backs marked PROVISIONAL, lifted only when the gate
closes) instead of silent. It also matches infrastructure that already
exists and is proven elsewhere: the harness's own
`intake.require_approval` / `PENDING_APPROVAL` semantics
(`harness/README.md`'s "inverse shadow" mode) are the identical shape —
work happens, but stays visibly held until a human looks.

The judged+human split isn't wrong, just heavier than this fix needed to
be. If a future version's evidence shows PROVISIONAL work getting merged
without anyone actually checking the "is it still provisional" box (i.e.
park-and-continue's discipline doesn't hold in practice), that's the
signal to revisit the split instead.

### (b) Gate-proof / test-reliance rule

v1's adjudication (item 2): a gate-proof test came back green because it
wasn't in the target project's compile list at all — a false-positive that
could have been mistaken for "the gate doesn't fire." Added as step 3's
third `common_mistakes` entry (the slot was free — steps 3/4 each had 2 of
the 3 `MAX_COMMON_MISTAKES` used): confirm a test is actually compiled in
AND that the run's own test count reflects it, before trusting a green
exit code.

### (c) Iterate locally before CI, when the target runtime allows it

v1's adjudication's quantitative finding: the legacy side (no locally
installable .NET Framework on this POC's own machine) cost 14 CI
iterations; the .NET 10 port, iterated locally first with a real SDK
installed via `dotnet-install.sh`, cost 1. Added to both step 3's and step
4's `purpose` text, phrased conditionally ("when the target runtime is
locally installable") rather than as a blanket instruction — it wasn't
true for step 3 in THIS run, and won't be true for every future run or
every future engineer's machine either.

## Not changed

Everything else — roles, inputs, step 2/4/5's gates, tags, ordering. No
structural change (a step split, add, or reorder) is in v2: per ASOP.md
§6.3's own bar, that needs the SAME divergence recurring at the SAME
boundary across more than one run, and v1 is the only run so far.
