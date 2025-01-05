using NLog.Config;
using NLog.Targets;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace AnlaxBase
{
    public static class AnlaxBaseLogManager
    {
        private static readonly Logger logger;
        private static readonly LogFactory logFactory;

        public static string PathToTxt
        {
            get
            {
                string location = Assembly.GetExecutingAssembly().Location;
                string directoryName = Path.GetDirectoryName(location);
                return Path.Combine(directoryName, "AnlaxBaseLog.txt");
            }
        }

        static AnlaxBaseLogManager()
        {
            logFactory = new LogFactory();
            ConfigureLogging();
            logger = logFactory.GetCurrentClassLogger();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                logger.Fatal(exception, "Domain unhandled exception");
                logFactory.Shutdown();
            };
        }

        private static void ConfigureLogging()
        {
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget("logfile")
            {
                FileName = PathToTxt,
                Layout = "${longdate} [${level:uppercase=true:padding=3}] ${message} ${exception}",
                ConcurrentWrites = true
            };

            config.AddTarget(fileTarget);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);

            logFactory.Configuration = config;
        }

        public static void LogInfo(string message)
        {
            logger.Info(message);
        }

        public static void LogFatal(Exception exception, string message)
        {
            logger.Fatal(exception, message);
        }

        public static void LogWarning(string message)
        {
            logger.Warn(message);
        }

        public static void LogError(string message)
        {
            logger.Error(message);
        }

        public static void ClearLogFile()
        {
            string pathToTxt = PathToTxt;
            try
            {
                using FileStream fileStream = new FileStream(pathToTxt, FileMode.Open, FileAccess.Write, FileShare.None);
                fileStream.SetLength(0L);
                logger.Info("Log file cleared.");
            }
            catch (IOException exception)
            {
                logger.Warn(exception, "Cannot clear log file as it is being used by another process.");
            }
            catch (Exception exception2)
            {
                logger.Error(exception2, "An error occurred while clearing the log file.");
            }
        }

        public static void ReleaseLogFile()
        {
            try
            {
                logger.Info("Попытка освободить логгер.");
                logFactory.Shutdown();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Произошла ошибка при освобождении файла логгера. " + ex.Message);
            }
        }
    }
}
