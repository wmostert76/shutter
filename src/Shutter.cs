// Shutter.exe
// Schedule shutdown or restart using a calendar + time picker.
// Made by WAM-Sofware (c) since 1997.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("Shutter")]
[assembly: AssemblyProduct("Shutter")]
[assembly: AssemblyCompany("WAM-Software")]
[assembly: AssemblyCopyright("Made by WAM-Sofware (c) since 1997.")]
[assembly: AssemblyVersion("1.0.8.0")]
[assembly: AssemblyFileVersion("1.0.8.0")]

namespace Shutter
{
    internal static class Program
    {
        private const string AppTitle = "Shutter";
        private const string SingleInstanceMutexName = "Local\\WAMSoftware.Shutter.SingleInstance";
        private const string ShowExistingMessageName = "WAMSoftware.Shutter.ShowExisting";

        internal static readonly int ShowExistingMessageId = NativeMethods.RegisterWindowMessage(ShowExistingMessageName);

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, SingleInstanceMutexName, out createdNew))
            {
                if (!createdNew)
                {
                    TryActivateExistingInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }

        private static void TryActivateExistingInstance()
        {
            try
            {
                if (ShowExistingMessageId != 0)
                {
                    NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST, ShowExistingMessageId, IntPtr.Zero, IntPtr.Zero);
                    return;
                }
            }
            catch { }

            try
            {
                MessageBox.Show(AppTitle + " draait al.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { }
        }
    }

    internal static class NativeMethods
    {
        internal const int HWND_BROADCAST = 0xffff;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }

    internal sealed class MainForm : Form
    {
        private const string AppTitle = "Shutter";
        private const string VersionLabel = "v1.0.7";
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
        private readonly Button _aboutButton;
        private readonly Timer _uiTimer;
        private bool _countdownActive;
        private DateTime _countdownTarget;
        private string _countdownServer;
        private string _countdownCommand;
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _trayToggleItem;
        private ToolStripMenuItem _trayStartItem;
        private ToolStripMenuItem _trayStopItem;
        private ToolStripMenuItem _trayAboutItem;
        private bool _allowExit;
        private bool _minimizeBalloonShown;
        private Icon _appIcon;
        private Icon _trayIconImage;

        public MainForm()
        {
            Text = AppTitle + " " + VersionLabel;
            Font = new Font("Segoe UI", 9f);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(780, 520);

            _appIcon = LoadAppIcon();
            Icon = _appIcon ?? SystemIcons.Application;

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
            _rbShutdown = new RadioButton { Text = "Shutdown (/s)", AutoSize = true, Checked = false, Margin = new Padding(4, 8, 18, 4) };
            _rbRestart = new RadioButton { Text = "Restart (/r)", AutoSize = true, Checked = true, Margin = new Padding(4, 8, 4, 4) };
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
                Text = "Made by WAM-Sofware (c) since 1997.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            _scheduleButton = new Button { Text = "Start", Width = 110, Height = 28, Margin = new Padding(6, 6, 0, 6) };
            _abortButton = new Button { Text = "Stop", Width = 110, Height = 28, Margin = new Padding(6, 6, 0, 6) };
            _aboutButton = new Button { Text = "Over...", Width = 110, Height = 28, Margin = new Padding(6, 6, 0, 6) };
            _scheduleButton.Click += (s, e) => Schedule();
            _abortButton.Click += (s, e) => Abort();
            _aboutButton.Click += (s, e) => ShowAbout();
            buttonPanel.Controls.Add(_scheduleButton);
            buttonPanel.Controls.Add(_abortButton);
            buttonPanel.Controls.Add(_aboutButton);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
            bottom.Controls.Add(footer, 0, 0);
            bottom.Controls.Add(buttonPanel, 1, 0);
            root.Controls.Add(bottom, 0, 2);
            root.SetColumnSpan(bottom, 2);

            _uiTimer = new Timer { Interval = 500 };
            _uiTimer.Tick += (s, e) => UpdateComputed();
            _uiTimer.Start();

            InitializeTray();
            Resize += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    HideToTray(!_minimizeBalloonShown);
                    _minimizeBalloonShown = true;
                }
            };
            FormClosing += (s, e) =>
            {
                if (e.CloseReason != CloseReason.UserClosing)
                {
                    return;
                }

                if (_allowExit)
                {
                    return;
                }

                e.Cancel = true;
                HideToTray(!_minimizeBalloonShown);
                _minimizeBalloonShown = true;
            };
            FormClosed += (s, e) =>
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }

                if (_trayMenu != null)
                {
                    _trayMenu.Dispose();
                    _trayMenu = null;
                }

                if (_trayIconImage != null)
                {
                    _trayIconImage.Dispose();
                    _trayIconImage = null;
                }

                if (_appIcon != null)
                {
                    _appIcon.Dispose();
                    _appIcon = null;
                }
            };

            UpdateComputed();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Program.ShowExistingMessageId)
            {
                ShowFromTray();
                return;
            }

            base.WndProc(ref m);
        }

        private void InitializeTray()
        {
            _trayMenu = new ContextMenuStrip();
            _trayToggleItem = new ToolStripMenuItem("Open");
            _trayStartItem = new ToolStripMenuItem("Start");
            _trayStopItem = new ToolStripMenuItem("Stop");
            _trayAboutItem = new ToolStripMenuItem("Over...");
            var exitItem = new ToolStripMenuItem("Afsluiten");

            _trayToggleItem.Click += (s, e) => ToggleWindowVisibility();
            _trayStartItem.Click += (s, e) => Schedule();
            _trayStopItem.Click += (s, e) => Abort();
            _trayAboutItem.Click += (s, e) => ShowAbout();
            exitItem.Click += (s, e) =>
            {
                _allowExit = true;
                Close();
            };

            _trayMenu.Items.Add(_trayToggleItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayStartItem);
            _trayMenu.Items.Add(_trayStopItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayAboutItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(exitItem);

            _trayIconImage = _appIcon != null ? (Icon)_appIcon.Clone() : (Icon)SystemIcons.Application.Clone();
            _trayIcon = new NotifyIcon
            {
                Icon = _trayIconImage,
                Text = AppTitle,
                Visible = true,
                ContextMenuStrip = _trayMenu
            };
            _trayIcon.DoubleClick += (s, e) => ToggleWindowVisibility();

            UpdateTrayToggleText();
        }

        private void ToggleWindowVisibility()
        {
            if (Visible && WindowState != FormWindowState.Minimized)
            {
                HideToTray(false);
                return;
            }

            ShowFromTray();
        }

        private void ShowFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            UpdateTrayToggleText();
        }

        private void HideToTray(bool showBalloon)
        {
            Hide();
            ShowInTaskbar = false;
            UpdateTrayToggleText();

            if (showBalloon && _trayIcon != null)
            {
                try
                {
                    _trayIcon.ShowBalloonTip(2500, AppTitle, "Shutter draait in het systeemvak.", ToolTipIcon.Info);
                }
                catch { }
            }
        }

        private void UpdateTrayToggleText()
        {
            if (_trayToggleItem == null)
            {
                return;
            }

            _trayToggleItem.Text = Visible ? "Verberg" : "Open";
        }

        private DateTime GetTargetDateTime()
        {
            var date = _calendar.SelectionStart.Date;
            var time = _timePicker.Value.TimeOfDay;
            return date.Add(time);
        }

        private void UpdateComputed()
        {
            if (_countdownActive)
            {
                var remaining = (int)Math.Ceiling((_countdownTarget - DateTime.Now).TotalSeconds);
                if (remaining < 0)
                {
                    remaining = 0;
                }

                _targetLabel.Text = "Gepland: " + _countdownTarget.ToString("yyyy-MM-dd HH:mm:ss");
                _secondsLabel.Text = "Aftellen: " + FormatDuration(remaining) + " (" + remaining + " sec)";
                _secondsLabel.ForeColor = remaining <= 10 ? Color.DarkOrange : Color.Black;

                if (!string.IsNullOrWhiteSpace(_countdownCommand))
                {
                    _commandBox.Text = _countdownCommand;
                }

                _scheduleButton.Enabled = false;
                _calendar.Enabled = false;
                _timePicker.Enabled = false;
                _serverBox.Enabled = false;
                _rbShutdown.Enabled = false;
                _rbRestart.Enabled = false;
                _forceBox.Enabled = false;

                if (_trayStartItem != null) _trayStartItem.Enabled = false;
                if (_trayStopItem != null) _trayStopItem.Enabled = true;
                if (_trayIcon != null)
                {
                    var tt = AppTitle + " - " + remaining + "s";
                    if (tt.Length > 63) tt = tt.Substring(0, 63);
                    try { _trayIcon.Text = tt; } catch { }
                }
                return;
            }

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

            var canStart = seconds >= 0 && seconds <= MaxShutdownSeconds;
            _scheduleButton.Enabled = canStart;
            _calendar.Enabled = true;
            _timePicker.Enabled = true;
            _serverBox.Enabled = true;
            _rbShutdown.Enabled = true;
            _rbRestart.Enabled = true;
            _forceBox.Enabled = true;

            if (_trayStartItem != null) _trayStartItem.Enabled = canStart;
            if (_trayStopItem != null) _trayStopItem.Enabled = true;
            if (_trayIcon != null)
            {
                try { _trayIcon.Text = AppTitle; } catch { }
            }
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

            if (_countdownActive)
            {
                MessageBox.Show("Er loopt al een countdown. Klik op Stop om te annuleren.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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

                _countdownActive = true;
                _countdownTarget = DateTime.Now.AddSeconds(seconds);
                _countdownServer = NormalizeServer(_serverBox.Text);
                _countdownCommand = "shutdown.exe " + args;
                UpdateComputed();

                if (_trayIcon != null)
                {
                    try
                    {
                        _trayIcon.ShowBalloonTip(4000, AppTitle, "Shutdown/restart gepland. Countdown gestart.", ToolTipIcon.Info);
                    }
                    catch { }
                }
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
            var server = _countdownActive ? (_countdownServer ?? "") : NormalizeServer(_serverBox.Text);
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

                _countdownActive = false;
                _countdownTarget = DateTime.MinValue;
                _countdownServer = "";
                _countdownCommand = "";
                UpdateComputed();

                if (_trayIcon != null)
                {
                    try
                    {
                        _trayIcon.ShowBalloonTip(3000, AppTitle, "Shutdown/restart geannuleerd.", ToolTipIcon.Info);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "Status: fout bij annuleren.";
                _statusLabel.ForeColor = Color.DarkRed;
            }
        }

        private static string FormatDuration(int totalSeconds)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            var ts = TimeSpan.FromSeconds(totalSeconds);
            if (ts.TotalDays >= 1)
            {
                return string.Format("{0}d {1:00}:{2:00}:{3:00}", (int)ts.TotalDays, ts.Hours, ts.Minutes, ts.Seconds);
            }

            return string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
        }

        private static Icon LoadAppIcon()
        {
            try
            {
                // Try loading from app.ico file first (more reliable)
                var exePath = Application.ExecutablePath;
                var exeDir = System.IO.Path.GetDirectoryName(exePath);
                var iconPath = System.IO.Path.Combine(exeDir, "app.ico");
                
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
                
                // Fallback to extracting from executable
                var icon = Icon.ExtractAssociatedIcon(exePath);
                return icon;
            }
            catch
            {
                return null;
            }
        }

        private void ShowAbout()
        {
            using (var about = new AboutForm(_appIcon))
            {
                if (Visible && WindowState != FormWindowState.Minimized)
                {
                    about.ShowDialog(this);
                    return;
                }

                about.ShowDialog();
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

    internal sealed class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    internal sealed class AboutForm : Form
    {
        private readonly Icon _icon;
        private Image _iconImage;

        public AboutForm(Icon appIcon)
        {
            _icon = appIcon != null ? (Icon)appIcon.Clone() : (Icon)SystemIcons.Application.Clone();
            Icon = _icon;

            Text = "Over Shutter";
            Font = new Font("Segoe UI", 9f);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(640, 420);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            Controls.Add(root);

            var header = new DoubleBufferedPanel { Dock = DockStyle.Fill };
            header.Paint += Header_Paint;
            root.Controls.Add(header, 0, 0);

            try
            {
                _iconImage = _icon.ToBitmap();
            }
            catch { }

            var title = new Label
            {
                Text = "Shutter",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(118, 16)
            };
            var subtitle = new Label
            {
                Text = "Made by WAM-Sofware (c) since 1997.",
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(120, 56)
            };
            var versionLabel = new Label
            {
                Text = "Version: " + GetFileVersion(),
                ForeColor = Color.FromArgb(215, 255, 255, 255),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(120, 80)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(versionLabel);

            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 10),
                BackColor = Color.White
            };
            root.Controls.Add(bodyPanel, 0, 1);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            bodyPanel.Controls.Add(body);

            body.Controls.Add(CreateSection("Wat is het?"), 0, 0);
            body.Controls.Add(CreateParagraph("Plan een shutdown of restart met een kalender + tijd. Shutter berekent de /t seconden en toont een live aftelmechanisme."), 0, 1);

            body.Controls.Add(CreateSection("Highlights"), 0, 2);
            body.Controls.Add(CreateBullets(new[]
            {
                "Start/Stop knoppen",
                "Remote target via shutdown /m \\\\SERVER",
                "Systeemvak icoon (tray) met menu",
                "Live countdown (HH:mm:ss + seconden)"
            }), 0, 3);

            body.Controls.Add(CreateSection("Info"), 0, 4);
            body.Controls.Add(CreateInfoTable(), 0, 5);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Color.FromArgb(245, 245, 245),
                WrapContents = false
            };
            root.Controls.Add(buttons, 0, 2);

            var closeButton = new Button { Text = "Sluiten", Width = 110, Height = 28, Margin = new Padding(8, 0, 0, 0) };
            closeButton.Click += (s, e) => Close();
            AcceptButton = closeButton;

            var openButton = new Button { Text = "Open GitHub", Width = 110, Height = 28, Margin = new Padding(8, 0, 0, 0) };
            openButton.Click += (s, e) => OpenUrl("https://github.com/wmostert76/Shutter");

            var copyButton = new Button { Text = "Kopieer info", Width = 110, Height = 28, Margin = new Padding(8, 0, 0, 0) };
            copyButton.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(BuildCopyText());
                    MessageBox.Show("Info gekopieerd naar het klembord.", "Over Shutter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Over Shutter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            buttons.Controls.Add(closeButton);
            buttons.Controls.Add(openButton);
            buttons.Controls.Add(copyButton);

            FormClosed += (s, e) =>
            {
                if (_iconImage != null)
                {
                    _iconImage.Dispose();
                    _iconImage = null;
                }

                if (_icon != null)
                {
                    _icon.Dispose();
                }
            };
        }

        private void Header_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ((Panel)sender).ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            using (var brush = new LinearGradientBrush(rect, Color.Black, Color.Black, LinearGradientMode.ForwardDiagonal))
            {
                var blend = new ColorBlend();
                blend.Positions = new float[] { 0f, 0.55f, 1f };
                blend.Colors = new Color[]
                {
                    Color.FromArgb(255, 58, 170, 255),
                    Color.FromArgb(255, 30, 48, 92),
                    Color.FromArgb(255, 18, 22, 30)
                };
                brush.InterpolationColors = blend;
                g.FillRectangle(brush, rect);
            }

            if (_iconImage != null)
            {
                var iconRect = new Rectangle(24, 24, 72, 72);
                using (var shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    g.FillEllipse(shadow, iconRect.X + 2, iconRect.Y + 3, iconRect.Width, iconRect.Height);
                }

                g.DrawImage(_iconImage, iconRect);
            }

            using (var highlight = new Pen(Color.FromArgb(40, 255, 255, 255), 2f))
            {
                g.DrawLine(highlight, 0, rect.Height - 1, rect.Width, rect.Height - 1);
            }

            using (var accent = new Pen(Color.FromArgb(140, 82, 180, 255), 3f))
            {
                g.DrawLine(accent, 0, rect.Height - 2, rect.Width, rect.Height - 2);
            }

            using (var dotBrush = new SolidBrush(Color.FromArgb(28, 255, 255, 255)))
            {
                g.FillEllipse(dotBrush, rect.Width - 120, 14, 90, 90);
                g.FillEllipse(dotBrush, rect.Width - 78, 56, 40, 40);
            }
        }

        private static Control CreateSection(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 35, 35),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
        }

        private static Control CreateParagraph(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(55, 55, 55),
                AutoSize = true,
                MaximumSize = new Size(590, 0),
                Margin = new Padding(0, 0, 0, 10)
            };
        }

        private static Control CreateBullets(string[] bullets)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            foreach (var b in bullets)
            {
                panel.Controls.Add(new Label
                {
                    Text = "• " + b,
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(55, 55, 55),
                    AutoSize = true,
                    MaximumSize = new Size(590, 0),
                    Margin = new Padding(0, 2, 0, 2)
                });
            }

            return panel;
        }

        private Control CreateInfoTable()
        {
            var info = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 0,
                AutoSize = true
            };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddInfoRow(info, "Versie", new Label { Text = GetFileVersion(), AutoSize = true });

            var exe = new Label { Text = Application.ExecutablePath, AutoSize = true, MaximumSize = new Size(470, 0) };
            AddInfoRow(info, "Exe", exe);

            try
            {
                var ts = File.GetLastWriteTime(Application.ExecutablePath).ToString("yyyy-MM-dd HH:mm:ss");
                AddInfoRow(info, "Build", new Label { Text = ts, AutoSize = true });
            }
            catch { }

            AddInfoRow(info, "Windows", new Label { Text = Environment.OSVersion.ToString(), AutoSize = true, MaximumSize = new Size(470, 0) });
            AddInfoRow(info, ".NET", new Label { Text = Environment.Version.ToString(), AutoSize = true });

            var gh = new LinkLabel
            {
                Text = "wmostert76/Shutter",
                AutoSize = true,
                LinkColor = Color.FromArgb(33, 150, 243),
                ActiveLinkColor = Color.FromArgb(255, 87, 34),
                VisitedLinkColor = Color.FromArgb(106, 27, 154)
            };
            gh.Links.Add(0, gh.Text.Length, "https://github.com/wmostert76/Shutter");
            gh.LinkClicked += (s, e) => OpenUrl(e.Link.LinkData as string);
            AddInfoRow(info, "GitHub", gh);

            AddInfoRow(info, "License", new Label { Text = "MIT", AutoSize = true });

            return info;
        }

        private static void AddInfoRow(TableLayoutPanel table, string key, Control value)
        {
            var row = table.RowCount;
            table.RowCount = row + 1;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var keyLabel = new Label
            {
                Text = key + ":",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 45),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Margin = new Padding(0, 2, 10, 6)
            };

            value.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            value.Margin = new Padding(0, 2, 0, 6);

            table.Controls.Add(keyLabel, 0, row);
            table.Controls.Add(value, 1, row);
        }

        private static string GetFileVersion()
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
                if (!string.IsNullOrWhiteSpace(info.FileVersion))
                {
                    return info.FileVersion;
                }
            }
            catch { }

            try
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                if (v != null)
                {
                    return v.ToString();
                }
            }
            catch { }

            return "unknown";
        }

        private static string BuildCopyText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Shutter");
            sb.AppendLine("Version: " + GetFileVersion());
            sb.AppendLine("Exe: " + Application.ExecutablePath);

            try
            {
                sb.AppendLine("Exe modified: " + File.GetLastWriteTime(Application.ExecutablePath).ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch { }

            sb.AppendLine("GitHub: https://github.com/wmostert76/Shutter");
            sb.AppendLine("License: MIT");
            sb.AppendLine("Made by WAM-Sofware (c) since 1997.");
            return sb.ToString().Trim();
        }

        private static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Over Shutter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
