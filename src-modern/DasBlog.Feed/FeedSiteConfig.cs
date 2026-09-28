namespace DasBlog.Feed;

/// <summary>
/// A slice-1-scoped stand-in for the legacy SiteConfig class
/// (source/newtelligence.DasBlog.Web.Core/SiteConfig.cs) - only the fields
/// RssFeedBuilder actually reads. Several legacy flags this slice defers
/// (modernization/feed/RULES.md's RULE-feed-D* ids) are NOT modeled here at
/// all - EnableComments, EnableGeoRss, EnableTitlePermaLink,
/// ExtensionlessUrls, EnableUrlRewriting, HtmlTidyContent=true,
/// ApplyContentFiltersToRSS=true. Building a feed that needs any of those
/// is out of this slice's scope by construction, not by a runtime flag
/// this type would let you flip.
/// </summary>
public class FeedSiteConfig
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Subtitle { get; set; }
    public string Copyright { get; set; }
    public string Contact { get; set; }

    /// <summary>The site's root URL, e.g. "http://example.test/". Must end in '/'.</summary>
    public string Root { get; set; }

    public string RssLanguage { get; set; }
    public string ChannelImageUrl { get; set; }
    public bool AlwaysIncludeContentInRSS { get; set; }
    public bool EnableAggregatorBugging { get; set; }
    public bool EnableRssItemFooters { get; set; }
    public string RssItemFooter { get; set; }
}
