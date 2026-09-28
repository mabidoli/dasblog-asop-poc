using System.Xml.Serialization;

namespace DasBlog.Feed.Rss20;

// RULE-feed-D-image: RssChannel.Image is only populated when
// SiteConfig.ChannelImageUrl is set - out of slice 1's fixtures (always
// null/omitted). Ported for structural completeness of the RssRoot graph.
public class ChannelImage
{
    [XmlElement("url")]
    public string Url { get; set; }

    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }
}
