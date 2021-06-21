using System.Diagnostics;
using Lurgle.Logging;

namespace Seq.Client.WindowsLogins
{
    public static class Extensions
    {
        public static LurgLevel MapLogLevel(EventLogEntryType type)
        {
            switch (type)
            {
                case EventLogEntryType.Information:
                    return LurgLevel.Information;
                case EventLogEntryType.Warning:
                    return LurgLevel.Warning;
                case EventLogEntryType.Error:
                    return LurgLevel.Error;
                case EventLogEntryType.SuccessAudit:
                    return LurgLevel.Information;
                case EventLogEntryType.FailureAudit:
                    return LurgLevel.Warning;
                default:
                    return LurgLevel.Debug;
            }
        }
    }
}