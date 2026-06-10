using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using CHMConverter.Services;

// 消除 WPF / WinForms 命名空间歧义
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using MessageBox = System.Windows.MessageBox;

namespace CHMConverter;

public partial class MainWindow : Window
{
    private readonly LogService _log;
    private readonly ChmConverterService _converter;
    private readonly ObservableCollection<string> _fileList = new();
    private string? _outputDir;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();

        _log = new LogService();
        _converter = new ChmConverterService(_log);

        // 绑定日志到 UI
        LogItemsControl.ItemsSource = _log.LogEntries;

        // 监听日志新增，自动滚动到底部
        _log.LogEntries.CollectionChanged += (_, _) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                LogScrollViewer.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        };

        // 绑定文件列表
        LstFiles.ItemsSource = _fileList;
    }

    // ==================== 窗口控制 ====================

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            ShowRestoreIcon();
        // 初始化主题菜单选中状态
        UpdateThemeMenuChecks();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            ShowRestoreIcon();
            // 最大化时去掉圆角
            var chrome = WindowChrome.GetWindowChrome(this);
            if (chrome != null) chrome.CornerRadius = new CornerRadius(0);
        }
        else
        {
            ShowMaximizeIcon();
            var chrome = WindowChrome.GetWindowChrome(this);
            if (chrome != null) chrome.CornerRadius = new CornerRadius(8);
        }
    }

    /// <summary>标题栏按住拖动窗口（双击最大化/还原）</summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            BtnMaxRestore_Click(sender, e);
        }
        else
        {
            DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnMaxRestore_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
        else
            WindowState = WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnThemeMenu_Click(object sender, RoutedEventArgs e)
    {
        // 左键点击打开下拉菜单（ContextMenu 默认只响应右键）
        ThemeContextMenu.PlacementTarget = BtnThemeMenu;
        ThemeContextMenu.IsOpen = true;
    }

    private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && item.Tag is string theme)
        {
            App.SwitchTheme(theme);
            UpdateThemeMenuChecks();
            _log.Info($"主题已切换至: {theme}");
        }
    }

    private void UpdateThemeMenuChecks()
    {
        foreach (System.Windows.Controls.MenuItem item in ThemeContextMenu.Items)
        {
            item.IsChecked = (item.Tag as string) == App.CurrentTheme;
        }
    }

    private void ShowMaximizeIcon()
    {
        IconMaximize.Visibility = Visibility.Visible;
        IconRestore.Visibility = Visibility.Collapsed;
        BtnMaxRestore.ToolTip = "最大化";
    }

    private void ShowRestoreIcon()
    {
        IconMaximize.Visibility = Visibility.Collapsed;
        IconRestore.Visibility = Visibility.Visible;
        BtnMaxRestore.ToolTip = "还原";
    }

    // ==================== 选择 CHM 文件 ====================

    private void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择 CHM 文件",
            Filter = "CHM 文件 (*.chm)|*.chm|所有文件 (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog() == true)
        {
            foreach (var file in dlg.FileNames)
            {
                if (!_fileList.Contains(file))
                {
                    _fileList.Add(file);
                    _log.Info($"已添加文件: {file}");
                }
            }
            UpdateFileCount();
        }
    }

    // ==================== 选择输出目录 ====================

    private void BtnSelectOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择 HTML 输出目录",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        // 使用 Windows Forms FolderBrowserDialog（WPF 没有内置的）
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _outputDir = dlg.SelectedPath;
            TxtOutputPath.Text = _outputDir;
            _log.Info($"输出目录已设置: {_outputDir}");
        }
    }

    // ==================== 清空文件列表 ====================

    private void BtnClearFiles_Click(object sender, RoutedEventArgs e)
    {
        _fileList.Clear();
        UpdateFileCount();
        _log.Info("文件列表已清空");
    }

    // ==================== 清空日志 ====================

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        _log.LogEntries.Clear();
        _log.Info("日志已清空");
    }

    // ==================== 开始转换 ====================

    private async void BtnConvert_Click(object sender, RoutedEventArgs e)
    {
        // 验证输入
        if (_fileList.Count == 0)
        {
            MessageBox.Show("请先选择至少一个 CHM 文件。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_outputDir))
        {
            MessageBox.Show("请先选择输出目录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 禁用按钮，防止重复点击
        SetUIEnabled(false);

        _cts = new CancellationTokenSource();
        var sw = Stopwatch.StartNew();

        int successCount = 0;
        int failCount = 0;

        try
        {
            _log.Info("========================================");
            _log.Info($"开始批量转换，共 {_fileList.Count} 个文件");
            _log.Info("========================================");

            var total = _fileList.Count;
            for (int i = 0; i < total; i++)
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    _log.Warning("用户取消转换。");
                    break;
                }

                var file = _fileList[i];
                var progress = (int)((double)i / total * 100);

                // 更新进度
                Dispatcher.Invoke(() =>
                {
                    PrgBar.Value = progress;
                    TxtProgress.Text = $"转换中 ({i + 1}/{total})";
                });

                var ok = await _converter.ConvertAsync(file, _outputDir, _cts.Token);

                if (ok) successCount++;
                else failCount++;

                // 更新统计
                Dispatcher.Invoke(() =>
                {
                    TxtSuccessCount.Text = successCount.ToString();
                    TxtFailCount.Text = failCount.ToString();
                });

                // 更新进度到当前完成位置
                var currentProgress = (int)((double)(i + 1) / total * 100);
                Dispatcher.Invoke(() => PrgBar.Value = currentProgress);
            }

            sw.Stop();

            _log.Info("========================================");
            _log.Success($"批量转换完成: 成功 {successCount}, 失败 {failCount}, 耗时 {sw.Elapsed:mm\\:ss\\.ff}");

            Dispatcher.Invoke(() =>
            {
                PrgBar.Value = 100;
                TxtProgress.Text = "完成";
                TxtElapsed.Text = $"{sw.Elapsed:mm\\:ss\\.ff}";
                TxtSuccessCount.Text = successCount.ToString();
                TxtFailCount.Text = failCount.ToString();
            });

            // 转换完成提示
            if (failCount == 0)
            {
                MessageBox.Show(
                    $"全部转换成功！\n成功: {successCount} 个文件\n输出目录: {_outputDir}\n耗时: {sw.Elapsed:mm\\:ss\\.ff}",
                    "转换完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    $"转换完成，部分文件失败。\n成功: {successCount} 个\n失败: {failCount} 个\n请查看日志了解详情。",
                    "转换完成", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // 打开输出目录
            if (successCount > 0)
            {
                try { Process.Start("explorer.exe", _outputDir); }
                catch { /* 忽略 */ }
            }
        }
        catch (Exception ex)
        {
            _log.Error($"批量转换异常: {ex.Message}");
            MessageBox.Show($"转换过程发生异常:\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetUIEnabled(true);
            _cts?.Dispose();
            _cts = null;
        }
    }

    // ==================== 辅助方法 ====================

    private void UpdateFileCount()
    {
        TxtFileCount.Text = _fileList.Count.ToString();
    }

    private void SetUIEnabled(bool enabled)
    {
        BtnSelectFiles.IsEnabled = enabled;
        BtnSelectOutput.IsEnabled = enabled;
        BtnClearFiles.IsEnabled = enabled;
        BtnConvert.IsEnabled = enabled;
        LstFiles.IsEnabled = enabled;
    }
}
