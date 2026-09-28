# Run evidence — strangler-slice-a-feature v2, slice "spam"

Per `asop/README.md`'s "how a run is actually recorded" and v2's own
PARK-AND-CONTINUE semantics (`asop/strangler-slice-a-feature/CHANGELOG.md`).
mabidoli's reviews for BOTH this run's step 1 and the still-open slice-1 v1
PR are pending — this run proceeded under park-and-continue, not silent
assumption. Every claim below links a real CI run or commit.

## Step 1 — map-the-slice

- Write-back: `modernization/spam/SLICE-MAP.md`, including the
  candidate-comparison section (why Akismet mapping over trackback or
  pingback).
- Gate: `kind: human`, verifier `mabidoli`. **Open — PROVISIONAL.** Per
  v2's PARK-AND-CONTINUE, steps 2-6 below proceeded with this step's
  output marked provisional. If mabidoli's review of SLICE-MAP.md finds a
  boundary error, steps 2-6 get REVISITED, not just re-labeled (v2.yaml's
  own step-1 write_back text).

## Step 2 — extract-business-rules

- Write-back: `modernization/spam/RULES.md` — 10 in-scope rules
  (RULE-spam-01..10), 3 deferred.
- Gate: `scripts/check-rules-have-tests.py modernization/spam/RULES.md
  source/newtelligence.DasBlog.Web.Core/Test`:

  ```
  modernization/spam/RULES.md: 10 in-scope rule(s), 3 deferred
    [ok] RULE-spam-01 ... [ok] RULE-spam-10
  OK: every in-scope rule is referenced by a test under source/newtelligence.DasBlog.Web.Core/Test
  ```

  Run locally 2026-09-28. **PASSED.**

## Step 3 — characterize-the-legacy-behaviour

Real CI iterations (this slice's build oracle was already proven in slice
1 — v2's own added purpose text credits iterating locally where possible,
but the legacy side still has no local .NET Framework, so every iteration
here is a real CI round-trip, same constraint as v1):

| # | What broke | Fix | Run |
|---|---|---|---|
| 1 | First capture-golden run for spam: `TargetInvocationException` / `NullReferenceException` on the very first fixture | **Real bug found, not a test-authoring mistake**: `AkismetSpamBlockingService.cs:63` uses bitwise `&`, not short-circuiting `&&` — a null `TargetEntryId` still evaluates `.Trim()` and crashes. Fixed the fixture to use an empty string (the surviving path) and added a 4th test (`NullTargetEntryId_ThrowsNullReferenceException`) characterizing the crash itself as RULE-spam-10. | [36496878440](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36496878440) — red, but the RIGHT kind of red |
| 2 | Build failed: `Assert.Throws<T>`/`Assert.IsInstanceOf<T>` don't exist on the vendored NUnit 2.x `lib/nunit.framework.dll` | Plain `try`/`catch` instead of the generic assertion helpers | [36497314292](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36497314292) — red (build error) |
| 3 | Capture run reported success, but `Show captured golden files`/the uploaded artifact found nothing new under `modernization/spam/golden/` | `GoldenFilePath`'s directory walk-up count was copied from slice 1's `Web.Services.Test` project (5 hops) without checking THIS project's own `OutputPath` (`bin\` directly, no `\Debug\` subfolder) — one hop too many, writing files to `D:\a\<repo>\modernization\...` instead of `D:\a\<repo>\<repo>\modernization\...`. Fixed the hop count to 4 (traced from `bin` → `Test` → `newtelligence.DasBlog.Web.Core` → `source` → repo root). | [36497622955](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36497622955) — green, but silently wrong (see "adjudication" below) |
| 4 | — | Re-ran capture-golden with the path fix; all 3 golden files landed in the right place this time | [36497981841](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36497981841) — **green, real files, hand-reviewed against RULES.md before committing** |

**Gate PASSED** for real: `legacy.yml` full suite green with all 4 new
spam tests included —
<https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36498284722>,
`Tests run: 52, Errors: 0, Failures: 0` (up from slice 1's 48 by exactly
the 4 new tests — checked, not assumed).

### A genuine adjudication item from iteration 3

Iteration 3's run reported **green** (`Show captured golden files` step
succeeded, `Upload captured golden files` step succeeded) while actually
doing NOTHING useful — the path bug meant it silently captured nothing new
and the workflow's own file-existence checks weren't strict enough to
catch it (the pre-existing feed golden files satisfied
`if-no-files-found: error` on their own). This is the SAME CLASS of
mistake RULE about test-reliance (v2's step 3 common_mistakes, added from
v1's own AppTest.cs incident) warns about, just one level up: a green
WORKFLOW is not evidence the workflow did the thing you think it did,
either. See `ADJUDICATION.md` for the proposal this generates for v3.

## Step 4 onward

Not yet executed at the time of writing this file.
