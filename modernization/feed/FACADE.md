# Facade — slice 1 (RSS 2.0 feed generation)

ASOP step 5 (`facade-and-route-traffic`,
`asop/strangler-slice-a-feature/v1.yaml`) asks for either (a) the legacy
call sites actually routed through the new .NET 10 component, or (b),
where that's impractical, an integration test proving the facade
**contract** holds on both paths, with which one applied stated plainly.

**This slice is (b).**

## Why not (a)

The legacy entry point
(`newtelligence.DasBlog.Web.SyndicationService.GetRss` →
`SyndicationServiceImplementation.GetRssWithCounts`, see
`modernization/feed/SLICE-MAP.md`) is an ASP.NET Web Service running on
.NET Framework, in-process, under IIS. The .NET 10 port
(`src-modern/DasBlog.Feed/`) is a separate, cross-platform runtime that
cannot be loaded into that same process — .NET Framework cannot host
.NET 10 assemblies in-process, and there is no interop bridge for this in
the current codebase. Routing real traffic from (a) to the .NET 10
component would need either an out-of-process call (the legacy ASMX method
calling out to a co-hosted .NET 10 HTTP/gRPC service) or a full migration
of the hosting model itself — both real engineering, out of scope for
proving the METHOD is correct, and the kind of infrastructure work a real
migration would do only after the method is trusted. That trust is what
steps 3/4 exist to establish first.

## The contract, made concrete

The facade contract is: **given the same set of blog entries and the same
site configuration, both paths produce the same RSS 2.0 XML.** Concretely:

| | Legacy | .NET 10 |
|---|---|---|
| Entry point | `SyndicationServiceImplementation.GetRssWithCounts(int, int)` | `RssFeedBuilder.Build(FeedSiteConfig, IEnumerable<FeedEntry>)` |
| Entry input type | `newtelligence.DasBlog.Runtime.EntryCollection` (via a fake `IBlogDataService` in the test — see its own file header) | `List<DasBlog.Feed.FeedEntry>` |
| Config input type | `newtelligence.DasBlog.Web.Core.SiteConfig` | `DasBlog.Feed.FeedSiteConfig` |
| Output type | `newtelligence.DasBlog.Web.Services.Rss20.RssRoot` | `DasBlog.Feed.Rss20.RssRoot` (ported, same `XmlSerializer` shape) |
| Serialized via | `System.Xml.Serialization.XmlSerializer` | `System.Xml.Serialization.XmlSerializer` (+ `RssFeedSerializer`'s documented xsi/xsd normalization, RULE-feed-19) |

`FeedEntry`/`FeedSiteConfig` (`src-modern/DasBlog.Feed/FeedEntry.cs`,
`FeedSiteConfig.cs`) ARE the translation layer a real out-of-process
facade would serialize across the wire — their doc comments say exactly
which legacy fields they stand in for and which legacy flags they
deliberately don't model (the same `RULE-feed-D*` boundary as everywhere
else in this slice).

## The integration proof

Two independent test suites assert against the SAME golden files under
`modernization/feed/golden/` (not against each other, and not against
in-memory objects passed between them — genuinely independent, on two
different runtimes, in two different CI jobs):

- `source/newtelligence.DasBlog.Web.Services/Test/FeedCharacterization/RssFeedCharacterizationTests.cs`
  (legacy path) — green on `legacy.yml`, windows-latest.
- `src-modern/DasBlog.Feed.Tests/RssFeedCharacterizationTests.cs`
  (.NET 10 path) — green on `modern.yml`, ubuntu-latest.

`legacy == golden` (proven by the first) AND `modern == golden` (proven by
the second) together mean `legacy == modern` by transitivity — for the
SAME two fixtures, exercised through both paths' own real entry point, not
a shortcut. `scripts/golden-diff.py`'s docstring makes this reasoning
explicit rather than leaving it implied. See
`asop/runs/feed/v1/EVIDENCE.md` steps 3 and 4 for both runs' URLs.

This is real evidence that the CONTRACT holds for both fixtures tried; it
is not evidence that routing (a) would be safe to flip on for 100% of live
traffic without further verification (that's exactly what a real (a)
would need to prove next, once someone builds it) — named here so the
limitation isn't implied away.
