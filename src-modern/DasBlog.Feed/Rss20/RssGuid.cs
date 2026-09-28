using System.Xml.Serialization;

namespace DasBlog.Feed.Rss20;

// Ported from the legacy "Guid" class (Rss.cs) - renamed to avoid shadowing
// System.Guid in this port. The XML element name is still "guid"
// ([XmlRoot]/[XmlElement] control the wire shape, not the C# type name).
[XmlRoot("guid")]
public class RssGuid
{
    [XmlAttribute("isPermaLink")]
    public bool IsPermaLink { get; set; }

    [XmlText]
    public string Text { get; set; }
}
