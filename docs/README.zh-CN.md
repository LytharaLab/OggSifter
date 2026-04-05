# OggSifter

Roblox音频缓存提取与筛选工具 - 轻松从Roblox缓存中提取OGG音频文件。

## 功能特性

- **智能扫描**: 递归扫描指定文件夹，自动识别包含OGG音频的文件
- **魔数检测**: 从文件中找到"OggS"魔数并提取有效的OGG音频
- **内置播放器**: 直接在程序中播放提取的音频，无需额外软件
- **筛选保存**: 可以逐个试听并保存你需要的音频文件

## 系统要求

- Windows 操作系统
- .NET 9.0 运行时

## 使用方法

1. **选择源文件夹**: 默认会自动检测 Roblox 缓存目录（`%USERPROFILE%\AppData\Local\Roblox\rbx-storage`），也可以手动选择其他文件夹
2. **选择保存目录**: 选择你想保存最终音频的位置
3. **扫描并提取**: 点击"扫描并提取"按钮开始扫描
4. **试听与筛选**:
   - 使用"上一个"和"下一个"浏览提取的音频
   - 点击"播放"试听当前音频
   - 满意的话点击"保存此音频"保存到指定目录
5. **管理临时文件**: 点击"打开临时目录"可以查看所有提取的临时文件

## 构建项目

```bash
# 克隆仓库
git clone https://github.com/LytharaLab/OggSifter.git

# 进入项目目录
cd OggSifter

# 还原 NuGet 包
dotnet restore

# 构建项目
dotnet build

# 运行项目
dotnet run --project OggSifter
```

## 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

---

**注意**: 本工具仅供学习和个人使用。请尊重版权，不要使用本工具提取或传播受版权保护的音频内容。
