namespace DasBlog.Feed;

/// <summary>
/// A slice-1-scoped stand-in for the legacy Entry class
/// (source/newtelligence.DasBlog.Runtime/Entry.cs) - only the fields
/// RssFeedBuilder actually reads (modernization/feed/RULES.md's in-scope
/// rules). Defaults match the legacy Entry class's own defaults
/// (IsPublic/Syndicated true by default).
/// </summary>
public class FeedEntry
{
    public string EntryId { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }

    /// <summary>Semicolon-separated; '|' means a category-path separator ('/') - RULE-feed-13.</summary>
    public string Categories { get; set; }

    public string Language { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string Description { get; set; }
    public string Content { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool Syndicated { get; set; } = true;
}
