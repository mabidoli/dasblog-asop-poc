using System.Xml;
using System.Xml.Serialization;

namespace DasBlog.Feed.Rss20;

[XmlRoot("channel")]
public class RssChannel
{
    // RULE-feed-17: the legacy channel's default Generator is
    // "newtelligence dasBlog " + the Web.Services assembly's own
    // AssemblyVersion, captured as "newtelligence dasBlog 4.0.0.0" in
    // modernization/feed/golden/*.xml. Reflection over THIS assembly's own
    // version would be meaningless in a ported context, so the value is
    // pinned as a literal, matching what was actually captured - a known
    // coupling to the legacy build's AssemblyInfo.cs at capture time.
    public const string LegacyGeneratorVersionString = "newtelligence dasBlog 4.0.0.0";

    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("description", IsNullable = false)]
    public string Description { get; set; } = "";

    [XmlElement("image")]
    public ChannelImage Image { get; set; }

    [XmlElement("language")]
    public string Language { get; set; }

    [XmlElement("copyright")]
    public string Copyright { get; set; }

    [XmlElement("lastBuildDate")]
    public string LastBuildDate { get; set; }

    [XmlElement("generator")]
    public string Generator { get; set; } = LegacyGeneratorVersionString;

    [XmlElement("managingEditor")]
    public string ManagingEditor { get; set; }

    [XmlElement("webMaster")]
    public string WebMaster { get; set; }

    [XmlElement("item")]
    public List<RssItem> Items { get; set; } = new();

    [XmlAnyElement]
    public XmlElement[] AnyElements;

    [XmlAnyAttribute]
    public XmlAttribute[] AnyAttributes;
}
