using Serilog;
using Serilog.Events;
using System.Configuration;

namespace TSMCMapGenerator.Services
{
    public static class Logger
    {
        public static void Init()
        {
            string logFilePath = ConfigurationManager.AppSettings["LogFilePath"];
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug)
                .CreateLogger();
        }

        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }
    }
}
