using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using Lurgle.Logging;
using Timer = System.Timers.Timer;

namespace Seq.Client.WindowsLogins
{
    public class EventLogListener
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static TimedEventBag _eventList = new TimedEventBag(60000, new TimeSpan(0, 10, 0));
        private static Timer _eventListTimer;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private EventLog _eventLog;
        private volatile bool _started;

        public void Start(bool isInteractive)
        {
            try
            {
                Log.Level(LurgLevel.Debug).Add("Starting listener");

                _eventLog = OpenEventLog();
                _eventLog.EntryWritten += OnEntryWritten;
                _eventLog.EnableRaisingEvents = true;
                _started = true;

                if (!isInteractive) return;
                _eventListTimer = new Timer {Interval = 60000};
                _eventListTimer.Elapsed += EventListStatus;
                _eventListTimer.Start();
            }
            catch (Exception ex)
            {
                Log.Exception(ex).Add("Failed to start listener: {Message:l}", ex.Message);
            }
        }

        private static void EventListStatus(object sender, EventArgs e)
        {
            if (_eventList.Count > 0)
                Log.Level(LurgLevel.Debug).Add("Cache of timed event ids is at {ItemCount} items", _eventList.Count);
        }

        private static EventLog OpenEventLog()
        {
            return new EventLog("Security");
        }

        public void Stop()
        {
            try
            {
                if (!_started)
                    return;

                _cancel.Cancel();
                _eventLog.EnableRaisingEvents = false;

                _eventLog.Close();
                _eventLog.Dispose();

                Log.Level(LurgLevel.Debug).Add("Listener stopped");
            }
            catch (Exception ex)
            {
                Log.Exception(ex).Add("Failed to stop listener: {Message:l}", ex.Message);
            }
        }

        private void OnEntryWritten(object sender, EntryWrittenEventArgs args)
        {
            try
            {
                //Ensure that events are new and have not been seen already. This addresses a scenario where large event logs can repeatedly pass events to the handler.
                if ((DateTime.Now - args.Entry.TimeGenerated).TotalSeconds < 60 && !_eventList.Contains(args.Entry.Index))
                    HandleEventLogEntry(args.Entry, _eventLog.Log);
            }
            catch (Exception ex)
            {
                Log.Exception(ex).Add("Failed to handle an event log entry: {Message:l}", ex.Message);
            }
        }

        private static void HandleEventLogEntry(EventLogEntry entry, string logName)
        {
            if (entry.EntryType != EventLogEntryType.SuccessAudit)
                return;

            if ((ushort) entry.InstanceId != 4624)
                return;

            _eventList.Add(entry.Index);

            try
            {
                var query = new EventLogQuery(logName, PathType.LogName,
                    "*[System[(EventRecordID=" + entry.Index + ")]]");
                var reader = new EventLogReader(query);
                for (var logEntry = reader.ReadEvent(); logEntry != null; logEntry = reader.ReadEvent())
                {
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

                    var eventProperties = ((EventLogRecord) logEntry).GetPropertyValues(loginEventPropertySelector);

                    //Only interactive users are of interest
                    if (((uint) eventProperties[8] == 2 || (uint) eventProperties[8] == 10) &&
                        (string) eventProperties[18] != "-" &&
                        eventProperties[12].ToString() != "00000000-0000-0000-0000-000000000000")
                        Log.Level(Extensions.MapLogLevel(entry.EntryType))
#pragma warning disable 618
                            .AddProperty("EventId", entry.EventID)
#pragma warning restore 618
                            .AddProperty("InstanceId", entry.InstanceId)
                            .AddProperty("EventTime", entry.TimeGenerated)
                            .AddProperty("Source", entry.Source)
                            .AddProperty("Category", entry.CategoryNumber)
                            .AddProperty("EventLogName", logName)
                            .AddProperty("EventRecordID", entry.Index)
                            .AddProperty("Details", entry.Message)
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
            }
            catch (Exception ex)
            {
                Log.Exception(ex).Add("Error parsing event: {Message:l}", ex.Message);
            }
        }
    }
}