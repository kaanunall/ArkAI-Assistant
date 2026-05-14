#nullable disable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ArkAI
{
    public partial class CarSettings : Form
    {
        private MainForm _mainForm;

        // Sürükleme değişkenleri
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        // Kontroller
        private Panel pnlHeader, pnlBody;
        private Label lblHizLimitValue;
        private TrackBar trackHizSınırıHUD;

        // Yeni Modern Anahtarlarımız
        private ModernToggle tglSimulationHUD;
        private ModernToggle tglAutoBrake;
        private ModernToggle tglStartStop;
        private ModernToggle tglWelcomeLight;
        private ModernToggle tglFollowMeHome;

        public CarSettings(MainForm mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;

            // Formun yepyeni devasa ve fütüristik ayarları
            this.Size = new Size(850, 650);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(10, 11, 20); // Derin uzay siyahı
            this.ForeColor = Color.White;

            // Etrafına şık bir mavi ince çerçeve çizmek için
            this.Paint += (s, e) => {
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(0, 242, 255), 1), 0, 0, this.Width - 1, this.Height - 1);
            };

            SetupPremiumUI();
        }

        private void SetupPremiumUI()
        {
            // ==========================================
            // HEADER (SÜRÜKLENEBİLİR BAŞLIK)
            // ==========================================
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(15, 16, 28) };
            pnlHeader.MouseDown += Header_MouseDown;
            pnlHeader.MouseMove += Header_MouseMove;
            pnlHeader.MouseUp += Header_MouseUp;

            Label lblHeaderTitle = new Label { Text = "ARK AI • ARAÇ YÖNETİM SİSTEMİ", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(0, 242, 255), Location = new Point(30, 20), AutoSize = true };
            lblHeaderTitle.MouseDown += Header_MouseDown;
            lblHeaderTitle.MouseMove += Header_MouseMove;
            lblHeaderTitle.MouseUp += Header_MouseUp;

            Button btnClose = new Button { Text = "✕", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(0, 242, 255), FlatStyle = FlatStyle.Flat, Size = new Size(50, 50), Location = new Point(780, 10), BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 77, 77);
            btnClose.Click += (s, e) => { this.Close(); };

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(btnClose);
            this.Controls.Add(pnlHeader);

            // ==========================================
            // BODY (ANA AYARLAR BÖLÜMÜ)
            // ==========================================
            pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30), AutoScroll = true };
            this.Controls.Add(pnlBody);

            // 1. KATEGORİ: SİMÜLASYON
            AddCategoryTitle("SÜRÜŞ GÖSTERGESİ (HUD) & DEMO", 10);
            tglSimulationHUD = new ModernToggle { Checked = false };
            tglSimulationHUD.CheckedChanged += (s, e) => {
                if (tglSimulationHUD.Checked) _mainForm.RunJavaScript("startVehicleSimulation();");
                else _mainForm.RunJavaScript("stopVehicleSimulation();");
            };
            AddSettingRow("Canlı Sürüş Verilerini Simüle Et", "Ana ekranda HUD panelini açar ve sensör verilerini canlı simüle eder.", tglSimulationHUD, 50);

            // 2. KATEGORİ: HIZ LİMİTİ (HUD)
            AddCategoryTitle("SÜRÜŞ GÜVENLİĞİ VE LİMİTLER", 150);

            Label lblHizTitle = new Label { Text = "HUD Hız Uyarı Limiti", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(40, 190), AutoSize = true };
            lblHizLimitValue = new Label { Text = "120 km/h", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(0, 242, 255), Location = new Point(710, 185), Size = new Size(100, 30), TextAlign = ContentAlignment.MiddleRight };

            trackHizSınırıHUD = new TrackBar { Minimum = 50, Maximum = 200, Value = 120, TickFrequency = 10, Location = new Point(35, 230), Size = new Size(780, 45), LargeChange = 10 };
            trackHizSınırıHUD.Scroll += (s, e) => {
                lblHizLimitValue.Text = $"{trackHizSınırıHUD.Value} km/h";
                _mainForm.RunJavaScript($"window.speedLimit = {trackHizSınırıHUD.Value};");
            };

            pnlBody.Controls.Add(lblHizTitle);
            pnlBody.Controls.Add(lblHizLimitValue);
            pnlBody.Controls.Add(trackHizSınırıHUD);

            // 3. KATEGORİ: OTONOM SİSTEMLER
            AddCategoryTitle("AKILLI SÜRÜŞ ASİSTANLARI", 300);
            tglAutoBrake = new ModernToggle { Checked = true };
            AddSettingRow("Otonom Acil Fren (AEB)", "Çarpışma riski algılandığında aracı otomatik olarak durdurur.", tglAutoBrake, 340);

            tglStartStop = new ModernToggle { Checked = true };
            AddSettingRow("Akıllı Start-Stop", "Trafikte beklerken motoru durdurarak yakıt tasarrufu sağlar.", tglStartStop, 420);

            // 4. KATEGORİ: AYDINLATMA SİSTEMLERİ
            AddCategoryTitle("GELİŞMİŞ DIŞ AYDINLATMA", 500);
            tglWelcomeLight = new ModernToggle { Checked = true };
            AddSettingRow("Karşılama Aydınlatması (Welcome Light)", "Araca yaklaşırken matrix LED'ler ile görsel animasyon oynatır.", tglWelcomeLight, 540);

            tglFollowMeHome = new ModernToggle { Checked = true };
            AddSettingRow("Refakat Aydınlatması (Follow-Me-Home)", "Karanlıkta araçtan indikten sonra farları belirli bir süre açık tutar.", tglFollowMeHome, 620);
        }

        // ==========================================
        // YARDIMCI TASARIM METODLARI
        // ==========================================

        private void AddCategoryTitle(string title, int yPos)
        {
            Label lbl = new Label();
            lbl.Text = title;
            lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(100, 150, 200);
            lbl.Location = new Point(20, yPos);
            lbl.AutoSize = true;
            pnlBody.Controls.Add(lbl);
        }

        private void AddSettingRow(string title, string description, ModernToggle toggle, int yPos)
        {
            Panel row = new Panel { Location = new Point(20, yPos), Size = new Size(790, 70), BackColor = Color.FromArgb(18, 19, 30) };

            // Satırın alt kısmına çok ince bir çizgi çizelim
            row.Paint += (s, e) => { e.Graphics.DrawLine(new Pen(Color.FromArgb(30, 30, 45)), 10, row.Height - 1, row.Width - 10, row.Height - 1); };

            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 15), AutoSize = true };
            Label lblDesc = new Label { Text = description, Font = new Font("Segoe UI", 9, FontStyle.Regular), ForeColor = Color.Gray, Location = new Point(20, 40), Size = new Size(600, 20) };

            toggle.Location = new Point(710, 20); // Toggle'ı en sağa hizala

            row.Controls.Add(lblTitle);
            row.Controls.Add(lblDesc);
            row.Controls.Add(toggle);
            pnlBody.Controls.Add(row);
        }

        // --- SÜRÜKLEME MANTIĞI ---
        private void Header_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragging = true; dragCursorPoint = Cursor.Position; dragFormPoint = this.Location; } }
        private void Header_MouseMove(object sender, MouseEventArgs e) { if (dragging) { Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint)); this.Location = Point.Add(dragFormPoint, new Size(dif)); } }
        private void Header_MouseUp(object sender, MouseEventArgs e) { dragging = false; }
    }

    // =========================================================================
    // YENİ EKLENTİ: TAMAMEN ÖZEL ÇİZİM MODERN "TOGGLE SWITCH" (iOS/Tesla Tarzı)
    // =========================================================================
    public class ModernToggle : CheckBox
    {
        public ModernToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            AutoSize = false;
            Size = new Size(60, 30); // Anahtarın boyutu
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent.BackColor);

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int thumbSize = Height - 6;

            if (Checked)
            {
                // AÇIK DURUM (Neon Camgöbeği)
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 242, 255)))
                using (GraphicsPath path = GetFigurePath(rect, Height))
                {
                    e.Graphics.FillPath(brush, path);
                }
                // Beyaz Top (Sağda)
                using (SolidBrush thumb = new SolidBrush(Color.White))
                {
                    e.Graphics.FillEllipse(thumb, Width - thumbSize - 3, 3, thumbSize, thumbSize);
                }
            }
            else
            {
                // KAPALI DURUM (Koyu Gri)
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, 50, 60)))
                using (GraphicsPath path = GetFigurePath(rect, Height))
                {
                    e.Graphics.FillPath(brush, path);
                }
                // Açık Gri Top (Solda)
                using (SolidBrush thumb = new SolidBrush(Color.Silver))
                {
                    e.Graphics.FillEllipse(thumb, 3, 3, thumbSize, thumbSize);
                }
            }
        }

        private GraphicsPath GetFigurePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}