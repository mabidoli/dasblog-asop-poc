using System.Xml;
using System.Xml.Serialization;

namespace DasBlog.Feed.Rss20;

// Element order is pinned with explicit [XmlElement(Order=...)]/[XmlAnyElement(Order=...)]
// rather than left to declaration order. Empirically (see
// modernization/feed/golden/*.xml, captured from the real legacy build),
// legacy's AnyElements-sourced content (trackback/pingback/dc:creator) is
// serialized BEFORE the named elements even though it's declared LAST in
// the legacy class - an XmlSerializer quirk this port does not try to
// reproduce implicitly. Matching the observed order explicitly is the
// reliable way to hit a byte-identical golden diff regardless of runtime
// version.
[XmlRoot("item")]
public class RssItem
{
    [XmlAttribute("xml:lang")]
    public string Language { get; set; }

    [XmlAnyElement(Order = 0)]
    public XmlElement[] AnyElements { get; set; }

    [XmlElement("author", Order = 1)]
    public string Author { get; set; }

    [XmlElement("title", Order = 2)]
    public string Title { get; set; }

    [XmlElement("guid", Order = 3)]
    public RssGuid Guid { get; set; }

    [XmlElement("link", Order = 4)]
    public string Link { get; set; }

    [XmlElement("pubDate", Order = 5)]
    public string PubDate { get; set; }

    [XmlElement("description", Order = 6)]
    public string Description { get; set; }

    [XmlElement("comments", Order = 7)]
    public string Comments { get; set; }

    [XmlElement("category", Order = 8)]
    public List<RssCategory> Categories { get; set; }

    [XmlElement("enclosure", Order = 9)]
    public Enclosure Enclosure { get; set; }

    [XmlAnyAttribute]
    public XmlAttribute[] AnyAttributes;
}
