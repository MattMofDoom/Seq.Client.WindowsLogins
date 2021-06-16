namespace Seq.Client.WindowsLogins
{
    internal class EventLogClient
    {
        private EventLogListener _eventLogListener;

        public void Start(bool isInteractive = false)
        {
            _eventLogListener = new EventLogListener();
            _eventLogListener.Start(isInteractive);
        }

        public void Stop()
        {
            _eventLogListener.Stop();
        }
    }
}