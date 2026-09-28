using System.Reflection;
using DasBlog.Feed;
using DasBlog.Feed.Rss20;

namespace DasBlog.Feed.Tests;

/// <summary>
/// .NET 10 side of ASOP step 4's gate (asop/strangler-slice-a-feature/v1.yaml).
/// Same two fixtures as
/// source/newtelligence.DasBlog.Web.Services/Test/FeedCharacterization/RssFeedCharacterizationTests.cs
/// (the legacy side), asserted against the SAME committed golden files
/// under modernization/feed/golden/ - not a fresh capture. legacy==golden
/// (proven by the legacy characterization tests, asop/runs/feed/v1/EVIDENCE.md)
/// and modern==golden (proven here) together prove legacy==modern, which is
/// the actual "golden-file diff = 0" claim step 4 requires - see
/// scripts/golden-diff.py, which checks both sides rather than
/// re-implementing a third comparison.
/// </summary>
public class RssFeedCharacterizationTests
{
    [Fact]
    public void TwoPublicEntries_ProduceOrderedPlainFeed()
    {
        // RULE-feed-01 RULE-feed-02 RULE-feed-03 RULE-feed-04
        // RULE-feed-05 RULE-feed-06 RULE-feed-07 RULE-feed-08
        // RULE-feed-09 RULE-feed-10 RULE-feed-11 RULE-feed-12
        // RULE-feed-13 RULE-feed-14 RULE-feed-15
        // RULE-feed-17 RULE-feed-18 RULE-feed-19 (discovered here, at step 4 -
        // see RULES.md; a byte-for-byte AreEqual against the golden file
        // fails if any of these three aren't handled correctly)
        FeedSiteConfig config = PlainSiteConfig();

        List<FeedEntry> entries = new List<FeedEntry>
        {
            MakeEntry("2003-07-31T120000Z", "First post", "unknown-author",
                "General|Misc;Announcements", "en-US",
                new DateTime(2003, 7, 31, 12, 0, 0, DateTimeKind.Utc),
                "A short description.",
                "<p>The full content, unused because Description is set.</p>"),
            MakeEntry("2003-08-01T090000Z", "Second post, newer", "",
                "", "en-US",
                new DateTime(2003, 8, 1, 9, 0, 0, DateTimeKind.Utc),
                "",
                "<p>Full content used because Description is blank.</p>"),
        };

        FeedEntry unpublished = MakeEntry("2003-08-02T000000Z", "Draft", "", "", "en-US",
            new DateTime(2003, 8, 2, 0, 0, 0, DateTimeKind.Utc), "x", "x");
        unpublished.IsPublic = false;
        entries.Add(unpublished);

        FeedEntry unsyndicated = MakeEntry("2003-08-03T000000Z", "Not syndicated", "", "", "en-US",
            new DateTime(2003, 8, 3, 0, 0, 0, DateTimeKind.Utc), "x", "x");
        unsyndicated.Syndicated = false;
        entries.Add(unsyndicated);

        AssertMatchesGolden("two-public-entries", config, entries);
    }

    [Fact]
    public void AggregatorBugAndFooter_AreAppended()
    {
        // RULE-feed-16
        FeedSiteConfig config = PlainSiteConfig();
        config.EnableAggregatorBugging = true;
        config.EnableRssItemFooters = true;
        config.RssItemFooter = "Subscribe at example.test/rss.aspx";

        List<FeedEntry> entries = new List<FeedEntry>
        {
            MakeEntry("2003-07-31T120000Z", "Post with footer and bug", "",
                "", "en-US",
                new DateTime(2003, 7, 31, 12, 0, 0, DateTimeKind.Utc),
                "",
                "<p>Content.</p>"),
        };

        AssertMatchesGolden("aggregator-bug-and-footer", config, entries);
    }

    private static FeedSiteConfig PlainSiteConfig() => new FeedSiteConfig
    {
        Title = "Test Blog",
        Description = "A test blog for feed characterization",
        Subtitle = "Subtitle fallback",
        Copyright = "Copyright 2003, Test Author",
        Contact = "test@example.test",
        Root = "http://example.test/",
        RssLanguage = "en-US",
        AlwaysIncludeContentInRSS = false,
        EnableAggregatorBugging = false,
        EnableRssItemFooters = false,
        ChannelImageUrl = null,
    };

    private static FeedEntry MakeEntry(string id, string title, string author, string categories,
        string language, DateTime createdUtc, string description, string content) => new FeedEntry
        {
            EntryId = id,
            Title = title,
            Author = author,
            Categories = categories,
            Language = language,
            CreatedUtc = createdUtc,
            Description = description,
            Content = content,
        };

    private static void AssertMatchesGolden(string fixtureName, FeedSiteConfig config, List<FeedEntry> entries)
    {
        RssRoot actual = RssFeedBuilder.Build(config, entries);
        string actualXml = RssFeedSerializer.Serialize(actual);

        string goldenPath = GoldenFilePath(fixtureName + ".xml");
        Assert.True(File.Exists(goldenPath), $"No golden file at {goldenPath}. Run the legacy capture first (capture-golden.yml).");

        string expectedXml = File.ReadAllText(goldenPath);
        Assert.Equal(expectedXml, actualXml);
    }

    private static string GoldenFilePath(string fileName)
    {
        string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        DirectoryInfo dir = new DirectoryInfo(binDir);
        // bin/Debug/net10.0 -> DasBlog.Feed.Tests -> src-modern -> repo root
        for (int i = 0; i < 5; i++)
        {
            dir = dir.Parent!;
        }
        return Path.Combine(dir.FullName, "modernization", "feed", "golden", fileName);
    }
}
