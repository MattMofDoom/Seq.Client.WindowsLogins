using System.Configuration;
using System.IO;
using System.Reflection;

namespace Seq.Client.WindowsLogins
{
    public static class Config
    {
        static Config()
        {
            AppName = ConfigurationManager.AppSettings["AppName"];
            SeqServer = ConfigurationManager.AppSettings["LogSeqServer"];
            SeqApiKey = ConfigurationManager.AppSettings["LogSeqApiKey"];
            LogFolder = ConfigurationManager.AppSettings["LogFolder"];

            var isSuccess = true;
            try
            {
                if (string.IsNullOrEmpty(AppName))
                    AppName = Assembly.GetEntryAssembly()?.GetName().Name;

                AppVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
                if (string.IsNullOrEmpty(LogFolder))
                    LogFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            }
            catch
            {
                isSuccess = false;
            }

            if (isSuccess) return;
            try
            {
                if (string.IsNullOrEmpty(AppName))
                    AppName = Assembly.GetExecutingAssembly().GetName().Name;

                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                if (string.IsNullOrEmpty(LogFolder))
                    LogFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            }
            catch
            {
                //We surrender ...
                AppVersion = string.Empty;
            }
        }

        public static string AppName { get; }
        public static string AppVersion { get; }
        public static string SeqServer { get; }
        public static string SeqApiKey { get; }
        public static string LogFolder { get; }
    }
}