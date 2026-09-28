using System.Text;

namespace DasBlog.Spam;

/// <summary>
/// Same canonical text shape as the legacy characterization tests'
/// Serialize method
/// (source/newtelligence.DasBlog.Web.Core/Test/AkismetCommentMappingCharacterizationTests.cs)
/// - a plain field dump, not XML, since Subtext.Akismet.Comment carries no
/// serialization attributes to reuse (unlike slice 1's Rss20 types).
/// </summary>
public static class AkismetCommentSerializer
{
    public static string Serialize(AkismetComment comment)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Author: ").Append(Or(comment.Author)).Append('\n');
        sb.Append("AuthorEmail: ").Append(Or(comment.AuthorEmail)).Append('\n');
        sb.Append("AuthorUrl: ").Append(comment.AuthorUrl == null ? "(null)" : comment.AuthorUrl.ToString()).Append('\n');
        sb.Append("Content: ").Append(Or(comment.Content)).Append('\n');
        sb.Append("Referer: ").Append(Or(comment.Referer)).Append('\n');
        sb.Append("Permalink: ").Append(comment.Permalink == null ? "(null)" : comment.Permalink.ToString()).Append('\n');
        sb.Append("UserAgent: ").Append(Or(comment.UserAgent)).Append('\n');
        sb.Append("CommentType: ").Append(Or(comment.CommentType)).Append('\n');
        sb.Append("IpAddress: ").Append(comment.IpAddress.ToString()).Append('\n');
        return sb.ToString();
    }

    private static string Or(string value) => value == null ? "(null)" : value;
}
