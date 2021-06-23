using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Seq.Client.WindowsLogins.Tests
{
    public class EventTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public EventTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        /// <summary>
        ///     Ensure valid properties will be passed
        /// </summary>
        [Fact]
        public void EvaluatesValidEvent()
        {
            IList<object> test = new List<object>
            {
                "00000000-0000-0000-0000-000000000001", "Barry", "BARRY", "Barry",
                "00000000-0000-0000-0000-000000000001", "Barry", "BARRY", "Barry", (uint) 2, "Barry", "BarryAuth",
                "BARRYPC",
                Guid.Parse("00000000-0000-0000-0000-000000000001"), "Barries", "Barry", 1024, 1, " BARRY.EXE",
                "127.0.0.1", 1111,
                "All The Impersonation"
            };

            Assert.False(EventLogListener.IsNotValid(test));
        }

        /// <summary>
        ///     Ensure invalid properties won't be passed
        /// </summary>
        [Fact]
        public void EvaluatesInvalidEvent()
        {
            IList<object> test = new List<object>
            {
                "00000000-0000-0000-0000-000000000001", "Barry", "BARRY", "Barry",
                "00000000-0000-0000-0000-000000000001", "Barry", "BARRY", "Barry", (uint) 2, "Barry", "BarryAuth",
                "BARRYPC",
                Guid.Parse("00000000-0000-0000-0000-000000000000"), "Barries", "Barry", 1024, 1, " BARRY.EXE", "-",
                1111,
                "All The Impersonation"
            };

            Assert.True(EventLogListener.IsNotValid(test));
        }

        /// <summary>
        ///     Allow a single event to expire after 2 seconds
        /// </summary>
        [Fact]
        public void EventBagExpiresEvent()
        {
            var unused = new EventLogListener(2);
            EventLogListener.EventList.Add(1000);
            Assert.True(EventLogListener.EventList.Contains(1000));
            Thread.Sleep(3000);
            Assert.False(EventLogListener.EventList.Contains(1000));
        }

        /// <summary>
        ///     A longer test that ensures an event is kept while it is accessed
        /// </summary>
        [Fact]
        public void EventBagKeepsAccessedEvent()
        {
            var unused = new EventLogListener(2);
            var watch = new Stopwatch();
            watch.Start();

            EventLogListener.EventList.Add(1000);

            for (var i = 1; i < 2001; i++)
            {
                var count = EventLogListener.EventList.Count();
                if (i % 100 == 0)
                    _testOutputHelper.WriteLine(
                        $"Loop {i} @ {watch.ElapsedMilliseconds / 1000:N0} seconds, Bag Count: {count}");

                Assert.True(EventLogListener.EventList.Contains(1000));
                Thread.Sleep(10);
            }

            Thread.Sleep(3000);
            Assert.False(EventLogListener.EventList.Contains(1000));
        }

        /// <summary>
        ///     A long test (60 seconds) that allows us to observe cache population and expiry
        /// </summary>
        [Fact]
        public void EventBagPopulationAndExpiry()
        {
            var unused = new EventLogListener(2);
            var watch = new Stopwatch();
            watch.Start();
            var random = new Random();
            new Thread(delegate()
            {
                //Populate the cache for ~30 seconds so it will expire before the test has finished
                for (var i = 1; i < 1001; i++)
                {
                    EventLogListener.EventList.Add(random.Next(1000, 100000));
                    var tCount = EventLogListener.EventList.Count();
                    if (i % 20 == 0)
                        _testOutputHelper.WriteLine(
                            $"Thread loop {i} @ {watch.ElapsedMilliseconds} milliseconds, Bag Count: {tCount}");
                    Thread.Sleep(25);
                }
            }).Start();

            var hasExpired = false;
            for (var x = 1; x < 4001; x++)
            {
                Thread.Sleep(10);
                var count = EventLogListener.EventList.Count();
                if (x % 100 == 0)
                    _testOutputHelper.WriteLine(
                        $"Loop {x} @ {watch.ElapsedMilliseconds / 1000:N0} seconds, Bag Count: {count}");
                if (count == 0 && !hasExpired)
                {
                    _testOutputHelper.WriteLine($"Cache has emptied @ {watch.ElapsedMilliseconds / 1000:N0} seconds!");
                    hasExpired = true;
                }

                Assert.False(EventLogListener.EventList.Contains(999));
            }

            watch.Stop();
            Assert.True(hasExpired);
        }
    }
}