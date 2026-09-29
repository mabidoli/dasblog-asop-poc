# Run evidence — strangler-slice-a-feature v1, slice "feed"

Per asop/README.md's "how a run is actually recorded": this is a manual run
against a `draft` ASOP, executed by me (the agent), with mabidoli as the
named human gate for steps 1 and 6. Every claim below links a real CI run
or commit, re-checkable by anyone.

## Step 1 — map-the-slice

- Write-back: `modernization/feed/SLICE-MAP.md`, commit `dec1d5f`... through
  the slice-map commit (see `git log -- modernization/feed/SLICE-MAP.md`).
- Gate: `kind: human`, verifier `mabidoli`. **Closed 2026-09-28T23:57:07Z**
  — approved via merge of PR #1
  (<https://github.com/mabidoli/dasblog-asop-poc/pull/1>), by `mabidoli`.
  Recorded honestly: this is approval via PR merge, not a separate written
  review of SLICE-MAP.md itself — the PR carries no review comments (`gh
  pr view 1 --json reviews,comments` returns empty for both). The merge is
  the only signal that closes this gate.

## Step 2 — extract-business-rules

- Write-back: `modernization/feed/RULES.md` — 16 in-scope rules
  (RULE-feed-01..16), 16 explicitly deferred (RULE-feed-D1..D16).
- Gate: `kind: deterministic`, check =
  `scripts/check-rules-have-tests.py modernization/<slice>/RULES.md modernization/<slice>/tests-legacy`.
  **Substitution disclosed**: the real characterization tests live under
  `source/newtelligence.DasBlog.Web.Services/Test/FeedCharacterization/`
  (they must, to compile and run against the legacy build), not
  `modernization/feed/tests-legacy`. Actual command run:

  ```
  $ python3 scripts/check-rules-have-tests.py modernization/feed/RULES.md \
      source/newtelligence.DasBlog.Web.Services/Test/FeedCharacterization
  modernization/feed/RULES.md: 16 in-scope rule(s), 16 deferred
    [ok] RULE-feed-01
    ... (all 16 ok)
  OK: every in-scope rule is referenced by a test under source/newtelligence.DasBlog.Web.Services/Test/FeedCharacterization
  ```

  Run locally 2026-09-28; also implicitly re-verified by every
  legacy.yml/capture-golden.yml CI run since the test file itself carries
  the RULE-feed-NN citations the script greps for.
  **Gate: PASSED.**

## Step 3 — characterize-the-legacy-behaviour

Real CI iterations, in order (each a real failure with a real fix, not
guesses accepted on faith):

| # | What broke | Fix | Run |
|---|---|---|---|
| 9 | `newtelligence.DasBlog.Web.Services.Test.csproj`'s new refs to Web.Services/Runtime/Web.Core: two of three silently missing from csc.exe's `/reference:` list (CS0234/CS0246) despite correct GUIDs | (misdiagnosed as a parallel-build race) `/m:1` | [36438552264](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36438552264) - still red |
| 10 | Same error, proven NOT a race (direct single-project build also failed) | Added `ProjectSection(ProjectDependencies)` to `source/DasBlog.sln` for the Test project | [36439661458](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36439661458) - still red |
| 11 | Same error again | Pivoted Web.Services/Web.Core to plain `Reference`+`HintPath` | [36440111210](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36440111210) - still red |
| 12 | Same error a fourth time - forced a detailed msbuild log | **Real root cause found**: `MSB3274` - the Test project still targeted `.NETFramework,Version=v3.5`; Web.Services/Web.Core were retargeted to v4.8 back in `legacy.yml` iteration 3/4 (Phase 0). A v3.5 project cannot reference a v4.8 assembly. Bumped the Test project to v4.8, reverted the HintPath detour back to `ProjectReference`. | [36441074886](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36441074886) - build green, tests ran |
| 13 | Capture mode wrote golden files under `...\Temp\nunit20\ShadowCopyCache\...` instead of the repo, because nunit-console shadow-copies the test assembly by default and the test resolves its own path via `Assembly.GetExecutingAssembly().Location` | Added `/noshadow` to both workflows' nunit-console invocation | [36441539832](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36441539832) - **capture succeeded**, real golden XML produced |
| 14 | The two characterization tests failed against their OWN just-captured golden files on the next run (`Expected string length 1647 but was 1624`, a `\r\n` vs `\n` diff at the XML declaration line) | Windows runner's git checkout applied `autocrlf` to the committed (pure-LF) golden files; added `.gitattributes` marking `modernization/**/golden/** -text` | [36442560896](https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36442560896) - **GREEN** |

**Gate (legacy_ci_workflow green at the commit adding these tests): PASSED.**
Final green run: <https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36442560896>
(`legacy.yml`, commit range ending at the `.gitattributes` commit, branch
`master`). `Tests run: 48, Errors: 0, Failures: 0` for the full legacy
suite including both feed characterization tests.

Golden files reviewed by hand before commit (not just "captured and
trusted") - see the golden-file commit message for what was checked in
each one against `RULES.md`.

## Step 4 — implement-on-dotnet-10

- Write-back: `src-modern/DasBlog.Feed/` (the port: ported `Rss20` DTOs,
  `RssFeedBuilder`, `RssFeedSerializer`), `src-modern/DasBlog.Feed.Tests/`
  (the same two fixtures as the legacy characterization tests, asserted
  against the same committed golden files).
- Local verification first (no .NET Framework on this Mac, but .NET 10
  installed via `dotnet-install.sh --channel 10.0` and runs natively):
  `dotnet test src-modern/DasBlog.Feed.Tests` — 2/2 passed, byte-for-byte,
  before ever pushing.
- Gate: `checks` (staged) —
  1. `gh run list --workflow=<modern_ci_workflow> --commit <sha> ...` —
     **PASSED**, first attempt:
     <https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36444318253>
     (`modern.yml`, ubuntu-latest, `actions/setup-dotnet` 10.0.x, commit
     `4e502fc`).
  2. `scripts/golden-diff.py src-modern/DasBlog.Feed.Tests/...` — **PASSED**
     as part of the same CI run (the workflow's own gate step). Per the
     script's own design (see its docstring), this proves
     `legacy == golden` (step 3's gate) AND `modern == golden` (this gate)
     together, which is `legacy == modern` by transitivity — checked twice
     independently rather than computed once and trusted.

Three rules the legacy SOURCE reading (step 2) missed, surfaced only by
actually trying to reproduce the golden files byte-for-byte — added to
`RULES.md` as RULE-feed-17/18/19 rather than silently patched around:
- `<generator>` is a hardcoded legacy-assembly-version string
  (`"newtelligence dasBlog 4.0.0.0"`), not derivable in a port.
- The custom `xmlns` declaration order in the golden files doesn't match
  the legacy source's own `.Add(...)` call order.
- `XmlSerializer`'s auto-declared `xmlns:xsi`/`xmlns:xsd` order is opposite
  between .NET Framework and .NET 10, and empirically not controllable via
  insertion order on the .NET 10 side at all — normalized as a documented
  string swap.

This is exactly the ASOP's own self-revision premise (divergence is
input, ASOP.md §6.1) working as intended: step 2's gate (every rule has a
test) still held, because these three became NEW rules with their own
tests, not exceptions carved out of it.

## Step 5 — facade-and-route-traffic

- Write-back: `modernization/feed/FACADE.md`.
- **Option (b) taken, stated plainly**: full in-process WebForms routing to
  the .NET 10 component is impractical (different runtimes, no interop
  bridge in this codebase — see FACADE.md for why). The facade contract
  (same entries + config in, same RSS 2.0 XML out) is proven by both
  legacy.yml and modern.yml independently asserting the SAME committed
  golden files, on two different runtimes, in two different CI jobs.
- Gate: `kind: deterministic`, check = "CI job running the facade
  integration test suite is green at this commit, for both the
  legacy-path and new-path assertions." **PASSED** — this is the same
  legacy.yml/modern.yml pair from steps 3/4, both green at the commits
  cited there; re-verified together as the facade proof per FACADE.md's
  reasoning rather than treated as a separate new job.

## Step 6 — review-and-open-pr

- Write-back: PR opened at
  <https://github.com/mabidoli/dasblog-asop-poc/pull/1>, base `baseline`
  (a branch pinned to the original fork commit `036f9f2`, pushed solely so
  the PR has something to diff against — all of this run's work landed
  directly on `master`), head `master`.
- Gate: `kind: human`, verifier `mabidoli`. **Closed 2026-09-28T23:57:07Z**
  — merged by `mabidoli`. No written review comments on the PR (`gh pr
  view 1 --json reviews,comments` returns empty) — the merge itself is the
  approval signal, recorded as such rather than implied to be a
  line-by-line review.

Self-revision read: `asop/runs/feed/v1/ADJUDICATION.md`.
