using System.Net;

namespace DasBlog.Spam;

/// <summary>
/// Ported from Subtext.Akismet.Comment (source/Subtext.Akismet/Comment.cs)
/// - a plain POCO, no XmlSerializer/framework-specific attributes to carry
/// over.
/// </summary>
public class AkismetComment
{
    public AkismetComment(IPAddress ipAddress, string userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public string Author { get; set; }
    public string AuthorEmail { get; set; }
    public Uri AuthorUrl { get; set; }
    public string Content { get; set; }
    public string Referer { get; set; }
    public Uri Permalink { get; set; }
    public string UserAgent { get; }
    public string CommentType { get; set; }
    public IPAddress IpAddress { get; }
}
