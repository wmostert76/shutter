// Shutter.exe
// Schedule shutdown or restart using a calendar + time picker.
// Mad by WAM-Sofware (c) since 1997.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("Shutter")]
[assembly: AssemblyProduct("Shutter")]
[assembly: AssemblyCompany("WAM-Software")]
[assembly: AssemblyCopyright("Mad by WAM-Sofware (c) since 1997.")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Shutter
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private const string AppTitle = "Shutter";
        private const string VersionLabel = "v1.0.0";
        private const int MaxShutdownSeconds = 315360000; // shutdown.exe /t max

        private readonly MonthCalendar _calendar;
        private readonly DateTimePicker _timePicker;
        private readonly TextBox _serverBox;
        private readonly RadioButton _rbShutdown;
        private readonly RadioButton _rbRestart;
        private readonly CheckBox _forceBox;
        private readonly Label _targetLabel;
        private readonly Label _secondsLabel;
        private readonly Label _statusLabel;
        private readonly TextBox _commandBox;
        private readonly Button _scheduleButton;
        private readonly Button _abortButton;
        private readonly Timer _uiTimer;

        public MainForm()
        {
            Text = AppTitle + " " + VersionLabel;
            Font = new Font("Segoe UI", 9f);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(780, 520);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Controls.Add(root);

            _calendar = new MonthCalendar
            {
                MaxSelectionCount = 1,
                Dock = DockStyle.Fill
            };
            _calendar.DateChanged += (s, e) => UpdateComputed();
            _calendar.DateSelected += (s, e) => UpdateComputed();
            root.Controls.Add(_calendar, 0, 0);

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
            };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // action
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // server
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // time
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // force
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // info
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // status spacer
            root.Controls.Add(right, 1, 0);

            var actionGroup = new GroupBox { Text = "Actie", Dock = DockStyle.Fill, Padding = new Padding(10) };
            var actionFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            _rbShutdown = new RadioButton { Text = "Shutdown (/s)", AutoSize = true, Checked = true, Margin = new Padding(4, 8, 18, 4) };
            _rbRestart = new RadioButton { Text = "Restart (/r)", AutoSize = true, Margin = new Padding(4, 8, 4, 4) };
            _rbShutdown.CheckedChanged += (s, e) => UpdateComputed();
            _rbRestart.CheckedChanged += (s, e) => UpdateComputed();
            actionFlow.Controls.Add(_rbShutdown);
            actionFlow.Controls.Add(_rbRestart);
            actionGroup.Controls.Add(actionFlow);
            right.Controls.Add(actionGroup, 0, 0);

            var serverGroup = new GroupBox { Text = "Server", Dock = DockStyle.Fill, Padding = new Padding(10) };
            var serverLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            serverLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            serverLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var serverLabel = new Label { Text = "Computernaam/IP (leeg = lokaal):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true };
            _serverBox = new TextBox { Dock = DockStyle.Fill };
            _serverBox.TextChanged += (s, e) => UpdateComputed();
            serverLayout.Controls.Add(serverLabel, 0, 0);
            serverLayout.Controls.Add(_serverBox, 1, 0);
            serverGroup.Controls.Add(serverLayout);
            right.Controls.Add(serverGroup, 0, 1);

            var timeGroup = new GroupBox { Text = "Planning", Dock = DockStyle.Fill, Padding = new Padding(10) };
            var timeLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            timeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            timeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var timeLabel = new Label { Text = "Tijd (HH:mm:ss):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true };
            _timePicker = new DateTimePicker
            {
                Dock = DockStyle.Left,
                Width = 120,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm:ss",
                ShowUpDown = true,
                Value = DateTime.Now.AddMinutes(5)
            };
            _timePicker.ValueChanged += (s, e) => UpdateComputed();
            timeLayout.Controls.Add(timeLabel, 0, 0);
            timeLayout.Controls.Add(_timePicker, 1, 0);
            timeGroup.Controls.Add(timeLayout);
            right.Controls.Add(timeGroup, 0, 2);

            _forceBox = new CheckBox { Text = "Forceer apps sluiten (/f)", Dock = DockStyle.Fill, Padding = new Padding(6, 6, 6, 6) };
            _forceBox.CheckedChanged += (s, e) => UpdateComputed();
            right.Controls.Add(_forceBox, 0, 3);

            var infoPanel = new Panel { Dock = DockStyle.Fill };
            _targetLabel = new Label { AutoSize = true, Location = new Point(0, 4) };
            _secondsLabel = new Label { AutoSize = true, Location = new Point(0, 26) };
            infoPanel.Controls.Add(_targetLabel);
            infoPanel.Controls.Add(_secondsLabel);
            right.Controls.Add(infoPanel, 0, 4);

            _statusLabel = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 40,
                Padding = new Padding(0, 12, 0, 0)
            };
            right.Controls.Add(_statusLabel, 0, 5);

            var commandGroup = new GroupBox { Text = "Shutdown commando", Dock = DockStyle.Fill, Padding = new Padding(10) };
            _commandBox = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };
            commandGroup.Controls.Add(_commandBox);
            root.Controls.Add(commandGroup, 0, 1);
            root.SetColumnSpan(commandGroup, 2);

            var footer = new Label
            {
                Text = "Mad by WAM-Sofware (c) since 1997.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            _scheduleButton = new Button { Text = "Plan", Width = 110, Height = 28, Margin = new Padding(6, 6, 0, 6) };
            _abortButton = new Button { Text = "Annuleer", Width = 110, Height = 28, Margin = new Padding(6, 6, 0, 6) };
            _scheduleButton.Click += (s, e) => Schedule();
            _abortButton.Click += (s, e) => Abort();
            buttonPanel.Controls.Add(_scheduleButton);
            buttonPanel.Controls.Add(_abortButton);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            bottom.Controls.Add(footer, 0, 0);
            bottom.Controls.Add(buttonPanel, 1, 0);
            root.Controls.Add(bottom, 0, 2);
            root.SetColumnSpan(bottom, 2);

            _uiTimer = new Timer { Interval = 500 };
            _uiTimer.Tick += (s, e) => UpdateComputed();
            _uiTimer.Start();

            UpdateComputed();
        }

        private DateTime GetTargetDateTime()
        {
            var date = _calendar.SelectionStart.Date;
            var time = _timePicker.Value.TimeOfDay;
            return date.Add(time);
        }

        private void UpdateComputed()
        {
            var target = GetTargetDateTime();
            var seconds = (int)Math.Ceiling((target - DateTime.Now).TotalSeconds);

            _targetLabel.Text = "Gepland: " + target.ToString("yyyy-MM-dd HH:mm:ss");
            if (seconds < 0)
            {
                _secondsLabel.Text = "Seconden tot actie: " + seconds + " (tijd ligt in het verleden)";
                _secondsLabel.ForeColor = Color.DarkRed;
            }
            else if (seconds > MaxShutdownSeconds)
            {
                _secondsLabel.Text = "Seconden tot actie: " + seconds + " (te groot; max " + MaxShutdownSeconds + ")";
                _secondsLabel.ForeColor = Color.DarkRed;
            }
            else
            {
                _secondsLabel.Text = "Seconden tot actie: " + seconds;
                _secondsLabel.ForeColor = Color.Black;
            }

            var cmd = BuildShutdownArguments(seconds);
            _commandBox.Text = "shutdown.exe " + cmd;
        }

        private string BuildShutdownArguments(int seconds)
        {
            var server = NormalizeServer(_serverBox.Text);
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(server))
            {
                sb.Append("/m ");
                sb.Append(server);
                sb.Append(' ');
            }

            sb.Append(_rbRestart.Checked ? "/r " : "/s ");
            sb.Append("/t ");
            sb.Append(seconds < 0 ? 0 : seconds);

            if (_forceBox.Checked)
            {
                sb.Append(" /f");
            }

            return sb.ToString().Trim();
        }

        private static string NormalizeServer(string serverInput)
        {
            var s = (serverInput ?? "").Trim();
            if (s.Length == 0)
            {
                return "";
            }

            if (s.StartsWith(@"\\"))
            {
                return s;
            }

            return @"\\" + s;
        }

        private void Schedule()
        {
            var target = GetTargetDateTime();
            var seconds = (int)Math.Ceiling((target - DateTime.Now).TotalSeconds);

            if (seconds < 0)
            {
                MessageBox.Show("Kies een datum/tijd in de toekomst.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (seconds > MaxShutdownSeconds)
            {
                MessageBox.Show("De gekozen tijd is te ver weg. Max /t is " + MaxShutdownSeconds + " seconden.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var action = _rbRestart.Checked ? "RESTART" : "SHUTDOWN";
            var serverDisplay = string.IsNullOrWhiteSpace(_serverBox.Text) ? "lokaal" : _serverBox.Text.Trim();
            var cmd = "shutdown.exe " + BuildShutdownArguments(seconds);

            var confirm = MessageBox.Show(
                "Bevestigen?\n\nActie: " + action + "\nServer: " + serverDisplay + "\nTijd: " + target.ToString("yyyy-MM-dd HH:mm:ss") + "\nSeconden: " + seconds + "\n\nCommando:\n" + cmd,
                AppTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                _statusLabel.Text = "Status: geannuleerd door gebruiker.";
                _statusLabel.ForeColor = Color.DimGray;
                return;
            }

            var args = BuildShutdownArguments(seconds);
            try
            {
                var result = RunShutdown(args);
                if (result.ExitCode != 0)
                {
                    MessageBox.Show("shutdown.exe faalde (exit " + result.ExitCode + ").\n\n" + result.Output, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = "Status: fout bij uitvoeren.";
                    _statusLabel.ForeColor = Color.DarkRed;
                    return;
                }

                _statusLabel.Text = "Status: gepland.";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "Status: fout bij uitvoeren.";
                _statusLabel.ForeColor = Color.DarkRed;
            }
        }

        private void Abort()
        {
            var server = NormalizeServer(_serverBox.Text);
            var args = string.IsNullOrWhiteSpace(server) ? "/a" : ("/a /m " + server);

            try
            {
                var result = RunShutdown(args);
                if (result.ExitCode != 0)
                {
                    MessageBox.Show("Annuleren faalde (exit " + result.ExitCode + ").\n\n" + result.Output, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = "Status: annuleren faalde.";
                    _statusLabel.ForeColor = Color.DarkRed;
                    return;
                }

                _statusLabel.Text = "Status: shutdown/restart geannuleerd.";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "Status: fout bij annuleren.";
                _statusLabel.ForeColor = Color.DarkRed;
            }
        }

        private static ShutdownResult RunShutdown(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (var p = new Process { StartInfo = psi })
            {
                if (!p.Start())
                {
                    throw new InvalidOperationException("Kon shutdown.exe niet starten.");
                }

                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();

                if (!p.WaitForExit(5000))
                {
                    try { p.Kill(); } catch { }
                    throw new TimeoutException("shutdown.exe reageerde niet binnen 5 seconden.");
                }

                var output = (stdout + "\n" + stderr).Trim();
                return new ShutdownResult(p.ExitCode, output);
            }
        }

        private sealed class ShutdownResult
        {
            public int ExitCode { get; private set; }
            public string Output { get; private set; }

            public ShutdownResult(int exitCode, string output)
            {
                ExitCode = exitCode;
                Output = output ?? "";
            }
        }
    }
}
