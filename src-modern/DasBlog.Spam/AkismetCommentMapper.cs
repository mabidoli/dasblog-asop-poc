using System.Net;

namespace DasBlog.Spam;

/// <summary>
/// .NET 10 port of slice 2 (Akismet spam-check request mapping) per
/// asop/strangler-slice-a-feature/v2.yaml step 4. Every RULE-spam-NN
/// comment cites modernization/spam/RULES.md; ported from
/// AkismetSpamBlockingService.ConvertToAkismetComment in
/// source/newtelligence.DasBlog.Web.Core/AkismetSpamBlockingService.cs:39-73.
/// </summary>
public static class AkismetCommentMapper
{
    public static AkismetComment Convert(Feedback feedback)
    {
        // RULE-spam-01: IPAddress.Parse, falling back to IPAddress.None on
        // null or a parse failure.
        IPAddress ipAddress = IPAddress.None;
        if (feedback.AuthorIPAddress != null)
        {
            try
            {
                ipAddress = IPAddress.Parse(feedback.AuthorIPAddress);
            }
            catch (FormatException) { }
        }

        // RULE-spam-02 (UserAgent, via the constructor).
        AkismetComment comment = new AkismetComment(ipAddress, feedback.AuthorUserAgent);
        comment.Author = feedback.Author; // RULE-spam-03
        comment.AuthorEmail = feedback.AuthorEmail; // RULE-spam-04

        // RULE-spam-05: AuthorUrl parsed only if non-null/non-empty; on
        // UriFormatException it stays unset - no IPAddress.None-style
        // fallback for this field.
        if (feedback.AuthorHomepage != null && feedback.AuthorHomepage.Length > 0)
        {
            try
            {
                comment.AuthorUrl = new Uri(feedback.AuthorHomepage);
            }
            catch (UriFormatException) { }
        }

        comment.Content = feedback.Content; // RULE-spam-06
        comment.Referer = feedback.Referer; // RULE-spam-07

        // RULE-spam-08 / RULE-spam-10: the legacy line
        // (AkismetSpamBlockingService.cs:63) reads
        //   feedback.TargetEntryId != null & feedback.TargetEntryId.Trim().Length > 0
        // using bitwise '&', not short-circuiting '&&'. Reproduced
        // literally (C#'s '&' on bool operands doesn't short-circuit
        // here either) so a null TargetEntryId throws
        // NullReferenceException the same way it does in the legacy
        // build - a real bug this slice preserves rather than silently
        // fixes (see RULES.md RULE-spam-10 and
        // asop/runs/spam/v2/ADJUDICATION.md).
        if (feedback.TargetEntryId != null & feedback.TargetEntryId.Trim().Length > 0)
        {
            // RULE-spam-D1, deferred: the legacy code calls
            // SiteUtilities.GetPermaLinkUrl(feedback.TargetEntryId), which
            // needs SiteConfig.GetSiteConfig() -> a real ASP.NET hosting
            // environment - not modeled in this slice at all (see
            // SLICE-MAP.md). A real caller reaching this branch on the
            // .NET 10 side needs that ported first.
            throw new NotImplementedException(
                "RULE-spam-D1 is deferred: populating Permalink from a " +
                "non-blank TargetEntryId needs SiteUtilities.GetPermaLinkUrl, " +
                "not ported in this slice. See modernization/spam/RULES.md.");
        }

        comment.CommentType = feedback.FeedbackType; // RULE-spam-09
        return comment;
    }
}
