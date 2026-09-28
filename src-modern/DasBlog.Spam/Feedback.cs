namespace DasBlog.Spam;

/// <summary>
/// A slice-2-scoped stand-in for the legacy IFeedback interface
/// (source/newtelligence.DasBlog.Runtime/IFeedback.cs) - implemented by
/// Entry/Comment/Tracking in the legacy codebase (blog comments,
/// trackbacks, and pingbacks all funnel through the same mapping this
/// slice ports). Field-for-field, same names.
/// </summary>
public class Feedback
{
    public string Author { get; set; }
    public string AuthorEmail { get; set; }
    public string AuthorHomepage { get; set; }
    public string AuthorIPAddress { get; set; }
    public string AuthorUserAgent { get; set; }
    public string FeedbackType { get; set; }
    public string Content { get; set; }
    public string Referer { get; set; }
    public string TargetEntryId { get; set; }
}
