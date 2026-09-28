# Slice map — comment/trackback/pingback spam-check request mapping (slice 2)

Written per ASOP step 1 (`asop/strangler-slice-a-feature/v2.yaml`). Per v2's
PARK-AND-CONTINUE semantics: this step's gate (human, mabidoli) is still
open at the time steps 2-6 below executed — see this run's
`asop/runs/spam/v2/EVIDENCE.md` for the explicit PROVISIONAL marking that
applies as a result.

## Why this slice, over trackback/pingback parsing

Both "comment spam filter" and "trackback/pingback parsing" were offered as
slice 2 candidates. A closer read of both (not just their names) found:

- **Trackback** (`TrackbackHandler.ProcessRequest`,
  `source/newtelligence.DasBlog.Web.Services/TrackbackHandler.cs:41-140`)
  takes an `HttpContext` directly as a parameter (a cleaner seam than
  pingback's inherited `Context` property, on paper) but makes a **live
  outbound HTTP GET** to verify the backlink
  (`WebRequest.Create(url).GetResponse()`, line ~107) — a real network
  dependency inside the business logic itself, not just at the transport
  edge. Deterministic without either mocking `WebRequest` globally or
  standing up a local HTTP listener as a fixture.
- **Pingback** (`PingbackAPI.ping`,
  `source/newtelligence.DasBlog.Web.Services/PingbackAPI.cs:77-221`) makes
  no live network call, but its ONLY constructor
  (`PingbackAPI.cs:69-74`) unconditionally calls
  `SiteConfig.GetSiteConfig()` →
  `CacheFactory.GetCache()` → `new HttpCache()`
  (`source/newtelligence.DasBlog.Web.Core/HttpCache.cs:112-127`), which
  reads `System.Web.Hosting.HostingEnvironment.Cache` and throws
  `NotSupportedException` outside a real ASP.NET hosting environment. Slice
  1's equivalent problem had an escape hatch (an alternate constructor
  taking `SiteConfig`/`IBlogDataService`/`ILoggingDataService` directly,
  bypassing the whole chain); `PingbackAPI` has no such constructor.
  Reaching it would need either a genuine in-process ASP.NET host
  (`System.Web.Hosting.ApplicationHost`, untested, uncertain on a CI
  runner) or a test-only constructor added to the legacy class itself.
- **The Akismet spam-check request mapping**
  (`AkismetSpamBlockingService.ConvertToAkismetComment`,
  `source/newtelligence.DasBlog.Web.Core/AkismetSpamBlockingService.cs:39-73`)
  has **neither problem**: `AkismetSpamBlockingService`'s constructor
  (`AkismetSpamBlockingService(string apiKey, string blogUrl)`, line 16)
  takes no ambient state at all, and `ConvertToAkismetComment` itself only
  touches `HostingEnvironment`/`SiteConfig` on ONE conditional path (line
  67, only when `feedback.TargetEntryId` is non-blank) — a path this
  slice defers (RULE-spam-D1) exactly the way slice 1 deferred
  `HttpContext`-coupled branches, rather than needing a workaround for the
  WHOLE class. The live Akismet HTTP API itself
  (`AkismetClient.CheckCommentForSpam`/`SubmitSpam`/`SubmitHam`) is a
  separate concern this slice also defers (RULE-spam-D2) — this slice
  characterizes the REQUEST-BUILDING mapping, not the transport, the same
  boundary slice 1 drew between "build the feed XML" and "serve it over
  ASMX."

**Chosen: the Akismet spam-check request mapping.** Cleaner seams (no
HttpContext, no ASP.NET hosting environment needed for the in-scope
surface) and a genuinely deterministic oracle (a pure `IFeedback` →
`IComment` mapping function, invoked via reflection since it's `private`
— the same reflection-based testing technique already used in slice 1 for
`SyndicationServiceBase`'s `cache` field).

## Entry point chain

1. `source/newtelligence.DasBlog.Web.Core/AkismetSpamBlockingService.cs:12`
   — `AkismetSpamBlockingService : ISpamBlockingService`, the class real
   callers (`PingbackAPI.cs:150`, `TrackbackHandler.cs`, comment-posting
   code) reach through `SiteConfig.SpamBlockingService`
   (an `ISpamBlockingService`-typed property, wired up per-site).
2. `AkismetSpamBlockingService.cs:21-37` — `IsSpam`/`ReportSpam`/
   `ReportNotSpam`, the three public methods `ISpamBlockingService`
   declares. All three build their Akismet payload the same way, then call
   one of `AkismetClient`'s three methods (`CheckCommentForSpam`,
   `SubmitSpam`, `SubmitHam` — `source/Subtext.Akismet/AkismetClient.cs:159-189`).
3. `AkismetSpamBlockingService.cs:39-73` — `ConvertToAkismetComment`.
   **This slice's boundary**: the mapping from an `IFeedback`
   (`source/newtelligence.DasBlog.Runtime/IFeedback.cs` — implemented by
   `Entry`/`Comment`/`Tracking`, i.e. blog comments, trackbacks, and
   pingbacks all funnel through the SAME mapping) to a
   `Subtext.Akismet.Comment` (`source/Subtext.Akismet/Comment.cs`).

## Dependencies touched

- **In scope** (pure, no external state):
  `System.Net.IPAddress.Parse`, `System.Uri`'s constructor (both BCL,
  deterministic), `Subtext.Akismet.Comment`'s constructor/properties
  (a plain POCO, `source/Subtext.Akismet/Comment.cs`).
- **Out of scope, deferred**:
  - `SiteUtilities.GetPermaLinkUrl(string)` (single-arg overload,
    `source/newtelligence.DasBlog.Web.Core/SiteUtilities.cs:354-356`) —
    only reached when `feedback.TargetEntryId` is non-blank
    (RULE-spam-D1).
  - `AkismetClient.CheckCommentForSpam`/`SubmitSpam`/`SubmitHam`
    (`source/Subtext.Akismet/AkismetClient.cs:159-189`) — the live HTTP
    call to `akismet.com` (RULE-spam-D2).
  - `IComment.ServerEnvironmentVariables` — declared on the interface,
    never populated by `ConvertToAkismetComment` (stays the `Comment`
    class's own default empty collection) — not a mapped field, noted for
    completeness (RULE-spam-D3).

## Data touched

- `IFeedback` (read-only in this slice): `Author`, `AuthorEmail`,
  `AuthorHomepage`, `AuthorIPAddress`, `AuthorUserAgent`, `FeedbackType`,
  `Content`, `Referer`, `TargetEntryId`.
- `Subtext.Akismet.Comment` / `IComment` (write-only output in this
  slice): `Author`, `AuthorEmail`, `AuthorUrl`, `Content`, `Referer`,
  `Permalink`, `UserAgent`, `CommentType`, `IpAddress`.
