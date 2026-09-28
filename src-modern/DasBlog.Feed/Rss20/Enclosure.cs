using System.Xml.Serialization;

namespace DasBlog.Feed.Rss20;

// RULE-feed-D4 (deferred): enclosures require entry.Attachments, out of
// slice 1's scope. Ported for structural completeness of the RssItem graph.
[XmlRoot("enclosure")]
public class Enclosure
{
    [XmlAttribute("url")]
    public string Url { get; set; }

    [XmlAttribute("type")]
    public string Type { get; set; }

    [XmlAttribute("length")]
    public string Length { get; set; }
}
