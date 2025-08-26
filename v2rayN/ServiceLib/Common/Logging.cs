using NLog;
using NLog.Config;
using NLog.Targets;

namespace ServiceLib.Common;

public class Logging
{
    private static readonly Logger _logger1 = LogManager.GetLogger("Log1");
    private static readonly Logger _logger2 = LogManager.GetLogger("Log2");

    public static void Setup()
    {
        LoggingConfiguration config = new();
        FileTarget fileTarget = new();
        config.AddTarget("file", fileTarget);
        fileTarget.Layout = "${longdate}-${level:uppercase=true} ${message}";
        fileTarget.FileName = Utils.GetLogPath("${shortdate}.txt");
        config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, fileTarget));
        LogManager.Configuration = config;
    }

    public static void LoggingEnabled(bool enable)
    {
        if (!enable)
        {
            LogManager.SuspendLogging();
        }
    }

    public static void SaveLog(string strContent)
    {
        if (!LogManager.IsLoggingEnabled())
        {
            return;
        }

        // Format the log entry with timestamp and category
        var formattedContent = FormatLogEntry(strContent);

        // Also output to console for immediate visibility in V2Ray logs
        Console.WriteLine(formattedContent);

        _logger1.Info(strContent);
    }

    /// <summary>
    /// Format log entry with proper categorization and emojis
    /// </summary>
    private static string FormatLogEntry(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        
        // Categorize and format based on content
        if (content.Contains("🚨") || content.Contains("403") || content.Contains("ERROR"))
        {
            return $"[{timestamp}] [EMERGENCY] {content}";
        }
        else if (content.Contains("✅") || content.Contains("SUCCESS"))
        {
            return $"[{timestamp}] [SUCCESS] {content}";
        }
        else if (content.Contains("⚠️") || content.Contains("WARNING"))
        {
            return $"[{timestamp}] [WARNING] {content}";
        }
        else if (content.Contains("🔄") || content.Contains("DNS") || content.Contains("RELOAD"))
        {
            return $"[{timestamp}] [CONFIG] {content}";
        }
        else if (content.Contains("IRAN-BYPASS") || content.Contains("SanctionsBypass"))
        {
            return $"[{timestamp}] [SANCTIONS] {content}";
        }
        else
        {
            return $"[{timestamp}] [INFO] {content}";
        }
    }

    public static void SaveLog(string strTitle, Exception ex)
    {
        if (!LogManager.IsLoggingEnabled())
        {
            return;
        }

        _logger2.Debug($"{strTitle},{ex.Message}");
        _logger2.Debug(ex.StackTrace);
        if (ex?.InnerException != null)
        {
            _logger2.Error(ex.InnerException);
        }
    }
}
