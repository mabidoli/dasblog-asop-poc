using System;
using System.Collections;
using System.Web.Caching;
using newtelligence.DasBlog.Web.Core;

namespace newtelligence.DasBlog.Web.Services.Test.FeedCharacterization
{
	/// <summary>
	/// An in-memory DataCache for the feed characterization tests.
	/// SyndicationServiceBase's DataCache is normally HttpCache, which
	/// wraps HostingEnvironment.Cache and throws outside IIS - this fake
	/// stands in for it so GetRssCore's cache[CacheKey]/cache.Insert(...)
	/// calls (SyndicationServiceImplementation.cs:239-241,498) have
	/// somewhere real to read and write instead of null-refing.
	/// </summary>
	public class FakeDataCache : DataCache
	{
		private readonly Hashtable store = new Hashtable();

		public override void Clear()
		{
			store.Clear();
		}

		public override void Insert(string key, object value, CacheItemPriority priority)
		{
			store[key] = value;
		}

		public override void Insert(string key, object value, CacheDependency dependency)
		{
			store[key] = value;
		}

		public override void Insert(string key, object value, DateTime absoluteExpiration)
		{
			store[key] = value;
		}

		public override void Insert(string key, object value, TimeSpan slidingExpiration)
		{
			store[key] = value;
		}

		public override object Remove(string key)
		{
			object value = store[key];
			store.Remove(key);
			return value;
		}

		public override object this[string key]
		{
			get { return store[key]; }
		}
	}
}
