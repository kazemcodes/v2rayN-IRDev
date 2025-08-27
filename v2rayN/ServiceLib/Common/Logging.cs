using NLog;
using NLog.Config;
using NLog.Targets;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace ServiceLib.Common;

public class Logging
{
    private static readonly Logger _logger1 = LogManager.GetLogger("Log1");
    private static readonly Logger _logger2 = LogManager.GetLogger("Log2");
    private static readonly Logger _performanceLogger = LogManager.GetLogger("Performance");
    private static readonly ConcurrentDictionary<string, PerformanceMonitor> _performanceMonitors = new();
    private static readonly Stopwatch _appStartTime = Stopwatch.StartNew();

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
    /// Enhanced logging with performance metrics and structured data
    /// </summary>
    public static void LogWithMetrics(string operation, string message, TimeSpan? duration = null, string category = "INFO")
    {
        var timestamp = DateTime.Now;
        var uptime = _appStartTime.Elapsed;
        var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024; // MB

        var enhancedMessage = $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{category}] [UPTIME:{uptime:hh\\:mm\\:ss}] [MEM:{memoryUsage}MB] ";
        if (duration.HasValue)
        {
            enhancedMessage += $"[DURATION:{duration.Value.TotalMilliseconds:F0}ms] ";
        }
        enhancedMessage += $"{operation}: {message}";

        SaveLog(enhancedMessage);
    }

    /// <summary>
    /// Start performance monitoring for an operation
    /// </summary>
    public static string StartPerformanceMonitor(string operationName)
    {
        var monitorId = Guid.NewGuid().ToString();
        var monitor = new PerformanceMonitor(operationName, Stopwatch.StartNew());
        _performanceMonitors[monitorId] = monitor;

        LogWithMetrics(operationName, "STARTED", category: "PERFORMANCE");
        return monitorId;
    }

    /// <summary>
    /// Stop performance monitoring and log results
    /// </summary>
    public static void StopPerformanceMonitor(string monitorId, string additionalInfo = null)
    {
        if (_performanceMonitors.TryRemove(monitorId, out var monitor))
        {
            monitor.Stopwatch.Stop();
            var duration = monitor.Stopwatch.Elapsed;

            var message = $"COMPLETED in {duration.TotalMilliseconds:F0}ms";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                message += $" - {additionalInfo}";
            }

            LogWithMetrics(monitor.OperationName, message, duration, "PERFORMANCE");
        }
    }

    /// <summary>
    /// Log DNS-related operations with enhanced metrics
    /// </summary>
    public static void LogDnsOperation(string dnsServer, string operation, bool success, TimeSpan duration, string details = null)
    {
        var status = success ? "SUCCESS" : "FAILED";
        var message = $"{operation} via {dnsServer} - {status}";
        if (!string.IsNullOrEmpty(details))
        {
            message += $" ({details})";
        }

        LogWithMetrics("DNS_OPERATION", message, duration, success ? "SUCCESS" : "ERROR");
    }

    /// <summary>
    /// Log sanctions bypass events
    /// </summary>
    public static void LogSanctionsEvent(string eventType, string details, bool success = true)
    {
        var status = success ? "SUCCESS" : "FAILED";
        var message = $"{eventType}: {details}";

        LogWithMetrics("SANCTIONS_BYPASS", message, category: success ? "SUCCESS" : "WARNING");
    }

    /// <summary>
    /// Log system health metrics
    /// </summary>
    public static void LogSystemHealth(string component, string status, double responseTime, string details = null)
    {
        var message = $"{component} - {status} ({responseTime:F0}ms)";
        if (!string.IsNullOrEmpty(details))
        {
            message += $" - {details}";
        }

        LogWithMetrics("SYSTEM_HEALTH", message, category: status.Contains("ERROR") || status.Contains("FAILED") ? "ERROR" : "INFO");
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

    /// <summary>
    /// Clean up old performance monitors to prevent memory leaks
    /// </summary>
    public static void CleanupPerformanceMonitors()
    {
        var expiredMonitors = new List<string>();
        var now = DateTime.Now;

        foreach (var kvp in _performanceMonitors)
        {
            // Remove monitors older than 1 hour
            if (now - kvp.Value.StartTime > TimeSpan.FromHours(1))
            {
                expiredMonitors.Add(kvp.Key);
            }
        }

        foreach (var key in expiredMonitors)
        {
            _performanceMonitors.TryRemove(key, out _);
        }

        if (expiredMonitors.Count > 0)
        {
            LogWithMetrics("PERFORMANCE_CLEANUP", $"Removed {expiredMonitors.Count} expired monitors", category: "MAINTENANCE");
        }
    }
}

/// <summary>
/// Performance monitor for tracking operation duration
/// </summary>
public class PerformanceMonitor
{
    public string OperationName { get; }
    public Stopwatch Stopwatch { get; }
    public DateTime StartTime { get; }

    public PerformanceMonitor(string operationName, Stopwatch stopwatch)
    {
        OperationName = operationName;
        Stopwatch = stopwatch;
        StartTime = DateTime.Now;
    }
}
