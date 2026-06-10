using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CHMConverter.Services;

/// <summary>
/// CHM 转换服务 —— 通过 Windows 内置 hh.exe 将 .chm 文件反编译为 HTML
/// </summary>
public class ChmConverterService
{
    private readonly LogService _log;

    public ChmConverterService(LogService log)
    {
        _log = log;
    }

    /// <summary>
    /// 将单个 CHM 文件转换为 HTML 输出到指定目录
    /// </summary>
    public async Task<bool> ConvertAsync(string chmFilePath, string outputDir, CancellationToken ct = default)
    {
        var fileName = Path.GetFileNameWithoutExtension(chmFilePath);
        var chmOutputDir = Path.Combine(outputDir, fileName);

        _log.Info($"开始转换: {Path.GetFileName(chmFilePath)}");

        try
        {
            // 确保输出子目录存在
            if (Directory.Exists(chmOutputDir))
            {
                _log.Warning($"输出目录已存在，将清空: {chmOutputDir}");
                Directory.Delete(chmOutputDir, true);
            }
            Directory.CreateDirectory(chmOutputDir);

            // 使用 Windows hh.exe 反编译 CHM
            var hhPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "hh.exe");

            if (!File.Exists(hhPath))
            {
                _log.Error("未找到 hh.exe，请确认系统已安装 HTML Help。");
                return false;
            }

            // 关键：hh.exe 的 ProcessStartInfo.Arguments 中不能使用引号，
            // 否则 hh.exe 无法正确解析参数。使用 8.3 短路径避免空格问题。
            var shortOutputDir = GetShortPath(chmOutputDir);
            var shortChmPath = GetShortPath(chmFilePath);

            var arguments = $"-decompile {shortOutputDir} {shortChmPath}";
            _log.Info($"执行命令: {hhPath} {arguments}");

            var psi = new ProcessStartInfo
            {
                FileName = hhPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                _log.Error("无法启动 hh.exe 进程。");
                return false;
            }

            // 先启动异步读取 stdout/stderr，防止缓冲区满导致进程阻塞
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            // 等待进程结束，超时 5 分钟
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                _log.Warning($"转换取消: {fileName}");
                return false;
            }
            catch (OperationCanceledException)
            {
                // 超时
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                _log.Error($"转换超时 (5 分钟): {fileName}");
                return false;
            }

            // 读取输出（仅用于日志）
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (!string.IsNullOrWhiteSpace(stdout))
                _log.Info($"输出: {stdout.Trim()}");
            if (!string.IsNullOrWhiteSpace(stderr))
                _log.Warning($"警告: {stderr.Trim()}");

            // 不依赖退出码，直接检查输出目录是否有文件
            var files = Directory.GetFiles(chmOutputDir, "*", SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                _log.Error($"转换失败: {fileName} — 未生成任何文件（exit code {process.ExitCode}）");
                try { Directory.Delete(chmOutputDir, true); } catch { /* 忽略 */ }
                return false;
            }

            _log.Success($"转换完成: {fileName} → 输出 {files.Length} 个文件到 {chmOutputDir}");
            return true;
        }
        catch (OperationCanceledException)
        {
            _log.Warning($"转换取消: {fileName}");
            return false;
        }
        catch (Exception ex)
        {
            _log.Error($"转换失败 {fileName}: {ex.Message}");
            return false;
        }
    }

    // ==================== P/Invoke ====================

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint GetShortPathName(
        [MarshalAs(UnmanagedType.LPTStr)] string lpszLongPath,
        [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszShortPath,
        uint cchBuffer);

    /// <summary>
    /// 将长路径转换为 8.3 短路径（无空格，hh.exe 可正确解析）
    /// </summary>
    private static string GetShortPath(string longPath)
    {
        // 先尝试获取所需缓冲区大小
        var length = GetShortPathName(longPath, null!, 0);
        if (length == 0)
            return longPath; // 转换失败，回退到长路径

        var sb = new StringBuilder((int)length);
        var result = GetShortPathName(longPath, sb, (uint)sb.Capacity);
        if (result == 0)
            return longPath; // 转换失败，回退到长路径

        return sb.ToString();
    }
}
