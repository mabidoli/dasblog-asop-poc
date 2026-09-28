using System.Xml.Serialization;

namespace DasBlog.Feed.Rss20;

[XmlRoot("category")]
public class RssCategory
{
    [XmlText]
    public string Text { get; set; }
}
