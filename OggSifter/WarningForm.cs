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
using System.Drawing;
using System.Windows.Forms;

namespace OggSifter
{
    public sealed class WarningForm : Form
    {
        public WarningForm()
        {
            Text = "OggSifter - 使用说明";
            Width = 680;
            Height = 520;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;

            BuildUi();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            var lblCopyright = new Label
            {
                Text = "LytharaLab 版权所有 (MIT License)",
                Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            root.Controls.Add(lblCopyright, 0, 0);

            var pathGroup = new GroupBox
            {
                Text = "Roblox 资源仓库路径",
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            var pathPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10),
                AutoSize = true
            };
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pathPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var txtPath = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = SystemColors.Control,
                Text = GetRobloxPath(),
                Margin = new Padding(0, 0, 8, 0),
                Height = 26
            };

            var btnCopy = new Button
            {
                Text = "复制路径",
                Width = 100,
                Height = 32,
                UseVisualStyleBackColor = true,
                Margin = new Padding(0, 0, 0, 0)
            };
            btnCopy.Click += (_, __) =>
            {
                Clipboard.SetText(txtPath.Text);
                MessageBox.Show(this, "路径已复制到剪贴板！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var lblHint = new Label
            {
                Text = "当前Roblox资源缓存位置",
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0)
            };

            pathPanel.Controls.Add(txtPath, 0, 0);
            pathPanel.Controls.Add(btnCopy, 1, 0);
            pathPanel.Controls.Add(lblHint, 0, 1);
            pathPanel.SetColumnSpan(lblHint, 2);
            pathGroup.Controls.Add(pathPanel);
            root.Controls.Add(pathGroup, 0, 1);

            var stepsGroup = new GroupBox
            {
                Text = "获取特定服务器资源的方法",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };

            var stepsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12)
            };
            for (int i = 0; i < 5; i++)
            {
                stepsPanel.RowStyles.Add(new RowStyle(i < 4 ? SizeType.AutoSize : SizeType.Percent, 100f));
            }

            var steps = new[]
            {
                "1. 删除 Roblox 资源仓库文件夹",
                "2. 打开 Roblox 并进入目标游戏",
                "3. 等待游戏完全加载后关闭 Roblox",
                "4. 使用本工具扫描资源仓库文件夹"
            };

            for (int i = 0; i < steps.Length; i++)
            {
                var stepPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    RowCount = 1,
                    Height = 32,
                    Margin = new Padding(0, 0, 0, 4)
                };
                stepPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                stepPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

                var stepNum = new Label
                {
                    Text = (i + 1).ToString(),
                    Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(0, 122, 204),
                    AutoSize = false,
                    Width = 28,
                    Height = 28,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(0, 2, 10, 2)
                };

                var stepText = new Label
                {
                    Text = steps[i],
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                stepPanel.Controls.Add(stepNum, 0, 0);
                stepPanel.Controls.Add(stepText, 1, 0);
                stepsPanel.Controls.Add(stepPanel, 0, i);
            }

            var note = new Label
            {
                Text = "⚠️ 注意：确保游戏完全加载后再关闭，这样资源文件才会完整保存。",
                ForeColor = Color.FromArgb(192, 80, 77),
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };
            stepsPanel.Controls.Add(note, 0, 4);

            stepsGroup.Controls.Add(stepsPanel);
            root.Controls.Add(stepsGroup, 0, 2);

            var btnOk = new Button
            {
                Text = "我知道了",
                DialogResult = DialogResult.OK,
                Width = 140,
                Height = 36,
                UseVisualStyleBackColor = true
            };
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40
            };
            btnPanel.Controls.Add(btnOk);
            root.Controls.Add(btnPanel, 0, 3);

            AcceptButton = btnOk;
        }

        private static string GetRobloxPath()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return System.IO.Path.Combine(userProfile, "AppData", "Local", "Roblox", "rbx-storage");
        }
    }
}
