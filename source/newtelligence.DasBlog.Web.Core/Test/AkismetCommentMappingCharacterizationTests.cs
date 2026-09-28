using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using newtelligence.DasBlog.Runtime;
using Subtext.Akismet;
using AkismetComment = Subtext.Akismet.Comment;

namespace newtelligence.DasBlog.Web.Core.Test
{
	/// <summary>
	/// Fixture IFeedback for slice 2 (modernization/spam/) - a plain,
	/// non-ASP.NET-coupled stand-in for the real implementers (Entry,
	/// Comment, Tracking), all of which funnel through the same
	/// AkismetSpamBlockingService.ConvertToAkismetComment mapping this
	/// slice characterizes.
	/// </summary>
	public class FakeFeedback : IFeedback
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

	/// <summary>
	/// Characterization tests for slice 2 (Akismet spam-check request
	/// mapping), per asop/strangler-slice-a-feature/v2.yaml step 3. Every
	/// test method's comment names the RULE-spam-NN business rules
	/// (modernization/spam/RULES.md) it exercises.
	///
	/// ConvertToAkismetComment is private - invoked via reflection, same
	/// technique already used in slice 1
	/// (FeedCharacterization/RssFeedCharacterizationTests.cs) to reach
	/// SyndicationServiceBase's protected `cache` field. AkismetSpamBlockingService's
	/// own constructor takes no ambient state (no SiteConfig, no
	/// HttpContext, no HostingEnvironment) - see SLICE-MAP.md for why this
	/// slice was chosen over trackback/pingback parsing.
	///
	/// Run with DASBLOG_CAPTURE_GOLDEN=1 to (re)write a fixture's golden
	/// file from this build's actual output instead of comparing against
	/// it - same convention as slice 1.
	/// </summary>
	[TestFixture]
	public class AkismetCommentMappingCharacterizationTests
	{
		[Test]
		public void FullFeedback_WithoutTargetEntryId_MapsEveryField()
		{
			// RULE-spam-01 RULE-spam-02 RULE-spam-03 RULE-spam-04
			// RULE-spam-05 RULE-spam-06 RULE-spam-07 RULE-spam-08
			// RULE-spam-09
			FakeFeedback feedback = new FakeFeedback();
			feedback.Author = "Jane Commenter";
			feedback.AuthorEmail = "jane@example.test";
			feedback.AuthorHomepage = "http://jane.example.test/";
			feedback.AuthorIPAddress = "203.0.113.7";
			feedback.AuthorUserAgent = "Mozilla/5.0 (characterization fixture)";
			feedback.FeedbackType = "comment";
			feedback.Content = "Great post!";
			feedback.Referer = "http://referrer.example.test/";
			// Empty, NOT null - RULE-spam-10 is what happens when it's null
			// (a real bug: '&' not '&&' at AkismetSpamBlockingService.cs:63
			// means a null TargetEntryId throws instead of being treated as
			// blank). RULE-spam-D1 (populated Permalink) stays deferred.
			feedback.TargetEntryId = "";

			AssertMatchesGolden("full-feedback-no-target-entry", feedback);
		}

		[Test]
		public void MalformedIpAndHomepage_FallBackGracefully()
		{
			// RULE-spam-01 (fallback to IPAddress.None) RULE-spam-05
			// (AuthorUrl stays unset, no fallback)
			FakeFeedback feedback = new FakeFeedback();
			feedback.Author = "Spammer";
			feedback.AuthorEmail = "spam@example.test";
			feedback.AuthorHomepage = "not a valid uri";
			feedback.AuthorIPAddress = "not-an-ip-address";
			feedback.AuthorUserAgent = "curl/8.0";
			feedback.FeedbackType = "trackback";
			feedback.Content = "buy pills now";
			feedback.Referer = null;
			feedback.TargetEntryId = "   "; // blank after Trim() - still the empty path

			AssertMatchesGolden("malformed-ip-and-homepage", feedback);
		}

		[Test]
		public void MinimalFeedback_NullOptionalFields_DoesNotThrow()
		{
			// RULE-spam-01 (null AuthorIPAddress -> IPAddress.None)
			// RULE-spam-05 (null AuthorHomepage -> AuthorUrl unset)
			FakeFeedback feedback = new FakeFeedback();
			feedback.Author = null;
			feedback.AuthorEmail = null;
			feedback.AuthorHomepage = null;
			feedback.AuthorIPAddress = null;
			feedback.AuthorUserAgent = null;
			feedback.FeedbackType = "pingback";
			feedback.Content = null;
			feedback.Referer = null;
			feedback.TargetEntryId = "";

			AssertMatchesGolden("minimal-feedback-nulls", feedback);
		}

		[Test]
		public void NullTargetEntryId_ThrowsNullReferenceException()
		{
			// RULE-spam-10 - a REAL legacy bug, found by this characterization
			// test, not invented: AkismetSpamBlockingService.cs:63 reads
			//   if (feedback.TargetEntryId != null & feedback.TargetEntryId.Trim().Length > 0)
			// using bitwise '&', not short-circuiting '&&'. Both sides
			// evaluate even when TargetEntryId IS null, so
			// `.Trim()` runs on a null reference. This slice characterizes
			// the bug AS-IS (Strangler Fig preserves observable behaviour,
			// bugs included, until a separate, explicit decision changes
			// it) - the .NET 10 port reproduces the same throw, not a
			// silent fix. See modernization/spam/RULES.md RULE-spam-10 and
			// asop/runs/spam/v2/ADJUDICATION.md.
			FakeFeedback feedback = new FakeFeedback();
			feedback.Author = "Someone";
			feedback.FeedbackType = "comment";
			feedback.TargetEntryId = null;

			// Plain try/catch, not Assert.Throws<T> - the vendored
			// lib/nunit.framework.dll (NUnit 2.x, pre-generic-assertions)
			// doesn't have it (CS0117, caught by legacy.yml's own build
			// gate on the first attempt at this test).
			try
			{
				ConvertToAkismetComment(feedback);
				Assert.Fail("Expected a TargetInvocationException wrapping a NullReferenceException.");
			}
			catch (TargetInvocationException ex)
			{
				Assert.IsTrue(ex.InnerException is NullReferenceException,
					"Expected NullReferenceException, got " + ex.InnerException);
			}
		}

		private void AssertMatchesGolden(string fixtureName, IFeedback feedback)
		{
			IComment actual = ConvertToAkismetComment(feedback);
			string actualText = Serialize(actual);

			string goldenPath = GoldenFilePath(fixtureName + ".txt");
			if (Environment.GetEnvironmentVariable("DASBLOG_CAPTURE_GOLDEN") == "1")
			{
				Directory.CreateDirectory(Path.GetDirectoryName(goldenPath));
				File.WriteAllText(goldenPath, actualText, new System.Text.UTF8Encoding(false));
				Assert.Ignore("Captured golden file: " + goldenPath);
				return;
			}

			if (!File.Exists(goldenPath))
			{
				Assert.Fail(
					"No golden file at " + goldenPath + ". Run with " +
					"DASBLOG_CAPTURE_GOLDEN=1 to capture the legacy build's " +
					"actual output first, review it, then commit it.");
			}

			string expectedText = File.ReadAllText(goldenPath);
			Assert.AreEqual(expectedText, actualText,
				"Legacy output for fixture '" + fixtureName + "' diverged from its golden file.");
		}

		private static IComment ConvertToAkismetComment(IFeedback feedback)
		{
			AkismetSpamBlockingService service = new AkismetSpamBlockingService("dummy-api-key", "http://example.test/");
			MethodInfo method = typeof(AkismetSpamBlockingService).GetMethod(
				"ConvertToAkismetComment", BindingFlags.NonPublic | BindingFlags.Instance);
			return (IComment)method.Invoke(service, new object[] { feedback });
		}

		private static string Serialize(IComment comment)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("Author: ").Append(Or(comment.Author)).Append("\n");
			sb.Append("AuthorEmail: ").Append(Or(comment.AuthorEmail)).Append("\n");
			sb.Append("AuthorUrl: ").Append(comment.AuthorUrl == null ? "(null)" : comment.AuthorUrl.ToString()).Append("\n");
			sb.Append("Content: ").Append(Or(comment.Content)).Append("\n");
			sb.Append("Referer: ").Append(Or(comment.Referer)).Append("\n");
			sb.Append("Permalink: ").Append(comment.Permalink == null ? "(null)" : comment.Permalink.ToString()).Append("\n");
			sb.Append("UserAgent: ").Append(Or(comment.UserAgent)).Append("\n");
			sb.Append("CommentType: ").Append(Or(comment.CommentType)).Append("\n");
			sb.Append("IpAddress: ").Append(comment.IpAddress.ToString()).Append("\n");
			return sb.ToString();
		}

		private static string Or(string value)
		{
			return value == null ? "(null)" : value;
		}

		private static string GoldenFilePath(string fileName)
		{
			string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			DirectoryInfo dir = new DirectoryInfo(binDir);
			// bin/Debug -> Test -> newtelligence.DasBlog.Web.Core -> source -> repo root
			for (int i = 0; i < 5; i++)
			{
				dir = dir.Parent;
			}
			return Path.Combine(dir.FullName, "modernization", "spam", "golden", fileName);
		}
	}
}
