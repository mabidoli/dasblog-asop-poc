using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NUnit.Framework;
using newtelligence.DasBlog.Runtime;
using newtelligence.DasBlog.Web;
using newtelligence.DasBlog.Web.Core;
using newtelligence.DasBlog.Web.Services.Rss20;

namespace newtelligence.DasBlog.Web.Services.Test.FeedCharacterization
{
	/// <summary>
	/// Characterization tests for slice 1 (RSS 2.0 feed generation), per
	/// asop/strangler-slice-a-feature/v1.yaml step 3. Each test method's
	/// comment names the RULE-feed-NN business rules (modernization/feed/RULES.md)
	/// it exercises; scripts/check-rules-have-tests.py greps this file (and
	/// its siblings under tests-legacy naming, if any are added later) for
	/// those IDs as the step-2 gate.
	///
	/// Constructs SyndicationServiceImplementation directly (not via the
	/// ASMX subclass), so `inASMX` stays false and every HttpContext-coupled
	/// branch in GetRssCore (FeedBurner redirect, conditional-GET, referral
	/// logging - see SLICE-MAP.md) is skipped by the legacy code's own
	/// existing guard clauses, with no reimplementation needed to exclude
	/// them.
	///
	/// Run with the environment variable DASBLOG_CAPTURE_GOLDEN=1 to
	/// (re)write a fixture's golden file from this build's actual output
	/// instead of comparing against it. See asop/runs/feed/v1/EVIDENCE.md
	/// for when/why that was done for each golden file checked in here.
	/// </summary>
	[TestFixture]
	public class RssFeedCharacterizationTests
	{
		[Test]
		public void TwoPublicEntries_ProduceOrderedPlainFeed()
		{
			// RULE-feed-01 RULE-feed-02 RULE-feed-03 RULE-feed-04
			// RULE-feed-05 RULE-feed-06 RULE-feed-07 RULE-feed-08
			// RULE-feed-09 RULE-feed-10 RULE-feed-11 RULE-feed-12
			// RULE-feed-13 RULE-feed-14 RULE-feed-15
			SiteConfig config = PlainSiteConfig();

			EntryCollection entries = new EntryCollection();
			entries.Add(MakeEntry(
				"2003-07-31T120000Z", "First post", "unknown-author",
				"General|Misc;Announcements", "en-US",
				new DateTime(2003, 7, 31, 12, 0, 0, DateTimeKind.Utc),
				"A short description.",
				"<p>The full content, unused because Description is set.</p>"));
			entries.Add(MakeEntry(
				"2003-08-01T090000Z", "Second post, newer", "",
				"", "en-US",
				new DateTime(2003, 8, 1, 9, 0, 0, DateTimeKind.Utc),
				"",
				"<p>Full content used because Description is blank.</p>"));

			// RULE-feed-01: excluded from the feed either way.
			Entry unpublished = MakeEntry("2003-08-02T000000Z", "Draft", "", "", "en-US",
				new DateTime(2003, 8, 2, 0, 0, 0, DateTimeKind.Utc), "x", "x");
			unpublished.IsPublic = false;
			entries.Add(unpublished);

			Entry unsyndicated = MakeEntry("2003-08-03T000000Z", "Not syndicated", "", "", "en-US",
				new DateTime(2003, 8, 3, 0, 0, 0, DateTimeKind.Utc), "x", "x");
			unsyndicated.Syndicated = false;
			entries.Add(unsyndicated);

			AssertMatchesGolden("two-public-entries", config, entries);
		}

		[Test]
		public void AggregatorBugAndFooter_AreAppended()
		{
			// RULE-feed-16
			SiteConfig config = PlainSiteConfig();
			config.EnableAggregatorBugging = true;
			config.EnableRssItemFooters = true;
			config.RssItemFooter = "Subscribe at example.test/rss.aspx";

			EntryCollection entries = new EntryCollection();
			entries.Add(MakeEntry(
				"2003-07-31T120000Z", "Post with footer and bug", "",
				"", "en-US",
				new DateTime(2003, 7, 31, 12, 0, 0, DateTimeKind.Utc),
				"",
				"<p>Content.</p>"));

			AssertMatchesGolden("aggregator-bug-and-footer", config, entries);
		}

		private static SiteConfig PlainSiteConfig()
		{
			SiteConfig config = new SiteConfig();
			config.Title = "Test Blog";
			config.Description = "A test blog for feed characterization";
			config.Subtitle = "Subtitle fallback";
			config.Copyright = "Copyright 2003, Test Author";
			config.Contact = "test@example.test";
			config.Root = "http://example.test/";
			config.RssLanguage = "en-US";
			config.RssDayCount = 365;
			config.RssMainEntryCount = 100;
			config.EnableUrlRewriting = false;        // RULE-feed-D7, deferred
			config.EnableTitlePermaLink = false;       // RULE-feed-D6, deferred
			config.ExtensionlessUrls = false;          // RULE-feed-D6, deferred
			config.EnableComments = false;             // RULE-feed-D3, deferred
			config.EnableGeoRss = false;               // RULE-feed-D2, deferred
			config.ApplyContentFiltersToRSS = false;   // RULE-feed-D16, deferred
			config.AlwaysIncludeContentInRSS = false;
			config.HtmlTidyContent = false;            // RULE-feed-D8, deferred
			config.EnableAggregatorBugging = false;
			config.EnableRssItemFooters = false;
			config.ChannelImageUrl = null;
			return config;
		}

		private static Entry MakeEntry(string id, string title, string author, string categories,
			string language, DateTime createdUtc, string description, string content)
		{
			Entry entry = new Entry();
			entry.EntryId = id;
			entry.Title = title;
			entry.Author = author;
			entry.Categories = categories;
			entry.Language = language;
			entry.CreatedUtc = createdUtc;
			entry.Description = description;
			entry.Content = content;
			return entry;
		}

		private void AssertMatchesGolden(string fixtureName, SiteConfig config, EntryCollection entries)
		{
			RssRoot actual = BuildRssRoot(config, entries);
			string actualXml = Serialize(actual);

			string goldenPath = GoldenFilePath(fixtureName + ".xml");
			if (Environment.GetEnvironmentVariable("DASBLOG_CAPTURE_GOLDEN") == "1")
			{
				Directory.CreateDirectory(Path.GetDirectoryName(goldenPath));
				File.WriteAllText(goldenPath, actualXml, new UTF8Encoding(false));
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

			string expectedXml = File.ReadAllText(goldenPath);
			Assert.AreEqual(expectedXml, actualXml,
				"Legacy output for fixture '" + fixtureName + "' diverged from its golden file.");
		}

		private static RssRoot BuildRssRoot(SiteConfig config, EntryCollection entries)
		{
			FakeBlogDataService dataService = new FakeBlogDataService(entries);
			SyndicationServiceImplementation svc =
				new SyndicationServiceImplementation(config, dataService, null);

			FieldInfo cacheField = typeof(SyndicationServiceBase).GetField(
				"cache", BindingFlags.NonPublic | BindingFlags.Instance);
			cacheField.SetValue(svc, new FakeDataCache());

			return svc.GetRssWithCounts(config.RssDayCount, config.RssMainEntryCount);
		}

		private static string Serialize(RssRoot root)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(RssRoot));
			StringBuilder sb = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "  ";
			settings.NewLineChars = "\n";
			settings.OmitXmlDeclaration = true;
			using (XmlWriter writer = XmlWriter.Create(sb, settings))
			{
				serializer.Serialize(writer, root);
			}
			return sb.ToString();
		}

		private static string GoldenFilePath(string fileName)
		{
			string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			DirectoryInfo dir = new DirectoryInfo(binDir);
			// bin/Debug -> Test -> newtelligence.DasBlog.Web.Services -> source -> repo root
			for (int i = 0; i < 5; i++)
			{
				dir = dir.Parent;
			}
			return Path.Combine(dir.FullName, "modernization", "feed", "golden", fileName);
		}
	}
}
