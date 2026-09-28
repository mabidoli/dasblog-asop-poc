# Slice map — RSS 2.0 feed generation (slice 1)

Written per ASOP step 1 (`asop/strangler-slice-a-feature/v1.yaml`). Every
claim below is a file:line citation into this repo's legacy source
(`source/`), not a paraphrase.

## Entry point chain

1. `source/newtelligence.DasBlog.Web/web.config:36` — URL rewrite rule:
   `/rss.aspx` or `/rss.ashx` → `SyndicationService.asmx/GetRss`.
2. `source/newtelligence.DasBlog.Web/SyndicationService.asmx:1` — the ASMX
   file, codebehind class `newtelligence.DasBlog.Web.SyndicationService`.
3. `source/newtelligence.DasBlog.Web/SyndicationService.asmx.cs:44` —
   `SyndicationService : SyndicationServiceImplementation`, sets
   `inASMX = true`.
4. `source/newtelligence.DasBlog.Web.Services/SyndicationServiceImplementation.cs:128`
   — `GetRss()` `[WebMethod]`, reads `siteConfig.RssDayCount` /
   `siteConfig.RssMainEntryCount`, calls `GetRssWithCounts`.
5. `...SyndicationServiceImplementation.cs:167` — `GetRssWithCounts(int, int)`
   calls `GetRssCore(null, maxDayCount, maxEntryCount)`.
6. `...SyndicationServiceImplementation.cs:196-501` — `GetRssCore`: the
   method that actually builds the `RssRoot` document. **This is the slice's
   real boundary** — everything above it is transport plumbing (ASMX/SOAP
   wrapping, URL rewriting).

**A genuine finding, not part of this slice**: `web.config:35` also maps
`/atom.ashx` to `SyndicationServiceExperimental.asmx/GetAtom`, but
`SyndicationServiceExperimentalImplementation.cs:62` declares no `GetAtom`
method — `SyndicationServiceExperimental.asmx.cs:9` documents that "the new
Atom 1.0 stuff is in SyndicationService" instead (which does have
`GetAtom()` at `SyndicationServiceImplementation.cs:724`, reachable only via
`/SyndicationService.asmx/GetAtom`, not the friendly `/atom.ashx` URL). The
`/atom.ashx` rewrite rule is dead configuration in this codebase snapshot.
Left as-is (out of scope to fix a legacy bug this slice doesn't touch);
noted here because it's exactly the kind of thing a slice map is supposed
to catch before it surprises someone later.

## Dependencies GetRssCore touches

Traced from `...SyndicationServiceImplementation.cs:196-501`:

- **HTTP/transport-coupled** (excluded from this slice — see RULES.md's
  "explicitly deferred" section):
  `RedirectToFeedBurnerIfNeeded` (:172-195), `HttpContext.Current` /
  `Context.Request.*` (:210-236), `SiteUtilities.GetStatusNotModified`
  (:212-215), `ReferralBlackList.IsBlockedReferrer` + `loggingService.AddReferral`
  (:217-236), the `cache[CacheKey]` / `cache.Insert(...)` response cache
  (:239-241, :498).
- **Data selection** (excluded — treated as a slice boundary, not
  reimplemented): `BuildEntries` (`SyndicationServiceBase.cs:109-141`) pulls
  from `IBlogDataService` (category lookup or `GetEntriesForDay`). This
  slice takes an already-built `EntryCollection` as its input instead of
  calling `BuildEntries` itself — `BuildEntries` is real business logic but
  belongs to a "which entries make the feed" slice, not "how an entry
  becomes feed XML".
- **In scope** — the pure mapping from `(EntryCollection, SiteConfig)` to
  `RssRoot`: the channel-building block (:263-308) and the per-item loop
  (:310-497), plus their direct helpers:
  - `SyndicationServiceBase.cs:75-98` — `PreprocessItemContent`
  - `SyndicationServiceBase.cs:101-107` — `EntrySorter` (applied by
    `BuildEntries` upstream; this slice applies it directly to its input
    `EntryCollection` since `BuildEntries` itself is out of scope)
  - `source/newtelligence.DasBlog.Web.Core/SiteUtilities.cs:456-459` —
    `GetPermaLinkUrl(SiteConfig, string id)`
  - `SiteUtilities.cs:477-500` — `GetPermaLinkUrl(SiteConfig, ITitledEntry)`
  - `SiteUtilities.cs:571-574` — `GetTrackbackUrl`
  - `SiteUtilities.cs:579-582` — `GetBaseUrl`
  - `SiteUtilities.cs:96-105` — `RelativeToRoot`
  - `SiteUtilities.cs:1019-1029` — `LinkRewriter` (a no-op when
    `siteConfig.EnableUrlRewriting == false`, this slice's fixture setting)
  - `source/newtelligence.DasBlog.Web.Core/SiteSecurity.cs:462-473` —
    `GetUser` (used for `dc:creator`; this slice's fixtures use entries
    whose author never resolves, exercising the "empty dc:creator" path —
    see RULES.md RULE-feed-D5 for the deferred resolvable-author case)

## Data touched

- `EntryCollection` / `Entry` (`newtelligence.DasBlog.Runtime`) — read-only
  in this slice: `Title`, `Author`, `EntryId`, `Categories`, `Language`,
  `CreatedUtc`, `Description`, `Content`, `IsPublic`, `Syndicated`.
- `SiteConfig` (`newtelligence.DasBlog.Web.Core`) — read-only: `Title`,
  `Description`, `Subtitle`, `Copyright`, `Contact`, `RssLanguage`,
  `ChannelImageUrl`, `Root`, `EnableUrlRewriting`, `EnableTitlePermaLink`,
  `ExtensionlessUrls`, `ApplyContentFiltersToRSS`,
  `EnableAggregatorBugging`, `EnableRssItemFooters`, `RssItemFooter`,
  `AlwaysIncludeContentInRSS`, `HtmlTidyContent`.
- `Rss20.RssRoot` / `RssChannel` / `RssItem` (`SyndicationService.Rss20`,
  `Rss.cs`) — write-only output structure.

## Scope decision (mabidoli-style: named, not implied)

Slice 1 is **RSS 2.0 generation for a "plain" entry**: no comments, no
GeoRSS, no attachments, no category-filtered channel, default (non-title)
permalinks, URL rewriting off, content filters off. Every excluded
concern is named as a deferred rule in `RULES.md`, not silently dropped.
This follows ASOP.md's own review note (`ASOP-MODERNIZATION-REVIEW.md`
gap #6): the full `GetRssCore` is NOT close-to-pure logic — it's laced
with `HttpContext`, caching, and a live security/config subsystem — so
the achievable first slice is the pure core within it, named precisely
enough that "achievable" doesn't quietly become "misleading".
