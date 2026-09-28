using System.Text;
using System.Xml;
using System.Xml.Serialization;
using DasBlog.Feed.Rss20;

namespace DasBlog.Feed;

/// <summary>
/// Serializes an RssRoot to the exact text shape the legacy build's own
/// XmlSerializer produces, so the golden-diff gate (ASOP step 4) compares
/// real, comparable XML rather than two documents that only "mean" the
/// same thing.
/// </summary>
public static class RssFeedSerializer
{
    public static string Serialize(RssRoot root)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(RssRoot));
        StringBuilder sb = new StringBuilder();
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            OmitXmlDeclaration = true,
        };
        using (XmlWriter writer = XmlWriter.Create(sb, settings))
        {
            serializer.Serialize(writer, root);
        }
        return NormalizeXsiXsdOrder(sb.ToString());
    }

    // RULE-feed-19: XmlSerializer always auto-declares xmlns:xsi and
    // xmlns:xsd on the root element (neither namespace is ever actually
    // USED by anything this slice writes), but .NET Framework and .NET 10
    // emit them in the OPPOSITE order (xsd-then-xsi vs xsi-then-xsd) -
    // confirmed by trying every reachable insertion order on the .NET 10
    // side and getting xsi-then-xsd regardless, so it's the runtime
    // forcing it, not something this code controls. Since the two
    // declarations carry no semantic content for an RSS 2.0 document, the
    // .NET 10 output is normalized to match the legacy order textually
    // rather than treated as a real divergence.
    private static string NormalizeXsiXsdOrder(string xml)
    {
        const string dotnetOrder =
            "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"";
        const string legacyOrder =
            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";
        return xml.Replace(dotnetOrder, legacyOrder);
    }
}
