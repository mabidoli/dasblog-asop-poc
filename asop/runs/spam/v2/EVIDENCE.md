# Run evidence — strangler-slice-a-feature v2, slice "spam"

Per `asop/README.md`'s "how a run is actually recorded" and v2's own
PARK-AND-CONTINUE semantics (`asop/strangler-slice-a-feature/CHANGELOG.md`).
This run proceeded under park-and-continue while both this run's step 1
and PR #1 (slice "feed") were pending — not silent assumption. Every claim
below links a real CI run or commit.

**Update 2026-09-28T23:57:50Z**: mabidoli merged PR #2 (this run's step 6
— see below), closing both this run's human gates. Every PROVISIONAL
marking below is lifted as of that timestamp.

## Step 1 — map-the-slice

- Write-back: `modernization/spam/SLICE-MAP.md`, including the
  candidate-comparison section (why Akismet mapping over trackback or
  pingback).
- Gate: `kind: human`, verifier `mabidoli`. **Closed 2026-09-28T23:57:50Z**
  — approved via merge of PR #2
  (<https://github.com/mabidoli/dasblog-asop-poc/pull/2>), by `mabidoli`.
  Recorded honestly: no separate written review of SLICE-MAP.md exists
  (`gh pr view 2 --json reviews,comments` returns empty) — the merge
  itself is what closed this gate. Per v2's PARK-AND-CONTINUE, steps 2-6
  below had proceeded with this step's output marked PROVISIONAL before
  that; the PROVISIONAL marking is lifted as of the merge timestamp above,
  and no boundary error was raised against SLICE-MAP.md, so nothing gets
  revisited.

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

## Step 4 — implement-on-dotnet-10

- Write-back: `src-modern/DasBlog.Spam/` (`Feedback`, `AkismetComment`,
  `AkismetCommentMapper`, `AkismetCommentSerializer`),
  `src-modern/DasBlog.Spam.Tests/` (the same 4 fixtures as the legacy
  side, asserted against the same committed golden files).
- Local verification first: `dotnet test
  src-modern/DasBlog.Spam.Tests` — 4/4 passed, byte-for-byte, before ever
  pushing (per v2's own added purpose text — iterate locally when the
  target runtime allows it).
- Gate: **PASSED, first CI attempt**:
  <https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36498987392>
  (`modern.yml`, both the feed and spam golden-diff gates green in the
  same run).
- **Faithful bug reproduction, not a silent fix**: `AkismetCommentMapper.Convert`
  reproduces RULE-spam-10's `&`-not-`&&` crash on a null `TargetEntryId`
  exactly — same operator semantics in C# on .NET 10, confirmed by the
  4th test (`NullTargetEntryId_ThrowsNullReferenceException`) passing on
  both sides. This is the ASOP's own "preserve the SAME observable
  behaviour" requirement taken literally, bug included; "fix" is a
  separate, later, explicit decision this run does not make silently.

## Step 5 — facade-and-route-traffic

- Write-back: `modernization/spam/FACADE.md`.
- **Option (b) taken, stated plainly** — same class of reason as slice 1
  (different runtimes, no interop bridge), plus this class's own
  dependency on a real ASP.NET hosting environment for the deferred
  RULE-spam-D1 path.
- Gate: **PASSED** — the same `legacy.yml`/`modern.yml` pair from steps
  3/4, both green at the commits cited there, re-verified together as the
  facade proof per `FACADE.md`'s reasoning.

## Step 6 — review-and-open-pr

- Write-back: PR opened at
  <https://github.com/mabidoli/dasblog-asop-poc/pull/2>, base `v1-final`
  (a branch pinned to the commit where PR #1's slice-1 evidence was
  complete, so this PR's diff is scoped to v2-authoring +
  Windows-packaging + slice-2 work only).
- Per v2's `definition_of_done` addition: the PR stated plainly, at open
  time, that step 1 was still PROVISIONAL under PARK-AND-CONTINUE.
- Gate: `kind: human`, verifier `mabidoli`. **Closed 2026-09-28T23:57:50Z**
  — merged by `mabidoli`. No written review comments on the PR — the
  merge itself is the approval signal, recorded as such rather than
  implied to be a line-by-line review.
