// MIT License
// 
// Copyright (c) 2026 LytharaLab
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OggSifter
{
    public sealed class MainForm : Form
    {
        private readonly TextBox _txtSource = new() { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Button _btnBrowseSource = new() { Text = "选择源文件夹", AutoSize = true };
        private readonly Button _btnExtract = new() { Text = "扫描并提取", AutoSize = false };

        private readonly TextBox _txtDest = new() { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Button _btnBrowseDest = new() { Text = "选择保存目录", AutoSize = true };

        private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100 };
        private readonly Label _lblStatus = new() { Dock = DockStyle.Fill, Text = "就绪", AutoEllipsis = true };

        private readonly Label _lblIndex = new() { Dock = DockStyle.Fill, Text = "0 / 0" };
        private readonly TextBox _txtInfo = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical };

        private readonly Button _btnPrev = new() { Text = "上一个", AutoSize = true, Enabled = false };
        private readonly Button _btnPlay = new() { Text = "播放", AutoSize = true, Enabled = false };
        private readonly Button _btnStop = new() { Text = "停止", AutoSize = true, Enabled = false };
        private readonly Button _btnNext = new() { Text = "下一个", AutoSize = true, Enabled = false };
        private readonly Button _btnSave = new() { Text = "保存此音频", AutoSize = true, Enabled = false };
        private readonly Button _btnOpenTemp = new() { Text = "打开临时目录", AutoSize = true, Enabled = false };

        private readonly OggPlayer _player = new();
        private readonly CancellationTokenSource _cts = new();

        private string? _sourceDir;
        private string? _destDir;
        private string? _tempDir;

        private List<ExtractedOgg> _items = new();
        private int _currentIndex = -1;

        public MainForm()
        {
            Text = $"OggSifter - Roblox音频缓存提取/筛选器 (by LytharaLab) (v{Program.VERSION.ToString()})";
            Width = 980;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(860, 560);

            FormClosing += (_, __) =>
            {
                try { _cts.Cancel(); } catch { }
                try { _player.Dispose(); } catch { }
            };

            _player.PlaybackError += (_, msg) => SafeUi(() => MessageBox.Show(this, msg, "播放失败", MessageBoxButtons.OK, MessageBoxIcon.Error));

            BuildUi();
            WireEvents();
            
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var robloxPath = Path.Combine(userProfile, "AppData", "Local", "Roblox", "rbx-storage");
            if (Directory.Exists(robloxPath))
            {
                _sourceDir = robloxPath;
                _txtSource.Text = robloxPath;
            }
        }

        private void BuildUi()
        {
            // Root layout: Path area / progress / info / controls
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12),
                AutoSize = false,
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // paths + extract
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // progress
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 68f));  // info
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 32f));  // controls
            Controls.Add(root);

            // --- Paths group (source/dest + big extract button) ---
            var pathsGroup = new GroupBox
            {
                Text = "路径设置",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10)
            };

            var paths = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 3,
                AutoSize = true
            };
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // labels
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // textboxes
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // browse buttons

            // Row 0: Source
            var lblSrc = new Label
            {
                Text = "源文件夹：",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 6, 8, 6)
            };
            _txtSource.Margin = new Padding(0, 4, 8, 4);
            _btnBrowseSource.Margin = new Padding(0, 3, 0, 3);

            paths.Controls.Add(lblSrc, 0, 0);
            paths.Controls.Add(_txtSource, 1, 0);
            paths.Controls.Add(_btnBrowseSource, 2, 0);

            // Row 1: Dest
            var lblDst = new Label
            {
                Text = "保存目录：",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 6, 8, 6)
            };
            _txtDest.Margin = new Padding(0, 4, 8, 4);
            _btnBrowseDest.Margin = new Padding(0, 3, 0, 3);

            paths.Controls.Add(lblDst, 0, 1);
            paths.Controls.Add(_txtDest, 1, 1);
            paths.Controls.Add(_btnBrowseDest, 2, 1);

            // Row 2: Big extract button (span full row)
            _btnExtract.Dock = DockStyle.Fill;
            _btnExtract.Height = 40;
            _btnExtract.Margin = new Padding(0, 10, 0, 2);
            _btnExtract.Font = new Font(Font.FontFamily, Font.Size + 1, FontStyle.Bold);

            paths.Controls.Add(_btnExtract, 0, 2);
            paths.SetColumnSpan(_btnExtract, 3);

            // Help note
            var note = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 6, 0, 0),
                Text = "提示：先选源文件夹，再点“扫描并提取”。提取完成后才能播放/保存。"
            };

            pathsGroup.Controls.Add(note);
            pathsGroup.Controls.Add(paths);

            // Ensure note below paths (order: add paths first then note dock top)
            paths.Dock = DockStyle.Top;
            note.Dock = DockStyle.Top;
            pathsGroup.Controls.Clear();
            pathsGroup.Controls.Add(note);
            pathsGroup.Controls.Add(paths);

            root.Controls.Add(pathsGroup, 0, 0);

            // --- Progress row ---
            var progRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 6)
            };
            progRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            progRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            progRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _progress.Margin = new Padding(0, 3, 8, 3);
            _lblStatus.Margin = new Padding(0, 6, 8, 6);
            _lblIndex.Margin = new Padding(0, 6, 0, 6);

            progRow.Controls.Add(_progress, 0, 0);
            progRow.Controls.Add(_lblStatus, 1, 0);
            progRow.Controls.Add(_lblIndex, 2, 0);

            root.Controls.Add(progRow, 0, 1);

            // --- Info box ---
            var infoGroup = new GroupBox { Text = "当前音频信息 / 日志", Dock = DockStyle.Fill, Padding = new Padding(10) };
            _txtInfo.Dock = DockStyle.Fill;
            infoGroup.Controls.Add(_txtInfo);
            root.Controls.Add(infoGroup, 0, 2);

            // --- Controls ---
            var ctrlGroup = new GroupBox { Text = "筛选控制", Dock = DockStyle.Fill, Padding = new Padding(10) };

            var ctrl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(2),
            };

            // Put frequently-used buttons first, and also include OpenTemp here so it doesn't “失踪”
            ctrl.Controls.Add(_btnPrev);
            ctrl.Controls.Add(_btnPlay);
            ctrl.Controls.Add(_btnStop);
            ctrl.Controls.Add(_btnNext);
            ctrl.Controls.Add(_btnSave);
            ctrl.Controls.Add(_btnOpenTemp);

            ctrlGroup.Controls.Add(ctrl);
            root.Controls.Add(ctrlGroup, 0, 3);
        }

        private void WireEvents()
        {
            _btnBrowseSource.Click += (_, __) =>
            {
                using var fbd = new FolderBrowserDialog
                {
                    Description = "选择要扫描的 Roblox 缓存/资源文件夹（会递归扫描子目录）",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = false
                };

                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    _sourceDir = fbd.SelectedPath;
                    _txtSource.Text = _sourceDir;
                }
            };

            _btnBrowseDest.Click += (_, __) =>
            {
                using var fbd = new FolderBrowserDialog
                {
                    Description = "选择你确认正确的音频要保存到哪里",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };

                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    _destDir = fbd.SelectedPath;
                    _txtDest.Text = _destDir;
                }
            };

            _btnExtract.Click += async (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(_sourceDir) || !Directory.Exists(_sourceDir))
                {
                    MessageBox.Show(this, "请先选择一个有效的源文件夹。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                await StartExtractionAsync(_sourceDir);
            };

            _btnOpenTemp.Click += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(_tempDir) && Directory.Exists(_tempDir))
                {
                    try { Process.Start(new ProcessStartInfo { FileName = _tempDir, UseShellExecute = true }); }
                    catch { }
                }
            };

            _btnPlay.Click += (_, __) =>
            {
                var item = CurrentItemOrNull();
                if (item == null) return;
                try
                {
                    _player.Play(item.ExtractedPath);
                    AppendLog($"PLAY: {Path.GetFileName(item.ExtractedPath)}");
                }
                catch (Exception ex)
                {
                    AppendLog($"PLAY ERROR: {ex.Message}");
                }
            };

            _btnStop.Click += (_, __) =>
            {
                _player.Stop();
                AppendLog("STOP");
            };

            _btnPrev.Click += (_, __) => MoveIndex(-1);
            _btnNext.Click += (_, __) => MoveIndex(+1);

            _btnSave.Click += (_, __) =>
            {
                var item = CurrentItemOrNull();
                if (item == null) return;

                if (string.IsNullOrWhiteSpace(_destDir) || !Directory.Exists(_destDir))
                {
                    MessageBox.Show(this, "请先选择一个有效的保存目录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    string baseName = Path.GetFileName(item.ExtractedPath);
                    string src = Path.GetFileName(item.SourcePath);
                    string targetName = $"{Path.GetFileNameWithoutExtension(baseName)}__SRC_{Sanitize(src)}.ogg";
                    string targetPath = Path.Combine(_destDir!, targetName);

                    int k = 1;
                    while (File.Exists(targetPath))
                    {
                        targetPath = Path.Combine(_destDir!, $"{Path.GetFileNameWithoutExtension(targetName)}_{k}.ogg");
                        k++;
                    }

                    File.Copy(item.ExtractedPath, targetPath);
                    AppendLog($"SAVED -> {targetPath}");
                    MessageBox.Show(this, "已保存！你可以继续听下一个。", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    AppendLog($"SAVE ERROR: {ex.Message}");
                    MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private async Task StartExtractionAsync(string sourceDir)
        {
            LockUiWhileExtracting(true);
            _player.Stop();
            _items = new List<ExtractedOgg>();
            _currentIndex = -1;

            _tempDir = Path.Combine(Path.GetTempPath(), "RobloxOggExtractor", DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            _btnOpenTemp.Enabled = true;
            AppendLog($"TEMP DIR: {_tempDir}");
            AppendLog($"LOG: 请稍等,正在扫描 {"rbx-storage 缓存目录"}");

            var prog = new Progress<(int scanned, int extracted, string currentFile)>(p =>
            {
                if (p.currentFile == "DONE")
                {
                    _progress.Value = 100;
                    _lblStatus.Text = $"完成：扫描 {p.scanned} 个文件，提取 {p.extracted} 个 OGG";
                }
                else
                {
                    _progress.Value = p.scanned % 101;
                    _lblStatus.Text = $"扫描 {p.scanned} / 提取 {p.extracted}：{Path.GetFileName(p.currentFile)}";
                }
            });

            try
            {
                _items = await OggMagicExtractor.ExtractFromFolderAsync(sourceDir, _tempDir, prog, _cts.Token);
                AppendLog($"EXTRACTED: {_items.Count}");

                if (_items.Count == 0)
                {
                    MessageBox.Show(this, "没有找到包含 OggS 魔数的文件。", "结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LockUiWhileExtracting(false);
                    UpdateNavButtons();
                    return;
                }

                _currentIndex = 0;
                ShowCurrent();
                LockUiWhileExtracting(false);
            }
            catch (OperationCanceledException)
            {
                AppendLog("CANCELLED");
                LockUiWhileExtracting(false);
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR: {ex}");
                MessageBox.Show(this, ex.Message, "提取失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LockUiWhileExtracting(false);
            }
        }

        private void LockUiWhileExtracting(bool extracting)
        {
            _btnBrowseSource.Enabled = !extracting;
            _btnExtract.Enabled = !extracting;
            _btnBrowseDest.Enabled = !extracting;
            _btnPrev.Enabled = !extracting && _items.Count > 0;
            _btnNext.Enabled = !extracting && _items.Count > 0;
            _btnPlay.Enabled = !extracting && _items.Count > 0;
            _btnStop.Enabled = !extracting && _items.Count > 0;
            _btnSave.Enabled = !extracting && _items.Count > 0;

            if (extracting)
            {
                _lblStatus.Text = "正在扫描并提取…";
                _progress.Value = 0;
            }

            UpdateNavButtons();
        }

        private void UpdateNavButtons()
        {
            bool has = _items.Count > 0 && _currentIndex >= 0 && _currentIndex < _items.Count;
            _btnPrev.Enabled = has && _currentIndex > 0 && _btnBrowseSource.Enabled;
            _btnNext.Enabled = has && _currentIndex < _items.Count - 1 && _btnBrowseSource.Enabled;
            _btnPlay.Enabled = has && _btnBrowseSource.Enabled;
            _btnStop.Enabled = has && _btnBrowseSource.Enabled;
            _btnSave.Enabled = has && _btnBrowseSource.Enabled;
            _lblIndex.Text = has ? $"{_currentIndex + 1} / {_items.Count}" : $"0 / {_items.Count}";
        }

        private void MoveIndex(int delta)
        {
            if (_items.Count == 0) return;

            int next = _currentIndex + delta;
            if (next < 0) next = 0;
            if (next >= _items.Count) next = _items.Count - 1;

            if (next == _currentIndex) return;

            _player.Stop();
            _currentIndex = next;
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var item = CurrentItemOrNull();
            if (item == null) return;

            UpdateNavButtons();

            var name = Path.GetFileName(item.ExtractedPath);
            var src = item.SourcePath;

            _txtInfo.Text =
$@"当前：{name}
索引：{_currentIndex + 1}/{_items.Count}
源文件：{src}
源大小：{item.SourceSizeBytes} bytes
OggS 偏移：{item.OggOffsetBytes} bytes

操作建议：
1) 点【播放】听
2) 不对就【下一个】
3) 对了就【保存此音频】（先选保存目录）

备注：
- 如果 Roblox 缓存文件有前置垃圾字节，本程序会从第一次出现的 OggS 开始截取，以提高可播放率。
- 临时目录可随时打开手动查看/删。
";
        }

        private ExtractedOgg? CurrentItemOrNull()
        {
            if (_currentIndex < 0 || _currentIndex >= _items.Count) return null;
            return _items[_currentIndex];
        }

        private void AppendLog(string line)
        {
            SafeUi(() =>
            {
                _txtInfo.AppendText(Environment.NewLine + $"[{DateTime.Now:HH:mm:ss}] {line}");
            });
        }

        private void SafeUi(Action action)
        {
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            if (s.Length > 120) s = s.Substring(0, 120);
            return s;
        }
    }
}
