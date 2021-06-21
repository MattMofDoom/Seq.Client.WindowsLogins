using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Seq.Client.WindowsLogins.Tests
{
    public class EventTests
    {
        [Fact]
        public void EvaluatesValidEvent()
        {
            //Passes the valid event filter
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

        [Fact]
        public void EvaluatesInvalidEvent()
        {
            //Pass properties that will trip a false
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

        [Fact]
        public void EventBagContainsEvent()
        {
            var unused = new EventLogListener(1000, new TimeSpan(0, 0, 2));
            EventLogListener.EventList.Add(1000);
            Assert.True(EventLogListener.EventBagHasEvent(1000));
        }

        [Fact]
        public void EventBagDoesNotContainEvent()
        {
            var unused = new EventLogListener(1000, new TimeSpan(0, 0, 2));
            EventLogListener.EventList.Add(1000);
            Assert.False(EventLogListener.EventBagHasEvent(1001));
        }

        [Fact]
        public async void EventBagExpiresEvent()
        {
            var unused = new EventLogListener(1000, new TimeSpan(0, 0, 2));
            EventLogListener.EventList.Add(1000);
            Assert.True(EventLogListener.EventBagHasEvent(1000));
            await Task.Delay(3000);
            Assert.False(EventLogListener.EventBagHasEvent(1000));
        }
    }
}