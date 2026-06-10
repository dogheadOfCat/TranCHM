<p align="center">
  <h1 align="center">CHM Converter</h1>
  <p align="center">将 CHM 帮助文件批量转换为 HTML 网页的现代 Windows 桌面工具</p>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-6C5CE7?logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-4A90D9?logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/license-MIT-00B894" alt="License">
  <img src="https://img.shields.io/badge/dependencies-zero-00CEC9" alt="Dependencies">
</p>

---

## ✨ 功能特性

- 🗂️ **批量选择** — 一次选择多个 `.chm` 文件，支持去重
- 🚀 **批量转换** — 自动逐个反编译 CHM 为完整 HTML 站点
- 📂 **自定义输出** — 每个 CHM 独立子文件夹，自动创建目录结构
- 📊 **实时进度** — 进度条 + 当前文件状态，直观展示转换进度
- 📝 **双通道日志** — UI 彩色日志 + 文件日志（按天切割）
- 🎨 **三套主题** — Dark Purple / Dark Blue / Light Modern，下拉菜单即时切换
- 🪟 **现代 UI** — 自定义标题栏、圆角窗口、无缝窗口控制按钮
- 🔒 **零依赖** — 纯 .NET 8 WPF，无任何 NuGet 包
- 📈 **统计面板** — 成功/失败计数、转换耗时一览
- 🔄 **可取消** — 支持中途取消，自动清理已生成的临时文件
- 📜 **独立脚本** — 附带 `chm2html.bat`，无需 GUI 也能用

---

## 🖥 界面

![CHM Converter UI](UI.png)

---

## 🎨 主题

内置三套主题，点击标题栏 🌓 下拉选择：

| 主题 | 风格 | 强调色 |
|------|------|--------|
| 🟣 **Dark Purple** | VS Code / Discord 暗色风 | `#6C5CE7` 紫 |
| 🔵 **Dark Blue** | JetBrains / Azure 暗色风 | `#4A90D9` 蓝 |
| ⚪ **Light Modern** | Windows 11 亮色风 | `#6C5CE7` 紫 |

主题通过 `DynamicResource` 实时切换，所有控件颜色即时更新，无需重启。

---

## 📁 项目结构

```
CHMConverter/
├── App.xaml                        # 应用程序资源、控件样式
├── App.xaml.cs                     # 主题切换逻辑
├── MainWindow.xaml                 # 主窗口界面（自定义标题栏）
├── MainWindow.xaml.cs              # 窗口控制 & 交互逻辑
├── CHMConverter.csproj             # .NET 8 WPF 项目文件
├── app.manifest                    # Windows 应用清单
├── chm2html.bat                    # 独立 BAT 脚本
├── Services/
│   ├── ChmConverterService.cs      # CHM → HTML 转换核心
│   └── LogService.cs               # 双通道日志服务
├── Themes/
│   ├── DarkPurple.xaml             # 暗紫主题
│   ├── DarkBlue.xaml               # 暗蓝主题
│   └── LightModern.xaml            # 亮色主题
└── .vscode/
    ├── launch.json                 # VS Code 调试配置
    └── tasks.json                  # VS Code 构建任务
```

---

## 🚀 快速开始

### 环境要求

| 项目 | 说明 |
|------|------|
| 操作系统 | Windows 10 / 11 |
| .NET SDK | [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高 |
| hh.exe | Windows 自带（`C:\Windows\hh.exe`） |

### 构建 & 运行

```bash
# 克隆项目
git clone https://github.com/yourname/CHMConverter.git
cd CHMConverter

# 还原依赖 & 构建
dotnet restore
dotnet build -c Release

# 运行
dotnet run

# 发布独立可执行文件
dotnet publish -c Release -o ./publish
```

### VS Code 调试

已预置 `launch.json` / `tasks.json`，直接按 `F5` 启动调试。

---

## 📖 使用说明

| 步骤 | 操作 |
|------|------|
| **1. 选择文件** | 点击「📁 选择 CHM 文件」→ 多选 `.chm` 文件 |
| **2. 选择输出** | 点击「📂 选择输出目录」→ 选择 HTML 存放位置 |
| **3. 开始转换** | 点击「🚀 开始转换」→ 等待进度完成 |
| **4. 查看结果** | 自动打开资源管理器定位到输出目录 |

> 💡 每个 CHM 文件生成一个以文件名命名的独立子文件夹，内含完整的 HTML/CSS/JS/图片。
