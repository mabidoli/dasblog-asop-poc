using System;
using newtelligence.DasBlog.Runtime;

namespace newtelligence.DasBlog.Web.Services.Test.FeedCharacterization
{
	/// <summary>
	/// A minimal IBlogDataService test double for the feed characterization
	/// tests (slice 1 - see modernization/feed/SLICE-MAP.md). GetRssCore's
	/// in-scope path (constructed directly, not via the ASMX subclass, so
	/// inASMX stays false - see SLICE-MAP.md's "Entry point chain") only
	/// ever calls GetEntriesForDay and GetLastEntryUpdate. Everything else
	/// on IBlogDataService throws, on purpose: a test that silently returns
	/// a default value from an unexpected call is worse than one that fails
	/// loudly and says which call it didn't expect.
	/// </summary>
	public class FakeBlogDataService : IBlogDataService
	{
		private readonly EntryCollection entries;

		public FakeBlogDataService(EntryCollection entries)
		{
			this.entries = entries;
		}

		public EntryCollection GetEntriesForDay(DateTime start, TimeZone tz, string acceptLanguages, int maxDays, int maxEntries, string categoryName)
		{
			// RULE-feed-D15: real day/entry-count limiting and category
			// filtering is BuildEntries' job, out of scope for this slice -
			// this fake always returns the full fixture set, already built
			// by the test. BuildEntries itself still applies RULE-feed-02's
			// sort to whatever this returns.
			return entries;
		}

		public DateTime GetLastEntryUpdate()
		{
			return DateTime.MinValue;
		}

		private static Exception NotNeeded(string member)
		{
			return new NotImplementedException(
				"FakeBlogDataService." + member + " was called, but the feed " +
				"characterization tests (modernization/feed/) don't expect the " +
				"in-scope RSS path to reach it. If a real call path needs this, " +
				"RULES.md is missing a rule.");
		}

		public Entry GetEntry(string entryId) { throw NotNeeded("GetEntry"); }
		public Entry GetEntryForEdit(string entryId) { throw NotNeeded("GetEntryForEdit"); }
		public EntryCollection GetEntries(bool fullContent) { throw NotNeeded("GetEntries"); }
		public EntryCollection GetEntries(Predicate<DayEntry> dayEntryCriteria, Predicate<Entry> entryCriteria, int maxDays, int maxEntries) { throw NotNeeded("GetEntries"); }
		public EntryCollection GetEntriesForMonth(DateTime summary, TimeZone tz, string acceptLanguages) { throw NotNeeded("GetEntriesForMonth"); }
		public EntryCollection GetEntriesForCategory(string categoryName, string acceptLanguages) { throw NotNeeded("GetEntriesForCategory"); }
		public EntryCollection GetEntriesForUser(string user) { throw NotNeeded("GetEntriesForUser"); }
		public DateTime[] GetDaysWithEntries(TimeZone tz) { throw NotNeeded("GetDaysWithEntries"); }
		public DayEntry GetDayEntry(DateTime date) { throw NotNeeded("GetDayEntry"); }
		public DayExtra GetDayExtra(DateTime date) { throw NotNeeded("GetDayExtra"); }
		public void DeleteEntry(string entryId, CrosspostSiteCollection crosspostSites) { throw NotNeeded("DeleteEntry"); }
		public EntrySaveState SaveEntry(Entry entryId, params object[] trackingInfos) { throw NotNeeded("SaveEntry"); }
		public CategoryCacheEntryCollection GetCategories() { throw NotNeeded("GetCategories"); }
		public void RunActions(object[] actions) { throw NotNeeded("RunActions"); }
		public void AddTracking(Tracking tracking, params object[] actions) { throw NotNeeded("AddTracking"); }
		public void DeleteTracking(string entryId, string trackingPermalink, TrackingType trackingType) { throw NotNeeded("DeleteTracking"); }
		public TrackingCollection GetTrackingsFor(string entryId) { throw NotNeeded("GetTrackingsFor"); }
		public void AddComment(Comment comment, params object[] actions) { throw NotNeeded("AddComment"); }
		public Comment GetCommentById(string entryId, string commentId) { throw NotNeeded("GetCommentById"); }
		public void ApproveComment(string entryId, string commentId) { throw NotNeeded("ApproveComment"); }
		public void DeleteComment(string entryId, string commentId) { throw NotNeeded("DeleteComment"); }
		public CommentCollection GetCommentsFor(string entryId) { throw NotNeeded("GetCommentsFor"); }
		public CommentCollection GetPublicCommentsFor(string entryId) { throw NotNeeded("GetPublicCommentsFor"); }
		public CommentCollection GetCommentsFor(string entryId, bool allComments) { throw NotNeeded("GetCommentsFor"); }
		public CommentCollection GetAllComments() { throw NotNeeded("GetAllComments"); }
		public DateTime GetLastCommentUpdate() { throw NotNeeded("GetLastCommentUpdate"); }
	}
}
