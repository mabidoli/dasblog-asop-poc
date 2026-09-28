using System.Reflection;
using DasBlog.Spam;

namespace DasBlog.Spam.Tests;

/// <summary>
/// .NET 10 side of ASOP step 4's gate (asop/strangler-slice-a-feature/v2.yaml),
/// slice 2 (spam). Same three golden-comparison fixtures as the legacy
/// side (source/newtelligence.DasBlog.Web.Core/Test/AkismetCommentMappingCharacterizationTests.cs),
/// asserted against the SAME committed golden files under
/// modernization/spam/golden/, plus the same RULE-spam-10 crash
/// characterization test - the .NET 10 port reproduces the legacy bug
/// faithfully rather than fixing it silently (see AkismetCommentMapper.cs).
/// </summary>
public class AkismetCommentMappingCharacterizationTests
{
    [Fact]
    public void FullFeedback_WithoutTargetEntryId_MapsEveryField()
    {
        // RULE-spam-01 RULE-spam-02 RULE-spam-03 RULE-spam-04
        // RULE-spam-05 RULE-spam-06 RULE-spam-07 RULE-spam-08
        // RULE-spam-09
        Feedback feedback = new Feedback
        {
            Author = "Jane Commenter",
            AuthorEmail = "jane@example.test",
            AuthorHomepage = "http://jane.example.test/",
            AuthorIPAddress = "203.0.113.7",
            AuthorUserAgent = "Mozilla/5.0 (characterization fixture)",
            FeedbackType = "comment",
            Content = "Great post!",
            Referer = "http://referrer.example.test/",
            TargetEntryId = "",
        };

        AssertMatchesGolden("full-feedback-no-target-entry", feedback);
    }

    [Fact]
    public void MalformedIpAndHomepage_FallBackGracefully()
    {
        // RULE-spam-01 (fallback to IPAddress.None) RULE-spam-05
        // (AuthorUrl stays unset, no fallback)
        Feedback feedback = new Feedback
        {
            Author = "Spammer",
            AuthorEmail = "spam@example.test",
            AuthorHomepage = "not a valid uri",
            AuthorIPAddress = "not-an-ip-address",
            AuthorUserAgent = "curl/8.0",
            FeedbackType = "trackback",
            Content = "buy pills now",
            Referer = null,
            TargetEntryId = "   ",
        };

        AssertMatchesGolden("malformed-ip-and-homepage", feedback);
    }

    [Fact]
    public void MinimalFeedback_NullOptionalFields_DoesNotThrow()
    {
        // RULE-spam-01 (null AuthorIPAddress -> IPAddress.None)
        // RULE-spam-05 (null AuthorHomepage -> AuthorUrl unset)
        Feedback feedback = new Feedback
        {
            Author = null,
            AuthorEmail = null,
            AuthorHomepage = null,
            AuthorIPAddress = null,
            AuthorUserAgent = null,
            FeedbackType = "pingback",
            Content = null,
            Referer = null,
            TargetEntryId = "",
        };

        AssertMatchesGolden("minimal-feedback-nulls", feedback);
    }

    [Fact]
    public void NullTargetEntryId_ThrowsNullReferenceException()
    {
        // RULE-spam-10 - the .NET 10 port reproduces this legacy bug
        // faithfully (AkismetCommentMapper.cs's own comment explains why).
        Feedback feedback = new Feedback { Author = "Someone", FeedbackType = "comment", TargetEntryId = null };

        Assert.Throws<NullReferenceException>(() => AkismetCommentMapper.Convert(feedback));
    }

    private static void AssertMatchesGolden(string fixtureName, Feedback feedback)
    {
        AkismetComment actual = AkismetCommentMapper.Convert(feedback);
        string actualText = AkismetCommentSerializer.Serialize(actual);

        string goldenPath = GoldenFilePath(fixtureName + ".txt");
        Assert.True(File.Exists(goldenPath), $"No golden file at {goldenPath}. Run the legacy capture first (capture-golden.yml).");

        string expectedText = File.ReadAllText(goldenPath);
        Assert.Equal(expectedText, actualText);
    }

    private static string GoldenFilePath(string fileName)
    {
        string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        DirectoryInfo dir = new DirectoryInfo(binDir);
        // bin/Debug/net10.0 -> DasBlog.Spam.Tests -> src-modern -> repo root
        for (int i = 0; i < 5; i++)
        {
            dir = dir.Parent!;
        }
        return Path.Combine(dir.FullName, "modernization", "spam", "golden", fileName);
    }
}
