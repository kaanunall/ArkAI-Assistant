using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ArkAI
{
    public partial class LoginForm : Form
    {
        private TextBox txtUser, txtPass;
        private Button btnLogin;
        private LinkLabel lnkForgot, lnkRegister;
        private Label lblClose;

        public LoginForm()
        {
            try { InitializeComponent(); } catch { }
            SetupDesign();

            // --- ARAÇ TANIMA / OTO GİRİŞ KONTROLÜ ---
            // Form yüklenirken araç kayıtlı mı bakıyoruz
            this.Load += (s, e) => {
                if (DataManager.AutoLogin())
                {
                    // Araç tanındıysa MainForm'u aç ve burayı gizle/kapat
                    MainForm main = new MainForm();
                    main.Show();

                    // Form daha yeni yüklendiği için direkt Close() bazen sorun çıkarır
                    // Bu yüzden gizleyip kapatıyoruz
                    this.Hide();
                    this.BeginInvoke(new MethodInvoker(delegate { this.Hide(); }));
                }
            };
        }

        private void SetupDesign()
        {
            this.Text = "Ark AI - Giriş";
            this.Size = new Size(450, 600);
            this.BackColor = Color.FromArgb(32, 33, 36);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            // X Kapatma Butonu
            lblClose = new Label() { Text = "X", ForeColor = Color.White, Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(410, 15), Cursor = Cursors.Hand, AutoSize = true };
            lblClose.Click += (s, e) => Application.Exit();

            Label lblTitle = new Label() { Text = "ARK AI GİRİŞ", ForeColor = Color.White, Font = new Font("Segoe UI", 24, FontStyle.Bold), Location = new Point(75, 60), AutoSize = true };
            Label lblSub = new Label() { Text = "Yapay zeka asistanınıza erişmek için giriş yapın.", ForeColor = Color.Gray, Font = new Font("Segoe UI", 10), Location = new Point(75, 110), AutoSize = true };

            txtUser = CreateStyledTextBox("Kullanıcı Adı", 180, false);
            txtPass = CreateStyledTextBox("Şifre", 260, true);

            btnLogin = new Button()
            {
                Text = "SİSTEME GİRİŞ YAP",
                Size = new Size(300, 55),
                Location = new Point(75, 350),
                BackColor = Color.FromArgb(66, 133, 244),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            lnkForgot = new LinkLabel() { Text = "Şifremi Unuttum?", Location = new Point(75, 430), LinkColor = Color.FromArgb(219, 68, 55), ActiveLinkColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10) };
            lnkForgot.LinkClicked += (s, e) => { new PasswordReset().Show(); this.Hide(); };

            lnkRegister = new LinkLabel() { Text = "Yeni hesap oluştur", Location = new Point(230, 430), LinkColor = Color.DeepSkyBlue, ActiveLinkColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lnkRegister.LinkClicked += (s, e) => { new RegisterForm().Show(); this.Hide(); };

            this.Controls.AddRange(new Control[] { lblClose, lblTitle, lblSub, txtUser, txtPass, btnLogin, lnkForgot, lnkRegister });
        }

        private TextBox CreateStyledTextBox(string placeholder, int y, bool isPassword)
        {
            TextBox tb = new TextBox() { Size = new Size(300, 40), Location = new Point(75, y), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12), Text = placeholder };
            tb.Enter += (s, e) => { if (tb.Text == placeholder) { tb.Text = ""; tb.ForeColor = Color.White; if (isPassword) tb.PasswordChar = '●'; } };
            tb.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = placeholder; tb.ForeColor = Color.Gray; if (isPassword) tb.PasswordChar = '\0'; } };
            return tb;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (txtUser.Text == "Kullanıcı Adı" || txtPass.Text == "Şifre")
            {
                ArkMessage.Show("Lütfen bilgilerinizi eksiksiz girin!");
                return;
            }

            using (var conn = Database.GetConnection())
            {
                try
                {
                    conn.Open();
                    // ID bilgisini de çekiyoruz
                    string sql = "SELECT id, is_verified FROM users WHERE username=@u AND password=@p";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@u", txtUser.Text);
                    cmd.Parameters.AddWithValue("@p", txtPass.Text);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32("id");
                            int isVerified = reader.GetInt32("is_verified");

                            if (isVerified == 1)
                            {
                                // Global verileri setle
                                DataManager.CurrentUserId = userId;
                                DataManager.CurrentUsername = txtUser.Text;
                                reader.Close();

                                // ARAÇ KAYDI (RegisterVehicle)
                                DataManager.RegisterCurrentVehicle(userId);

                                // MAINFORM'A GEÇİŞ
                                MainForm main = new MainForm();
                                main.Show();

                                // Login formunu sakla veya kapat
                                this.Hide();
                            }
                            else
                            {
                                ArkMessage.Show("Hesabınız henüz onaylanmamış!");
                                new VerificationForm(txtUser.Text).Show();
                                this.Hide();
                            }
                        }
                        else
                        {
                            ArkMessage.Show("Kullanıcı adı veya şifre hatalı!");
                        }
                    }
                }
                catch (Exception ex) { ArkMessage.Show("Sistem Hatası: " + ex.Message); }
            }
        }
    }
}