# Business rules — Akismet spam-check request mapping (slice 2)

Written per ASOP step 2. Every rule cites the exact legacy line. In-scope
rules must each be referenced by at least one characterization test
(`scripts/check-rules-have-tests.py` — the step-2 gate).

## In scope (characterized in this slice)

- **RULE-spam-01** — `IpAddress` is parsed from `feedback.AuthorIPAddress`
  via `IPAddress.Parse`; falls back to `IPAddress.None` when
  `AuthorIPAddress` is null OR fails to parse (`FormatException` is
  swallowed).
  `AkismetSpamBlockingService.cs:41-49`
- **RULE-spam-02** — `UserAgent` copied verbatim from
  `feedback.AuthorUserAgent` (via the `Comment` constructor).
  `AkismetSpamBlockingService.cs:50`
- **RULE-spam-03** — `Author` copied verbatim.
  `AkismetSpamBlockingService.cs:51`
- **RULE-spam-04** — `AuthorEmail` copied verbatim.
  `AkismetSpamBlockingService.cs:52`
- **RULE-spam-05** — `AuthorUrl` is parsed from `feedback.AuthorHomepage`
  as a `Uri` ONLY when `AuthorHomepage` is non-null and non-empty; on
  `UriFormatException` it is swallowed and `AuthorUrl` stays unset (null)
  — there is no fallback value, unlike RULE-spam-01's `IPAddress.None`.
  `AkismetSpamBlockingService.cs:53-60`
- **RULE-spam-06** — `Content` copied verbatim.
  `AkismetSpamBlockingService.cs:61`
- **RULE-spam-07** — `Referer` copied verbatim.
  `AkismetSpamBlockingService.cs:62`
- **RULE-spam-08** — `Permalink` stays unset when `feedback.TargetEntryId`
  is a non-null string that is blank after `Trim()` (the "empty, but not
  null" path only — the "populated" path is RULE-spam-D1, deferred; the
  null path is RULE-spam-10, below — not the same code path).
  `AkismetSpamBlockingService.cs:63-70`
- **RULE-spam-09** — `CommentType` copied verbatim from
  `feedback.FeedbackType`.
  `AkismetSpamBlockingService.cs:71`
- **RULE-spam-10** — a real bug, found by characterizing this method, not
  invented: line 63 reads
  `feedback.TargetEntryId != null & feedback.TargetEntryId.Trim().Length > 0`
  using bitwise `&`, not short-circuiting `&&`. Both operands evaluate
  even when `TargetEntryId` IS null, so `.Trim()` runs on a null
  reference and the method throws `NullReferenceException` (wrapped in
  `TargetInvocationException` when invoked via reflection, as this
  slice's tests do) instead of gracefully treating a null `TargetEntryId`
  the same as an empty one. Characterized AS-IS per Strangler Fig's own
  discipline — this slice preserves observable behaviour, bugs included,
  until a separate, explicit decision changes it; the .NET 10 port
  (step 4) reproduces the same throw rather than silently fixing it.
  `AkismetSpamBlockingService.cs:63`

## Explicitly deferred (named, not silently dropped)

- **RULE-spam-D1** — `Permalink` populated from a non-blank
  `TargetEntryId` via `SiteUtilities.GetPermaLinkUrl(string)`
  (`SiteUtilities.cs:354-356`), which calls `SiteConfig.GetSiteConfig()`
  → `CacheFactory.GetCache()` → `HostingEnvironment.Cache`
  (`HttpCache.cs:112-127`) — throws `NotSupportedException` outside a
  real ASP.NET hosting environment, which this slice's tests don't stand
  up (see `SLICE-MAP.md`'s "why this slice" section).
- **RULE-spam-D2** — the live Akismet HTTP API itself
  (`AkismetClient.CheckCommentForSpam`/`SubmitSpam`/`SubmitHam`,
  `source/Subtext.Akismet/AkismetClient.cs:159-189`) — this slice
  characterizes the request-BUILDING mapping, not the transport.
- **RULE-spam-D3** — `IComment.ServerEnvironmentVariables` — declared on
  the interface, never populated by `ConvertToAkismetComment`; noted for
  completeness, not a mapped field.
