using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ArkAI
{
    public partial class ProfileForm : Form
    {
        private Label lblUser, lblEmail, lblJoinDate, lblKm, lblGeri;
        private TextBox txtPlaka, txtYeniSifre, txtDogrulamaKodu;
        private Button btnGuncelle, btnKodGonder, btnSifreOnayla;
        private PictureBox pbAvatar;
        private Panel pnlKart, pnlBilgiGrubu, pnlSifreGrubu;
        private string kullaniciEmail = "";

        public ProfileForm()
        {
            this.Size = new Size(1100, 750);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 15, 18);
            this.DoubleBuffered = true;

            ModernArayuzKur();
            this.Shown += (s, e) => VerileriYukle();
        }

        private void ModernArayuzKur()
        {
            // 1. Geri Dönüş
            lblGeri = new Label()
            {
                Text = "← ANA MENÜYE DÖN",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(40, 30),
                Cursor = Cursors.Hand,
                AutoSize = true
            };
            lblGeri.Click += (s, e) => { new MainForm().Show(); this.Close(); };
            lblGeri.MouseEnter += (s, e) => lblGeri.ForeColor = Color.White;

            // 2. Ana Gövde (Cam Efekti Paneli)
            pnlKart = new Panel()
            {
                Size = new Size(1000, 600),
                Location = new Point(50, 80),
                BackColor = Color.FromArgb(25, 25, 30)
            };

            // 3. Profil Üst Bilgi
            pbAvatar = new PictureBox() { Size = new Size(120, 120), Location = new Point(40, 40), BackColor = Color.FromArgb(40, 40, 50), SizeMode = PictureBoxSizeMode.StretchImage };
            YuvarlakYap(pbAvatar);

            lblUser = new Label() { Text = "KULLANICI ADI", ForeColor = Color.White, Font = new Font("Segoe UI", 24, FontStyle.Bold), Location = new Point(180, 50), AutoSize = true };
            lblEmail = CreateEtiket("E-POSTA: Yükleniyor...", 185, 100, 10);
            lblJoinDate = CreateEtiket("KAYIT TARİHİ: ...", 185, 125, 10);

            // Büyük Kilometre Göstergesi
            lblKm = new Label()
            {
                Text = "0 KM",
                ForeColor = Color.FromArgb(0, 255, 150),
                Font = new Font("Consolas", 32, FontStyle.Bold),
                Location = new Point(600, 50),
                Size = new Size(350, 60),
                TextAlign = ContentAlignment.MiddleRight
            };

            Panel ayraç = new Panel() { Size = new Size(920, 1), Location = new Point(40, 190), BackColor = Color.FromArgb(45, 45, 50) };

            // 4. İçerik Alanı (İki Sütun)
            // SOL: Araç Bilgileri
            Label lblBaslik1 = CreateEtiket("ARAÇ YÖNETİMİ", 40, 220, 9, true);
            txtPlaka = ModernTextBox(40, 250, 300, "PLAKA GİRİNİZ");

            btnGuncelle = ModernButon("PLAKAYI GÜNCELLE", 40, 310, 300, 45, Color.FromArgb(0, 120, 215));
            btnGuncelle.Click += btnGuncelle_Click;

            // SAĞ: Güvenlik Paneli
            pnlSifreGrubu = new Panel() { Size = new Size(450, 350), Location = new Point(500, 220) };
            Label lblBaslik2 = CreateEtiket("GÜVENLİK VE ŞİFRE", 0, 0, 9, true);

            btnKodGonder = ModernButon("DOĞRULAMA KODU GÖNDER", 0, 30, 400, 45, Color.FromArgb(40, 40, 45));
            btnKodGonder.Click += btnKodGonder_Click;

            txtDogrulamaKodu = ModernTextBox(0, 95, 190, "6 HANELİ KOD");
            txtYeniSifre = ModernTextBox(210, 95, 190, "YENİ ŞİFRE");
            txtYeniSifre.PasswordChar = '●';
            txtDogrulamaKodu.Visible = txtYeniSifre.Visible = false;

            btnSifreOnayla = ModernButon("KODU ONAYLA VE ŞİFREYİ DEĞİŞTİR", 0, 160, 400, 45, Color.FromArgb(0, 160, 100));
            btnSifreOnayla.Visible = false;
            btnSifreOnayla.Click += btnSifreOnayla_Click;

            pnlSifreGrubu.Controls.AddRange(new Control[] { lblBaslik2, btnKodGonder, txtDogrulamaKodu, txtYeniSifre, btnSifreOnayla });

            pnlKart.Controls.AddRange(new Control[] { pbAvatar, lblUser, lblEmail, lblJoinDate, lblKm, ayraç, lblBaslik1, txtPlaka, btnGuncelle, pnlSifreGrubu });
            this.Controls.AddRange(new Control[] { lblGeri, pnlKart });
        }

        // --- YARDIMCI METOTLAR (TASARIM) ---
        private Label CreateEtiket(string metin, int x, int y, int boyut, bool kalin = false) =>
            new Label() { Text = metin, ForeColor = Color.FromArgb(170, 170, 180), Font = new Font("Segoe UI", boyut, kalin ? FontStyle.Bold : FontStyle.Regular), Location = new Point(x, y), AutoSize = true };

        private TextBox ModernTextBox(int x, int y, int w, string placeholder) =>
            new TextBox() { Location = new Point(x, y), Size = new Size(w, 40), BackColor = Color.FromArgb(35, 35, 40), ForeColor = Color.White, Font = new Font("Segoe UI", 12), BorderStyle = BorderStyle.None, PlaceholderText = placeholder, TextAlign = HorizontalAlignment.Center };

        private Button ModernButon(string metin, int x, int y, int w, int h, Color renk)
        {
            Button b = new Button() { Text = metin, Location = new Point(x, y), Size = new Size(w, h), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand, BackColor = renk };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void YuvarlakYap(Control c)
        {
            GraphicsPath gp = new GraphicsPath(); gp.AddEllipse(0, 0, c.Width, c.Height); c.Region = new Region(gp);
        }

        // --- MANTIK VE VERİTABANI ---
        private void btnKodGonder_Click(object sender, EventArgs e)
        {
            string kod = new Random().Next(100000, 999999).ToString();
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE users SET verification_code = @c WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@c", kod);
                    cmd.Parameters.AddWithValue("@id", DataManager.CurrentUserId);
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        EmailService.SendVerificationCode(kullaniciEmail, kod);
                        ArkMessage.Show("Güvenlik kodu mail adresinize gönderildi.");
                        txtDogrulamaKodu.Visible = txtYeniSifre.Visible = btnSifreOnayla.Visible = true;
                    }
                }
            }
            catch (Exception ex) { ArkMessage.Show("Hata: " + ex.Message); }
        }

        private void btnSifreOnayla_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string kontrolSql = "SELECT COUNT(*) FROM users WHERE id = @id AND verification_code = @c";
                    MySqlCommand cmdCheck = new MySqlCommand(kontrolSql, conn);
                    cmdCheck.Parameters.AddWithValue("@id", DataManager.CurrentUserId);
                    cmdCheck.Parameters.AddWithValue("@c", txtDogrulamaKodu.Text.Trim());

                    if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                    {
                        string guncelleSql = "UPDATE users SET password = @p, verification_code = NULL WHERE id = @id";
                        MySqlCommand cmdUp = new MySqlCommand(guncelleSql, conn);
                        cmdUp.Parameters.AddWithValue("@p", txtYeniSifre.Text);
                        cmdUp.Parameters.AddWithValue("@id", DataManager.CurrentUserId);
                        if (cmdUp.ExecuteNonQuery() > 0) ArkMessage.Show("Şifreniz başarıyla güncellendi.");
                    }
                    else { ArkMessage.Show("Hatalı doğrulama kodu!"); }
                }
            }
            catch (Exception ex) { ArkMessage.Show("Sistem hatası: " + ex.Message); }
        }

        private void VerileriYukle()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT u.email, u.created_at, v.license_plate, v.current_km 
                                 FROM users u INNER JOIN vehicles v ON v.id = (SELECT MAX(id) FROM vehicles) WHERE u.id = @id";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", DataManager.CurrentUserId);
                    using (MySqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            kullaniciEmail = r["email"].ToString();
                            lblUser.Text = DataManager.CurrentUsername;
                            lblEmail.Text = "E-POSTA: " + kullaniciEmail;
                            lblJoinDate.Text = "KAYIT: " + Convert.ToDateTime(r["created_at"]).ToString("dd.MM.yyyy");
                            txtPlaka.Text = r["license_plate"].ToString();
                            lblKm.Text = (r["current_km"] ?? "0") + " KM";
                        }
                    }
                }
            }
            catch { }
        }

        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE vehicles SET license_plate = @p WHERE id = (SELECT vid FROM (SELECT MAX(id) as vid FROM vehicles) as t)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p", txtPlaka.Text.ToUpper());
                    if (cmd.ExecuteNonQuery() > 0) ArkMessage.Show("Plaka güncellendi.");
                }
            }
            catch (Exception ex) { ArkMessage.Show(ex.Message); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Pen p = new Pen(Color.FromArgb(50, 255, 255, 255), 1))
                e.Graphics.DrawRectangle(p, pnlKart.Bounds);
        }
    }
}