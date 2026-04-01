// Shutter.exe â€“ Schedule shutdown or restart.
// Made by WAM-Software (c) since 1997.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("Shutter")]
[assembly: AssemblyProduct("Shutter")]
[assembly: AssemblyCompany("WAM-Software")]
[assembly: AssemblyCopyright("Made by WAM-Software (c) since 1997.")]
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]

namespace Shutter
{
    // â”€â”€ Colour palette â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal static class Theme
    {
        public static readonly Color Bg        = Color.FromArgb(15,  17,  26);
        public static readonly Color Surface   = Color.FromArgb(24,  28,  42);
        public static readonly Color Card      = Color.FromArgb(32,  37,  56);
        public static readonly Color Border    = Color.FromArgb(48,  55,  80);
        public static readonly Color Accent    = Color.FromArgb(99,  179, 237);
        public static readonly Color Danger    = Color.FromArgb(239, 83,  80);
        public static readonly Color Success   = Color.FromArgb(102, 187, 106);
        public static readonly Color Warning   = Color.FromArgb(255, 183, 77);
        public static readonly Color TextPri   = Color.FromArgb(225, 232, 245);
        public static readonly Color TextSec   = Color.FromArgb(130, 145, 175);
        public static readonly Color TextMuted = Color.FromArgb(70,  85,  115);
        public static readonly Font  FontSm    = new Font("Segoe UI", 8f,  FontStyle.Regular);
        public static readonly Font  FontMd    = new Font("Segoe UI", 10f, FontStyle.Regular);
        public static readonly Font  FontLg    = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font  FontXl    = new Font("Segoe UI", 22f, FontStyle.Bold);
        public static readonly Font  FontTitle = new Font("Segoe UI", 10f, FontStyle.Bold);
    }

    // â”€â”€ P/Invoke â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal static class NativeMethods
    {
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int msg, int wp, int lp);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string msg);
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA data);
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION       = 0x2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct NOTIFYICONDATA
        {
            public int    cbSize;
            public IntPtr hWnd;
            public int    uID;
            public int    uFlags;
            public int    uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int    dwState;
            public int    dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int    uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int    dwInfoFlags;
        }
    }

    // â”€â”€ Rounded-corner helper â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal static class GfxHelper
    {
        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // â”€â”€ Flat custom button â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal class FlatBtn : Control
    {
        public enum Style { Primary, Danger, Ghost }
        private Style _style;
        private bool  _hover;
        private bool  _down;

        public Style BtnStyle { get { return _style; } set { _style = value; Invalidate(); } }

        public FlatBtn()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            Font   = Theme.FontMd;
            Size   = new Size(110, 36);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true;  Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)   { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color fill, text, border;
            if (_style == Style.Primary) {
                fill   = _down ? Color.FromArgb(60, 140, 200) : _hover ? Color.FromArgb(80, 160, 220) : Theme.Accent;
                text   = Color.White;
                border = Color.Transparent;
            } else if (_style == Style.Danger) {
                fill   = _down ? Color.FromArgb(180, 60, 60) : _hover ? Color.FromArgb(220, 90, 88) : Theme.Danger;
                text   = Color.White;
                border = Color.Transparent;
            } else {
                fill   = _down ? Color.FromArgb(40, 45, 65) : _hover ? Color.FromArgb(36, 42, 60) : Color.Transparent;
                text   = Theme.TextSec;
                border = Theme.Border;
            }

            using (var path = GfxHelper.RoundRect(new RectangleF(0, 0, Width - 1, Height - 1), 8))
            {
                if (fill != Color.Transparent)
                    using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                if (border != Color.Transparent)
                    using (var p = new Pen(border)) g.DrawPath(p, path);
            }

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using (var b = new SolidBrush(text))
                g.DrawString(Text, Font, b, new RectangleF(0, 0, Width, Height), sf);
        }
    }

    // â”€â”€ Pill toggle (Restart / Shutdown) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal class Toggle : Control
    {
        private bool _restart = true;
        public bool IsRestart { get { return _restart; } set { _restart = value; Invalidate(); } }

        public Toggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            Size   = new Size(240, 40);
            Font   = Theme.FontMd;
        }

        protected override void OnClick(EventArgs e)
        {
            _restart = !_restart;
            Invalidate();
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // track
            using (var path = GfxHelper.RoundRect(new RectangleF(0, 0, Width - 1, Height - 1), Height / 2))
            using (var b = new SolidBrush(Theme.Card))
                g.FillPath(b, path);
            using (var path = GfxHelper.RoundRect(new RectangleF(0, 0, Width - 1, Height - 1), Height / 2))
            using (var p = new Pen(Theme.Border))
                g.DrawPath(p, path);

            // active thumb
            float hw = Width / 2f;
            float thumbX = _restart ? 0 : hw;
            Color thumbCol = _restart ? Theme.Success : Theme.Danger;
            using (var path = GfxHelper.RoundRect(new RectangleF(thumbX + 2, 2, hw - 4, Height - 5), (Height - 5) / 2))
            using (var b = new SolidBrush(thumbCol))
                g.FillPath(b, path);

            // labels
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            Color lc = _restart ? Color.White : Theme.TextSec;
            Color rc = _restart ? Theme.TextSec : Color.White;
            using (var b = new SolidBrush(lc))
                g.DrawString("Restart", Font, b, new RectangleF(0, 0, hw, Height), sf);
            using (var b = new SolidBrush(rc))
                g.DrawString("Shutdown", Font, b, new RectangleF(hw, 0, hw, Height), sf);
        }
    }

    // â”€â”€ Countdown ring â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal class Ring : Control
    {
        private double _fraction; // 0..1
        private string _label   = "--:--:--";
        private Color  _color   = Theme.Accent;

        public double Fraction { get { return _fraction; } set { _fraction = value; Invalidate(); } }
        public string Label    { get { return _label; }    set { _label = value;    Invalidate(); } }
        public Color  RingColor { get { return _color; }   set { _color = value;    Invalidate(); } }

        public Ring()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(150, 150);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pad = 10, thick = 10;
            var rect = new Rectangle(pad, pad, Width - pad * 2, Height - pad * 2);

            // track ring
            using (var p = new Pen(Color.FromArgb(40, _color.R, _color.G, _color.B), thick))
                g.DrawArc(p, rect, -90, 360);

            // progress arc
            if (_fraction > 0)
            {
                float sweep = (float)(_fraction * 360.0);
                using (var p = new Pen(_color, thick) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(p, rect, -90, sweep);
            }

            // centre text
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using (var b = new SolidBrush(Theme.TextPri))
                g.DrawString(_label, new Font("Segoe UI", 11f, FontStyle.Bold), b,
                    new RectangleF(0, 0, Width, Height), sf);
        }
    }

    // â”€â”€ Dark card panel â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal class CardPanel : Panel
    {
        private string _heading = "";
        public string Heading { get { return _heading; } set { _heading = value; Invalidate(); } }

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Theme.Card;
            Padding   = new Padding(12, 36, 12, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var path = GfxHelper.RoundRect(new RectangleF(0, 0, Width - 1, Height - 1), 10))
            {
                using (var b = new SolidBrush(Theme.Card)) g.FillPath(b, path);
                using (var p = new Pen(Theme.Border)) g.DrawPath(p, path);
            }

            if (!string.IsNullOrEmpty(_heading))
            {
                var sf = new StringFormat { LineAlignment = StringAlignment.Center };
                using (var b = new SolidBrush(Theme.TextSec))
                    g.DrawString(_heading.ToUpperInvariant(), Theme.FontSm, b, new RectangleF(14, 6, Width - 28, 22), sf);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
    }

    // â”€â”€ Main form â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal class MainForm : Form
    {
        private const string VersionStr  = "v1.2";
        private const string AppName     = "Shutter";
        private const string SingleMutex = "Local\\WAMSoftware.Shutter.SingleInstance";
        private const string ShowMsg     = "WAMSoftware.Shutter.ShowExisting";

        private static readonly int ShowMsgId = NativeMethods.RegisterWindowMessage(ShowMsg);

        private System.Windows.Forms.Timer _countdown;
        private DateTime  _targetDt  = DateTime.MinValue;
        private bool      _scheduled = false;

        // controls
        private MonthCalendar _cal;
        private DateTimePicker _timePicker;
        private Toggle     _toggle;
        private Ring       _ring;
        private FlatBtn    _btnSchedule;
        private FlatBtn    _btnCancel;
        private Label      _lblStatus;
        private NotifyIcon _tray;
        private ContextMenuStrip _trayMenu;

        public MainForm()
        {
            SuspendLayout();
            Text            = AppName + " " + VersionStr;
            Size            = new Size(520, 580);
            MinimumSize     = new Size(520, 580);
            MaximumSize     = new Size(520, 580);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor       = Theme.Bg;
            ForeColor       = Theme.TextPri;
            Font            = Theme.FontMd;
            DoubleBuffered  = true;

            BuildUI();
            BuildTray();

            _countdown = new System.Windows.Forms.Timer { Interval = 1000 };
            _countdown.Tick += OnTick;

            ResumeLayout(true);
        }

        private void BuildUI()
        {
            // â”€â”€ Title bar â”€â”€
            var title = new Panel {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Theme.Surface
            };
            title.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
                }
            };

            var lblTitle = new Label {
                Text      = AppName + " " + VersionStr,
                Font      = Theme.FontTitle,
                ForeColor = Theme.TextPri,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Bounds    = new Rectangle(16, 0, 260, 44),
                TextAlign = ContentAlignment.MiddleLeft
            };
            title.Controls.Add(lblTitle);

            var btnClose = MakeTitleBtn("âœ•", 476, Theme.Danger);
            btnClose.Click += (s, e) => { _tray.Visible = true; Hide(); };
            title.Controls.Add(btnClose);

            var btnMin = MakeTitleBtn("â”€", 448, Theme.TextMuted);
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            title.Controls.Add(btnMin);

            Controls.Add(title);

            // â”€â”€ Body â”€â”€
            var body = new Panel {
                Location  = new Point(0, 44),
                Size      = new Size(520, 536),
                BackColor = Theme.Bg
            };
            Controls.Add(body);

            // Left col: calendar + ring
            var calCard = new CardPanel { Heading = "Select Date", Bounds = new Rectangle(14, 10, 246, 246) };
            _cal = new MonthCalendar {
                MaxSelectionCount = 1,
                MinDate           = DateTime.Today,
                BackColor         = Theme.Card,
                ForeColor         = Theme.TextPri,
                TitleBackColor    = Theme.Surface,
                TitleForeColor    = Theme.TextPri,
                TrailingForeColor = Theme.TextMuted,
                Location          = new Point(6, 28),
                Font              = Theme.FontSm
            };
            calCard.Controls.Add(_cal);
            body.Controls.Add(calCard);

            var ringCard = new CardPanel { Heading = "Time Remaining", Bounds = new Rectangle(14, 268, 246, 180) };
            _ring = new Ring {
                Location = new Point((246 - 150) / 2 - 6, 28),
                Size     = new Size(150, 140)
            };
            ringCard.Controls.Add(_ring);
            body.Controls.Add(ringCard);

            // Right col
            int rx = 272, ry = 10;

            // Action toggle
            var actCard = new CardPanel { Heading = "Action", Bounds = new Rectangle(rx, ry, 234, 80) };
            _toggle = new Toggle { Location = new Point((234 - 240) / 2 + 117 - 120, 30), IsRestart = true };
            actCard.Controls.Add(_toggle);
            body.Controls.Add(actCard);
            ry += 90;

            // Time picker
            var timeCard = new CardPanel { Heading = "Time", Bounds = new Rectangle(rx, ry, 234, 70) };
            _timePicker = new DateTimePicker {
                Format         = DateTimePickerFormat.Custom,
                CustomFormat   = "HH:mm",
                ShowUpDown     = true,
                BackColor      = Theme.Card,
                ForeColor      = Theme.TextPri,
                Font           = Theme.FontLg,
                Location       = new Point(8, 28),
                Size           = new Size(218, 32)
            };
            timeCard.Controls.Add(_timePicker);
            body.Controls.Add(timeCard);
            ry += 80;

            // Status
            var statusCard = new CardPanel { Heading = "Status", Bounds = new Rectangle(rx, ry, 234, 70) };
            _lblStatus = new Label {
                Text      = "Not scheduled",
                ForeColor = Theme.TextSec,
                BackColor = Color.Transparent,
                Font      = Theme.FontSm,
                AutoSize  = false,
                Bounds    = new Rectangle(8, 28, 218, 32),
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusCard.Controls.Add(_lblStatus);
            body.Controls.Add(statusCard);
            ry += 80;

            // Buttons
            _btnSchedule = new FlatBtn {
                Text     = "Schedule",
                BtnStyle = FlatBtn.Style.Primary,
                Bounds   = new Rectangle(rx, ry, 112, 38),
                Font     = Theme.FontMd
            };
            _btnSchedule.Click += OnSchedule;
            body.Controls.Add(_btnSchedule);

            _btnCancel = new FlatBtn {
                Text     = "Cancel",
                BtnStyle = FlatBtn.Style.Danger,
                Bounds   = new Rectangle(rx + 120, ry, 112, 38),
                Font     = Theme.FontMd,
                Enabled  = false
            };
            _btnCancel.Click += OnCancel;
            body.Controls.Add(_btnCancel);
            ry += 48;

            // About / version row
            var lblVer = new Label {
                Text      = "WAM-Software  " + VersionStr,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Font      = Theme.FontSm,
                AutoSize  = false,
                Bounds    = new Rectangle(rx, ry + 4, 234, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            body.Controls.Add(lblVer);
        }

        private Label MakeTitleBtn(string text, int x, Color hoverColor)
        {
            var b = new Label {
                Text      = text,
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                Bounds    = new Rectangle(x, 0, 32, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor    = Cursors.Hand
            };
            b.MouseEnter += (s, e) => { b.ForeColor = hoverColor; };
            b.MouseLeave += (s, e) => { b.ForeColor = Theme.TextMuted; };
            return b;
        }

        private void BuildTray()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Show", null, (s, e) => ShowForm());
            _trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            _tray = new NotifyIcon {
                Text    = AppName,
                Visible = false,
                ContextMenuStrip = _trayMenu
            };
            _tray.DoubleClick += (s, e) => ShowForm();

            Icon ico = LoadAppIcon();
            if (ico != null) { _tray.Icon = ico; Icon = ico; }
        }

        private Icon LoadAppIcon()
        {
            try {
                string dir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
                string path = Path.Combine(dir, "app.ico");
                if (File.Exists(path)) return new Icon(path);
                Icon assoc = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (assoc != null) return assoc;
            } catch { }
            return SystemIcons.Application;
        }

        private void ShowForm()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            _tray.Visible = false;
        }

        private void OnSchedule(object s, EventArgs e)
        {
            var d = _cal.SelectionStart.Date;
            var t = _timePicker.Value.TimeOfDay;
            _targetDt = d + t;

            if (_targetDt <= DateTime.Now) {
                MessageBox.Show("Please select a future date and time.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _scheduled = true;
            _btnSchedule.Enabled = false;
            _btnCancel.Enabled   = true;
            _ring.RingColor = _toggle.IsRestart ? Theme.Success : Theme.Danger;
            UpdateRing();
            _countdown.Start();
            _lblStatus.Text      = (_toggle.IsRestart ? "Restart" : "Shutdown") + " at " + _targetDt.ToString("MMM d, HH:mm");
            _lblStatus.ForeColor = _toggle.IsRestart ? Theme.Success : Theme.Danger;
        }

        private void OnCancel(object s, EventArgs e)
        {
            _countdown.Stop();
            _scheduled = false;
            _btnSchedule.Enabled = true;
            _btnCancel.Enabled   = false;
            _ring.Fraction       = 0;
            _ring.Label          = "--:--:--";
            _ring.RingColor      = Theme.Accent;
            _lblStatus.Text      = "Cancelled";
            _lblStatus.ForeColor = Theme.TextSec;
        }

        private void OnTick(object s, EventArgs e)
        {
            if (!_scheduled) return;
            UpdateRing();
            if (DateTime.Now >= _targetDt) {
                _countdown.Stop();
                Execute();
            }
        }

        private void UpdateRing()
        {
            TimeSpan remaining = _targetDt - DateTime.Now;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            TimeSpan total = _targetDt - _cal.SelectionStart.Date.Date;
            if (total <= TimeSpan.Zero) total = TimeSpan.FromSeconds(1);

            double fraction = 1.0 - (remaining.TotalSeconds / total.TotalSeconds);
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;

            _ring.Fraction = fraction;
            _ring.Label    = remaining.Hours.ToString("D2") + ":" +
                             remaining.Minutes.ToString("D2") + ":" +
                             remaining.Seconds.ToString("D2");
        }

        private void Execute()
        {
            string args = _toggle.IsRestart ? "/r /t 10" : "/s /t 10";
            try {
                Process.Start("shutdown.exe", args);
            } catch (Exception ex) {
                MessageBox.Show("Could not execute: " + ex.Message, AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Application.Exit();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == ShowMsgId) ShowForm();
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                _tray.Visible = true;
                Hide();
            } else {
                _tray.Dispose();
                base.OnFormClosing(e);
            }
        }
    }

    // â”€â”€ Entry point â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created;
            var mutex = new System.Threading.Mutex(true, "Local\\WAMSoftware.Shutter.SingleInstance", out created);
            if (!created) {
                int id = NativeMethods.RegisterWindowMessage("WAMSoftware.Shutter.ShowExisting");
                NativeMethods.SendMessage((IntPtr)0xFFFF, id, 0, 0);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            mutex.ReleaseMutex();
        }
    }
}
