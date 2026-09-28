# Business rules — RSS 2.0 feed generation (slice 1)

Written per ASOP step 2. Every rule cites the exact legacy line it was read
from. "In scope" rules must each be referenced by at least one
characterization test in `tests-legacy/` (checked by
`scripts/check-rules-have-tests.py` — ASOP step 2's gate).

## In scope (characterized in this slice)

- **RULE-feed-01** — Entry inclusion: only entries with `IsPublic == true`
  AND `Syndicated == true` appear in the feed.
  `SyndicationServiceImplementation.cs:312-315`
- **RULE-feed-02** — Entries are ordered newest-first by `CreatedUtc`.
  `SyndicationServiceBase.cs:101-107` (`EntrySorter`)
- **RULE-feed-03** — Channel title = site `Title` (this slice's fixtures
  use no category filter; the category-suffixed title is RULE-feed-D1).
  `SyndicationServiceImplementation.cs:265-268`
- **RULE-feed-04** — Channel description = site `Description` if non-blank,
  else site `Subtitle`.
  `SyndicationServiceImplementation.cs:274-281`
- **RULE-feed-05** — Channel link = site base URL
  (`RelativeToRoot(siteConfig, "")`).
  `SyndicationServiceImplementation.cs:283`; `SiteUtilities.cs:579-582`
- **RULE-feed-06** — Channel copyright/managingEditor/webMaster copied
  verbatim from site `Copyright`/`Contact`.
  `SyndicationServiceImplementation.cs:284,289-290`
- **RULE-feed-07** — Channel language set only if site `RssLanguage` is
  non-empty; omitted otherwise.
  `SyndicationServiceImplementation.cs:285-288`
- **RULE-feed-08** — Item guid: `isPermaLink=false`, text =
  `PermaLink.aspx?guid=<id>` under the site root.
  `SyndicationServiceImplementation.cs:320-322`; `SiteUtilities.cs:456-459`
- **RULE-feed-09** — Item link: with `EnableTitlePermaLink=false` (this
  slice's fixed fixture setting), the same `PermaLink.aspx?guid=<id>` shape
  as the guid, via the title-aware overload.
  `SyndicationServiceImplementation.cs:323`; `SiteUtilities.cs:477-500`
- **RULE-feed-10** — Every item unconditionally carries three extension
  elements: `trackback:ping` (=`Trackback.aspx?guid=<id>` under site root),
  `pingback:server` (=`pingback.aspx` under site root),
  `pingback:target` (=item guid text).
  `SyndicationServiceImplementation.cs:338-348`; `SiteUtilities.cs:571-574`
- **RULE-feed-11** — `dc:creator` element is always emitted; its text is
  set only when `entry.Author` resolves via `SiteSecurity.GetUser` to a
  known user (else the element is present but empty). This slice's fixtures
  use an author that never resolves, exercising the empty case; a
  resolvable author is RULE-feed-D5.
  `SyndicationServiceImplementation.cs:350-370`; `SiteSecurity.cs:462-473`
- **RULE-feed-12** — `item.Language` = `entry.Language` verbatim.
  `SyndicationServiceImplementation.cs:433`
- **RULE-feed-13** — Categories: `entry.Categories` split on `;`, each `|`
  replaced with `/`, mapped 1:1 to RSS `<category>` elements; the element
  is omitted entirely when `entry.Categories` is empty.
  `SyndicationServiceImplementation.cs:435-447`
- **RULE-feed-14** — `item.PubDate` = `entry.CreatedUtc` formatted RFC1123
  (`"R"`). Channel `LastBuildDate` = the FIRST item's `PubDate` (i.e. the
  newest entry's date, given RULE-feed-02's sort).
  `SyndicationServiceImplementation.cs:456-460`
- **RULE-feed-15** — Description branch: if NOT
  `siteConfig.AlwaysIncludeContentInRSS` and `entry.Description` is
  non-blank, description = filtered `entry.Description`; otherwise built
  from `entry.Content`, wrapped in a bare `<div>...</div>` when
  `HtmlTidyContent == false` (this slice's fixed fixture setting — the
  `HtmlTidyContent == true` / `ContentFormatter`-tidied path is
  RULE-feed-D8).
  `SyndicationServiceImplementation.cs:463-476`
- **RULE-feed-16** — Content preprocessing, applied to whichever content
  RULE-feed-15 selected: if `EnableAggregatorBugging`, a `0x0` tracking
  `<img>` pointing at the aggregator-bug URL is appended; if
  `EnableRssItemFooters` and a non-empty `RssItemFooter` is configured, that
  footer (preceded by `<br/><hr/>`) is appended. (`ApplyContentFiltersToRSS`'s
  content-filter chain is fixed OFF in every fixture — RULE-feed-D16.)
  `SyndicationServiceBase.cs:75-98`

## Explicitly deferred (named, not silently dropped)

Every one of these is a real rule in the legacy code that this slice does
not characterize or reimplement. A future slice extends `RULES.md` with
these IDs rather than discovering them from scratch.

- **RULE-feed-D1** — category-suffixed channel title
  (`SyndicationServiceImplementation.cs:269-272`)
- **RULE-feed-D2** — `georss:point` element when `EnableGeoRss`
  (`SyndicationServiceImplementation.cs:373-408`)
- **RULE-feed-D3** — comment-related elements (`wfw:comment`,
  `wfw:commentRss`, `slash:comments`) and `item.Comments`, gated by
  `EnableComments` (`SyndicationServiceImplementation.cs:410-432`)
- **RULE-feed-D4** — `<enclosure>` element for the first attachment
  (`SyndicationServiceImplementation.cs:448-455`)
- **RULE-feed-D5** — `dc:creator` populated from a `SiteSecurityConfig`
  user that actually resolves (`SiteSecurity.cs:462-473`)
- **RULE-feed-D6** — `EnableTitlePermaLink` / `EnableTitlePermaLinkUnique`
  / `EnableTitlePermaLinkSpaces` / `ExtensionlessUrls` permalink variants
  (`SiteUtilities.cs:477-500`)
- **RULE-feed-D7** — `EnableUrlRewriting` (the `LINKRW` url-mapper rewrite
  table) (`SiteUtilities.cs:1019-1029`, `web.config:38-41`)
- **RULE-feed-D8** — `HtmlTidyContent == true` path: `ContentFormatter`
  HTML-tidied description plus its extra XHTML `<div>` `anyElement`
  injection (`SyndicationServiceImplementation.cs:477-492`)
- **RULE-feed-D9** — FeedBurner redirect
  (`SyndicationServiceImplementation.cs:172-195`)
- **RULE-feed-D10** — conditional-GET / If-Modified-Since / ETag 304
  short-circuit (`SyndicationServiceImplementation.cs:210-215`)
- **RULE-feed-D11** — referral blacklist check + referral logging
  (`SyndicationServiceImplementation.cs:217-236`)
- **RULE-feed-D12** — 5-minute in-process response cache
  (`SyndicationServiceImplementation.cs:239-241,498`)
- **RULE-feed-D13** — Atom 1.0 generation (`GetAtom`/its core), same
  family, deferred to a possible slice 2
  (`SyndicationServiceImplementation.cs:724` onward)
- **RULE-feed-D14** — `GetRssCategory` / `GetRssWithCounts` (non-default
  counts) / `GetCommentsRss` / `GetEntryCommentsRss` entry-point variants —
  same core, different entry selection
- **RULE-feed-D15** — `BuildEntries`' data-service-driven entry selection:
  category filtering, day/entry-count limiting, `GetEntriesForDay`
  (`SyndicationServiceBase.cs:109-141`) — this slice takes an
  already-built `EntryCollection` as a direct input instead
- **RULE-feed-D16** — `ApplyContentFiltersToRSS`'s content-filter /
  click-through plugin chain (`SiteUtilities.FilterContent`) — fixed OFF
  in every fixture this slice uses
