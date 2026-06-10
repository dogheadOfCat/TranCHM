using System.Collections.ObjectModel;
using System.IO;

namespace CHMConverter.Services;

/// <summary>
/// 日志条目 —— 包含时间戳、级别和消息内容
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = "INFO";
    public string Message { get; init; } = "";

    /// <summary>用于 UI 绑定的完整显示文本</summary>
    public string DisplayText => $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message}";
}

/// <summary>
/// 日志服务 —— 同时输出到 UI 绑定集合和本地日志文件
/// </summary>
public class LogService
{
    private readonly string _logFilePath;
    private readonly object _fileLock = new();

    /// <summary>供 UI 绑定的日志集合（每项为 LogEntry，支持按级别着色）</summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public LogService()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var logDir = Path.Combine(appDir, "Logs");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"CHMConverter_{DateTime.Now:yyyyMMdd}.log");

        Info("========================================");
        Info("CHM 转换工具启动");
        Info($"日志文件: {_logFilePath}");
        Info("========================================");
    }

    public void Info(string message)    => Append("INFO", message);
    public void Success(string message) => Append("OK", message);
    public void Warning(string message) => Append("WARN", message);
    public void Error(string message)   => Append("ERR", message);

    private void Append(string level, string message)
    {
        var timestamp = DateTime.Now;
        var line = $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

        var entry = new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message
        };

        // 写入 UI 集合（需调度到 UI 线程）
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(entry);
        });

        // 写入文件
        lock (_fileLock)
        {
            try { File.AppendAllText(_logFilePath, line + Environment.NewLine); }
            catch { /* 静默处理文件写入失败 */ }
        }
    }
}
