using System;
using System.Runtime.Caching;

// ReSharper disable UnusedMember.Global

namespace Seq.Client.WindowsLogins
{
    public class TimedEventBag
    {
        private readonly ObjectCache _cache;
        private readonly int _expiration;

        /// <summary>
        ///     Cache objects that have already been seen, and expire them after X seconds
        /// </summary>
        /// <param name="expiration"></param>
        public TimedEventBag(int expiration)
        {
            _expiration = expiration >= 0 ? expiration : 600;
            _cache = MemoryCache.Default;
        }

        public void Add(int item)
        {
            _cache.Add(new CacheItem(item.ToString(), item),
                new CacheItemPolicy {AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(_expiration)});
        }

        public bool Contains(int item)
        {
            return _cache.Contains(item.ToString());
        }

        public long Count()
        {
            return _cache.GetCount();
        }
    }
}