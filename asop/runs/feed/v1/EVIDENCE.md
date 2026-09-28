# Run evidence — strangler-slice-a-feature v1, slice "feed"

Per asop/README.md's "how a run is actually recorded": this is a manual run
against a `draft` ASOP, executed by me (the agent), with mabidoli as the
named human gate for steps 1 and 6. Every claim below links a real CI run
or commit, re-checkable by anyone.

## Step 1 — map-the-slice

- Write-back: `modernization/feed/SLICE-MAP.md`, commit `dec1d5f`... through
  the slice-map commit (see `git log -- modernization/feed/SLICE-MAP.md`).
- Gate: `kind: human`, verifier `mabidoli`. **Not yet satisfied** — this
  run's step 1 gate is open pending mabidoli's review of SLICE-MAP.md
  against the actual source. Recorded here as an honest open item, not
  marked done.

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

## Step 4 onward

Not yet executed at the time of writing this file - see
`asop/runs/feed/v1/ADJUDICATION.md` (written once the run reaches a natural
checkpoint) for the self-revision read on steps 1-3.
