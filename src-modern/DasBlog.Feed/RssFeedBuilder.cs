using System.Globalization;
using System.Xml;
using DasBlog.Feed.Rss20;

namespace DasBlog.Feed;

/// <summary>
/// .NET 10 port of slice 1 (RSS 2.0 feed generation) per
/// asop/strangler-slice-a-feature/v1.yaml step 4. Every RULE-feed-NN
/// comment below cites modernization/feed/RULES.md; ported from
/// GetRssCore in
/// source/newtelligence.DasBlog.Web.Services/SyndicationServiceImplementation.cs:196-501
/// (see modernization/feed/SLICE-MAP.md for exactly what was and wasn't
/// carried over).
/// </summary>
public static class RssFeedBuilder
{
    public static RssRoot Build(FeedSiteConfig config, IEnumerable<FeedEntry> entries)
    {
        // RULE-feed-01 (inclusion) + RULE-feed-02 (newest-first order).
        List<FeedEntry> ordered = entries
            .Where(e => e.IsPublic && e.Syndicated)
            .OrderByDescending(e => e.CreatedUtc)
            .ToList();

        RssRoot root = new RssRoot();
        // RULE-feed-18: the xmlns declaration order on <rss> in the legacy
        // golden files is trackback, dc, pingback - NOT the dc, trackback,
        // pingback source declaration order
        // (SyndicationServiceImplementation.cs:250-252). Matching the
        // captured order explicitly here (an XmlSerializerNamespaces
        // enumeration-order artifact, not a business rule) is what gets a
        // byte-identical golden diff; xsi/xsd, which XmlSerializer
        // auto-declares on every root element regardless of what this code
        // adds, are handled separately in RssFeedSerializer (RULE-feed-19 -
        // their relative order is a runtime difference, not something this
        // builder controls).
        root.Namespaces.Add("trackback", "http://madskills.com/public/xml/rss/module/trackback/");
        root.Namespaces.Add("dc", "http://purl.org/dc/elements/1.1/");
        root.Namespaces.Add("pingback", "http://madskills.com/public/xml/rss/module/pingback/");
        // RULE-feed-D3/RULE-feed-D2: wfw/slash/georss namespaces deferred
        // (EnableComments/EnableGeoRss are out of this slice's model).

        RssChannel channel = new RssChannel();
        channel.Title = config.Title; // category-suffixed title is RULE-feed-D1, deferred.
        channel.Description = string.IsNullOrWhiteSpace(config.Description) // RULE-feed-04
            ? config.Subtitle
            : config.Description;
        channel.Link = RelativeToRoot(config, ""); // RULE-feed-05
        channel.Copyright = config.Copyright; // RULE-feed-06
        channel.ManagingEditor = config.Contact;
        channel.WebMaster = config.Contact;
        if (!string.IsNullOrEmpty(config.RssLanguage)) // RULE-feed-07
        {
            channel.Language = config.RssLanguage;
        }
        channel.Image = null;
        if (!string.IsNullOrWhiteSpace(config.ChannelImageUrl))
        {
            channel.Image = new ChannelImage
            {
                Title = channel.Title,
                Link = channel.Link,
                Url = config.ChannelImageUrl.StartsWith("http")
                    ? config.ChannelImageUrl
                    : RelativeToRoot(config, config.ChannelImageUrl),
            };
        }

        XmlDocument scratch = new XmlDocument();

        foreach (FeedEntry entry in ordered)
        {
            RssItem item = new RssItem();
            string permaLink = GetPermaLinkUrl(config, entry.EntryId);

            RssGuid guid = new RssGuid { IsPermaLink = false, Text = permaLink }; // RULE-feed-08
            item.Guid = guid;
            item.Link = permaLink; // RULE-feed-09 (EnableTitlePermaLink=false path)

            // RULE-feed-10: unconditional trackback/pingback extension elements,
            // plus dc:creator (RULE-feed-11) - all via AnyElements, matching the
            // legacy code's own construction via anyElements.Add(...).
            List<XmlElement> anyElements = new List<XmlElement>();

            XmlElement trackbackPing = scratch.CreateElement(
                "trackback", "ping", "http://madskills.com/public/xml/rss/module/trackback/");
            trackbackPing.InnerText = GetTrackbackUrl(config, entry.EntryId);
            anyElements.Add(trackbackPing);

            XmlElement pingbackServer = scratch.CreateElement(
                "pingback", "server", "http://madskills.com/public/xml/rss/module/pingback/");
            pingbackServer.InnerText = new Uri(new Uri(RelativeToRoot(config, "")), "pingback.aspx").ToString();
            anyElements.Add(pingbackServer);

            XmlElement pingbackTarget = scratch.CreateElement(
                "pingback", "target", "http://madskills.com/public/xml/rss/module/pingback/");
            pingbackTarget.InnerText = guid.Text;
            anyElements.Add(pingbackTarget);

            XmlElement dcCreator = scratch.CreateElement("dc", "creator", "http://purl.org/dc/elements/1.1/");
            // RULE-feed-11: entry.Author never resolves to a known user in this
            // slice (no SiteSecurityConfig port - RULE-feed-D5, deferred), so
            // dcCreator's InnerText stays unset; the element is still added,
            // matching the legacy code's unconditional anyElements.Add(dcCreator).
            anyElements.Add(dcCreator);

            item.AnyElements = anyElements.ToArray();

            item.Title = entry.Title;
            item.Language = entry.Language; // RULE-feed-12

            if (!string.IsNullOrEmpty(entry.Categories)) // RULE-feed-13
            {
                item.Categories = entry.Categories
                    .Split(';')
                    .Select(c => new RssCategory { Text = c.Replace('|', '/') })
                    .ToList();
            }

            item.PubDate = entry.CreatedUtc.ToString("R", CultureInfo.InvariantCulture); // RULE-feed-14
            if (string.IsNullOrEmpty(channel.LastBuildDate))
            {
                channel.LastBuildDate = item.PubDate;
            }

            if (!config.AlwaysIncludeContentInRSS && !string.IsNullOrWhiteSpace(entry.Description)) // RULE-feed-15
            {
                item.Description = PreprocessItemContent(config, entry.EntryId, entry.Description);
            }
            else
            {
                // config.HtmlTidyContent==true's ContentFormatter-tidied path is
                // RULE-feed-D8, deferred; this slice only ports the "false" branch.
                item.Description = "<div>" + PreprocessItemContent(config, entry.EntryId, entry.Content) + "</div>";
            }

            channel.Items.Add(item);
        }

        root.Channels.Add(channel);
        return root;
    }

    private static string PreprocessItemContent(FeedSiteConfig config, string entryId, string content)
    {
        // config.ApplyContentFiltersToRSS==true's filter chain is
        // RULE-feed-D16, deferred - not modeled in FeedSiteConfig at all.
        if (config.EnableAggregatorBugging) // RULE-feed-16
        {
            content += $"<img width=\"0\" height=\"0\" src=\"{GetAggregatorBugUrl(config, entryId)}\"/>";
        }
        if (config.EnableRssItemFooters && !string.IsNullOrEmpty(config.RssItemFooter)) // RULE-feed-16
        {
            content += "<br/><hr/>" + config.RssItemFooter;
        }
        return content;
    }

    private static string RelativeToRoot(FeedSiteConfig config, string relative)
        => new Uri(new Uri(config.Root), relative).AbsoluteUri;

    // EnableUrlRewriting==true's LINKRW rewrite table is RULE-feed-D7,
    // deferred - this always takes the "rewriting off" path.
    private static string GetPermaLinkUrl(FeedSiteConfig config, string id)
        => RelativeToRoot(config, "PermaLink.aspx?guid=" + id);

    private static string GetTrackbackUrl(FeedSiteConfig config, string id)
        => RelativeToRoot(config, "Trackback.aspx?guid=" + id);

    private static string GetAggregatorBugUrl(FeedSiteConfig config, string entryId)
        => RelativeToRoot(config, "aggbug.ashx?id=" + entryId);
}
