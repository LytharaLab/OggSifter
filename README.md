# OggSifter

Roblox Audio Cache Extractor & Filter - Easily extract OGG audio files from Roblox cache.

---

**Localized Versions**: [简体中文](docs/README.zh-CN.md) | [Русский](docs/README.ru-RU.md) | [日本語](docs/README.ja-JP.md)

---

## Features

- **Smart Scanning**: Recursively scan specified folders and automatically identify files containing OGG audio
- **Magic Number Detection**: Locate the "OggS" magic number in files and extract valid OGG audio
- **Built-in Player**: Play extracted audio directly in the program without additional software
- **Filter & Save**: Listen to and save only the audio files you need

## System Requirements

- Windows operating system
- .NET 9.0 runtime

## Usage

1. **Select Source Folder**: Automatically detects the Roblox cache directory by default (`%USERPROFILE%\AppData\Local\Roblox\rbx-storage`), or manually select another folder
2. **Select Save Directory**: Choose where you want to save the final audio files
3. **Scan & Extract**: Click the "Scan & Extract" button to begin scanning
4. **Preview & Filter**:
   - Use "Previous" and "Next" to browse extracted audio
   - Click "Play" to preview the current audio
   - Click "Save This Audio" to save to your specified directory if you're satisfied
5. **Manage Temporary Files**: Click "Open Temp Directory" to view all extracted temporary files

## Building the Project

```bash
# Clone the repository
git clone https://github.com/LytharaLab/OggSifter.git

# Enter project directory
cd OggSifter

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the project
dotnet run --project OggSifter
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

**Note**: This tool is for educational and personal use only. Please respect copyright laws and do not use this tool to extract or distribute copyrighted audio content.
