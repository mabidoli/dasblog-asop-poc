# Facade — slice 2 (Akismet spam-check request mapping)

ASOP step 5. Per v2.yaml, either (a) legacy call sites actually routed
through the new .NET 10 component, or (b), where impractical, an
integration test proving the facade contract on both paths, stated
plainly which applies.

**This slice is (b), for the same class of reason as slice 1.**

## Why not (a)

`AkismetSpamBlockingService` is reached through
`SiteConfig.SpamBlockingService`, an `ISpamBlockingService`-typed property
resolved inside the legacy ASP.NET Framework process (see
`modernization/spam/SLICE-MAP.md`'s entry-point chain). The .NET 10 port
(`src-modern/DasBlog.Spam/`) runs on a separate, cross-platform runtime
with no interop bridge in this codebase — the same structural gap slice
1's `FACADE.md` already named. Building one is real infrastructure work,
appropriately deferred until the mapping logic itself is trusted (which is
exactly what steps 3/4 establish).

## The contract, made concrete

Given the same `IFeedback`/`Feedback` fields, both paths produce the same
canonical text dump of the resulting Akismet comment payload:

| | Legacy | .NET 10 |
|---|---|---|
| Entry point | `AkismetSpamBlockingService.ConvertToAkismetComment` (private, invoked via reflection in tests) | `AkismetCommentMapper.Convert` |
| Input type | `newtelligence.DasBlog.Runtime.IFeedback` (via a fixture `FakeFeedback` in the test) | `DasBlog.Spam.Feedback` |
| Output type | `Subtext.Akismet.Comment` (`IComment`) | `DasBlog.Spam.AkismetComment` |
| Serialized via | A plain field dump (no XmlSerializer attributes on the legacy type to reuse — unlike slice 1) | The same field dump shape, `AkismetCommentSerializer` |

`Feedback`/`AkismetComment` are the translation layer a real out-of-process
facade would serialize across the wire, the same role slice 1's
`FeedEntry`/`FeedSiteConfig` play.

## The integration proof

Two independent test suites assert against the SAME golden files under
`modernization/spam/golden/`:

- `source/newtelligence.DasBlog.Web.Core/Test/AkismetCommentMappingCharacterizationTests.cs`
  (legacy path) — green on `legacy.yml`, windows-latest
  (<https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36498284722>).
- `src-modern/DasBlog.Spam.Tests/AkismetCommentMappingCharacterizationTests.cs`
  (.NET 10 path) — green on `modern.yml`, ubuntu-latest
  (<https://github.com/mabidoli/dasblog-asop-poc/actions/runs/36498987392>).

`legacy == golden` AND `modern == golden` together mean `legacy == modern`
by transitivity, for all 4 fixtures (including the RULE-spam-10 crash
case) — the same reasoning slice 1's `FACADE.md` and
`scripts/golden-diff.py`'s docstring already make explicit.

This proves the contract holds for the fixtures tried, including the one
deliberately chosen to exercise a real bug; it is not evidence that a live
routing facade would be safe to build without further verification of its
own (the ASP.NET-hosting-environment gap named above is real integration
work still to do).
