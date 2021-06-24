using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Lurgle.Logging;
using Timer = System.Timers.Timer;

namespace Seq.Client.WindowsLogins
{
    public class EventLogListener
    {
        private static bool _isInteractive;
        private static Timer _heartbeatTimer;
        private static readonly DateTime ServiceStart = DateTime.Now;
        private static long _logonsDetected;
        private static long _nonInteractiveLogons;
        private static long _unhandledEvents;
        private static long _oldEvents;
        private static long _emptyEvents;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private EventLogQuery _eventLog;
        private volatile bool _started;
        private EventLogWatcher _watcher;

        public EventLogListener(int? expiry = null)
        {
            int eventExpiryTime;

            if (expiry != null)
                eventExpiryTime = (int) expiry;
            else
                eventExpiryTime = 600;

            EventList = new TimedEventBag(eventExpiryTime);
        }

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        public static TimedEventBag EventList { get; private set; }

        public void Start(bool isInteractive)
        {
            try
            {
                _isInteractive = isInteractive;
                Log.Level(LurgLevel.Debug).Add("Starting listener");

                //Query for success audits with event id 4624
                _eventLog = new EventLogQuery("Security", PathType.LogName,
                    "*[System[band(Keywords,9007199254740992) and (EventID=4624)]]");
                _watcher = new EventLogWatcher(_eventLog);
                _watcher.EventRecordWritten += OnEntryWritten;
                _watcher.Enabled = true;

                _started = true;

                //Heartbeat timer that can be used to detect if the service is not running
                if (Config.HeartbeatInterval <= 0) return;
                //First heartbeat will be at a random interval between 2 and 10 seconds
                _heartbeatTimer = isInteractive
                    ? new Timer {Interval = 10000}
                    : new Timer {Interval = new Random().Next(2000, 10000)};
                _heartbeatTimer.Elapsed += ServiceHeartbeat;
                _heartbeatTimer.AutoReset = false;
                _heartbeatTimer.Start();
            }
            catch (Exception ex)
            {
                Log.Exception(ex).Add("Failed to start listener: {Message:l}", ex.Message);
            }
        }

        private static void ServiceHeartbeat(object sender, ElapsedEventArgs e)
        {
            Log.Level(LurgLevel.Debug)
                .AddProperty("ItemCount", EventList.Count)
                .AddProperty("LogonsDetected", _logonsDetected)
                .AddProperty("NonInteractiveLogons", _nonInteractiveLogons)
                .AddProperty("OldEvents", _oldEvents)
                .AddProperty("EmptyEvents", _emptyEvents)
                .AddProperty("UnhandledEvents", _unhandledEvents)
                .AddProperty("NextTime", DateTime.Now.AddMilliseconds(Config.HeartbeatInterval))
                .Add(
                    Config.IsDebug
                        ? "{AppName:l} Heartbeat [{MachineName:l}] - Event cache: {ItemCount}, Logons detected: {LogonsDetected}, " +
                          "Non-interactive logons: {NonInteractiveLogons}, Unhandled events: {UnhandledEvents}, Old events seen: {OldEvents}, " +
                          "Empty events: {EmptyEvents}, Next Heartbeat: {NextTime:H:mm:ss tt}"
                        : "{AppName:l} Heartbeat [{MachineName:l}] - Event cache: {ItemCount}, Next Heartbeat: {NextTime:H:mm:ss tt}");

            if (_heartbeatTimer.AutoReset) return;
            //Set the timer to 10 minutes after initial heartbeat
            _heartbeatTimer.AutoReset = true;
            _heartbeatTimer.Interval = _isInteractive ? 10000 : Config.HeartbeatInterval;
            _heartbeatTimer.Start();
        }

        public void Stop()
        {
            try
            {
                if (!_started)
                    return;

                _cancel.Cancel();
                _watcher.Enabled = false;
                _watcher.Dispose();

                Log.Level(LurgLevel.Debug).Add("Listener stopped");
            }
            catch (Exception ex)
            {
                Log.Exception(ex).Add("Failed to stop listener: {Message:l}", ex.Message);
            }
        }

        private static async void OnEntryWritten(object sender, EventRecordWrittenEventArgs args)
        {
            try
            {
                //Ensure that events are new and have not been seen already. This addresses a scenario where event logs can repeatedly pass events to the handler.
                if (args.EventRecord != null && args.EventRecord.TimeCreated >= ServiceStart &&
                    !EventList.Contains(args.EventRecord.RecordId))
                    await Task.Run(() => HandleEventLogEntry(args.EventRecord));
                else if (args.EventRecord != null && args.EventRecord.TimeCreated < ServiceStart)
                    _oldEvents++;
                else if (args.EventRecord == null)
                    _emptyEvents++;
            }
            catch (Exception ex)
            {
                Log.Exception(ex).Add("Failed to handle an event log entry: {Message:l}", ex.Message);
            }
        }

        private static void HandleEventLogEntry(EventRecord entry)
        {
            //Ensure that we track events we've already seen
            EventList.Add(entry.RecordId);

            try
            {
                //Get all the properties of interest for passing to Seq
                var loginEventPropertySelector = new EventLogPropertySelector(new[]
                {
                    "Event/EventData/Data[@Name='SubjectUserSid']",
                    "Event/EventData/Data[@Name='SubjectUserName']",
                    "Event/EventData/Data[@Name='SubjectDomainName']",
                    "Event/EventData/Data[@Name='SubjectLogonId']",
                    "Event/EventData/Data[@Name='TargetUserSid']",
                    "Event/EventData/Data[@Name='TargetUserName']",
                    "Event/EventData/Data[@Name='TargetDomainName']",
                    "Event/EventData/Data[@Name='TargetLogonId']",
                    "Event/EventData/Data[@Name='LogonType']",
                    "Event/EventData/Data[@Name='LogonProcessName']",
                    "Event/EventData/Data[@Name='AuthenticationPackageName']",
                    "Event/EventData/Data[@Name='WorkstationName']",
                    "Event/EventData/Data[@Name='LogonGuid']",
                    "Event/EventData/Data[@Name='TransmittedServices']",
                    "Event/EventData/Data[@Name='LmPackageName']",
                    "Event/EventData/Data[@Name='KeyLength']",
                    "Event/EventData/Data[@Name='ProcessId']",
                    "Event/EventData/Data[@Name='ProcessName']",
                    "Event/EventData/Data[@Name='IpAddress']",
                    "Event/EventData/Data[@Name='IpPort']",
                    "Event/EventData/Data[@Name='ImpersonationLevel']"
                });

                var eventProperties = ((EventLogRecord) entry).GetPropertyValues(loginEventPropertySelector);

                if (eventProperties.Count != 21)
                {
                    _unhandledEvents++;
                    return;
                }

                if (IsNotValid(eventProperties))
                {
                    _nonInteractiveLogons++;
                    return;
                }

                _logonsDetected++;

                Log.Level(Extensions.MapLogLevel(EventLogEntryType.SuccessAudit))
#pragma warning disable 618
                    .AddProperty("EventId", (long) entry.Id)
#pragma warning restore 618
                    .AddProperty("InstanceId", entry.Id)
                    .AddProperty("EventTime", entry.TimeCreated)
                    .AddProperty("Source", entry.ProviderName)
                    .AddProperty("Category", entry.LevelDisplayName)
                    .AddProperty("EventLogName", entry.LogName)
                    .AddProperty("EventRecordID", entry.RecordId)
                    .AddProperty("Details", entry.FormatDescription())
                    .AddProperty("SubjectUserSid", eventProperties[0])
                    .AddProperty("SubjectUserName", eventProperties[1])
                    .AddProperty("SubjectDomainName", eventProperties[2])
                    .AddProperty("SubjectLogonId", eventProperties[3])
                    .AddProperty("TargetUserSid", eventProperties[4])
                    .AddProperty("TargetUserName", eventProperties[5])
                    .AddProperty("TargetDomainName", eventProperties[6])
                    .AddProperty("TargetLogonId", eventProperties[7])
                    .AddProperty("LogonType", eventProperties[8])
                    .AddProperty("LogonProcessName", eventProperties[9])
                    .AddProperty("AuthenticationPackageName", eventProperties[10])
                    .AddProperty("WorkstationName", eventProperties[11])
                    .AddProperty("LogonGuid", eventProperties[12])
                    .AddProperty("TransmittedServices", eventProperties[13])
                    .AddProperty("LmPackageName", eventProperties[14])
                    .AddProperty("KeyLength", eventProperties[15])
                    .AddProperty("ProcessId", eventProperties[16])
                    .AddProperty("ProcessName", eventProperties[17])
                    .AddProperty("IpAddress", eventProperties[18])
                    .AddProperty("IpPort", eventProperties[19])
                    .AddProperty("ImpersonationLevel", eventProperties[20])
                    .Add(
                        "[{AppName:l}] New login detected on {MachineName:l} - {TargetDomainName:l}\\{TargetUserName:l} at {EventTime:F}");
            }
            catch (Exception ex)
            {
                Log.Exception(ex).Add("Error parsing event: {Message:l}", ex.Message);
            }
        }

        public static bool IsNotValid(IList<object> eventProperties)
        {
            //Only interactive users are of interest - logonType 2 and 10. Some non-interactive services can launch processes with logontype 2 but can be filtered.
            return (uint) eventProperties[8] != 2 && (uint) eventProperties[8] != 10 ||
                   (string) eventProperties[18] == "-" ||
                   eventProperties[12].ToString() == "00000000-0000-0000-0000-000000000000";
        }
    }
}