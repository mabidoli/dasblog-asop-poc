// Ported from source/newtelligence.DasBlog.Web.Services/Rss.cs (legacy,
// namespace newtelligence.DasBlog.Web.Services.Rss20) for
// asop/strangler-slice-a-feature/v1.yaml step 4. Structurally identical to
// the legacy XmlSerializer shape on purpose - reusing the same
// [XmlElement]/[XmlRoot] contract is what lets the golden-diff gate compare
// real XML instead of guessing at equivalence. Renamed where a legacy type
// name would shadow a BCL one (Guid -> RssGuid); otherwise field-for-field.
using System.Xml;
using System.Xml.Serialization;

namespace DasBlog.Feed.Rss20;

[XmlType(Namespace = "", IncludeInSchema = false)]
[XmlRoot("rss", Namespace = "")]
public class RssRoot
{
    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces Namespaces { get; set; } = new XmlSerializerNamespaces();

    [XmlAttribute("version")]
    public string Version { get; set; } = "2.0";

    [XmlElement("channel")]
    public List<RssChannel> Channels { get; set; } = new();

    [XmlAnyElement]
    public XmlElement[] AnyElements;

    [XmlAnyAttribute]
    public XmlAttribute[] AnyAttributes;
}
