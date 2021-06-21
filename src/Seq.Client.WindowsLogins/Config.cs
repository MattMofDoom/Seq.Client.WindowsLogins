﻿using System;
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
            HeartbeatInterval = GetInt(ConfigurationManager.AppSettings["HeartbeatInterval"]);

            //Must be between 0 and 1 hour in seconds
            if (HeartbeatInterval < 0 || HeartbeatInterval > 3600)
                HeartbeatInterval = 600000;
            else
                HeartbeatInterval *= 1000;

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
        public static int HeartbeatInterval { get; }

        /// <summary>
        ///     Convert the supplied <see cref="object" /> to an <see cref="int" />
        ///     <para />
        ///     This will filter out nulls that could otherwise cause exceptions
        /// </summary>
        /// <param name="sourceObject">An object that can be converted to an int</param>
        /// <returns></returns>
        private static int GetInt(object sourceObject)
        {
            var sourceString = string.Empty;

            if (!Convert.IsDBNull(sourceObject)) sourceString = (string) sourceObject;

            if (int.TryParse(sourceString, out var destInt)) return destInt;

            return -1;
        }
    }
}