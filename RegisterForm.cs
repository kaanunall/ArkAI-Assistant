using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ArkAI
{
    public partial class RegisterForm : Form
    {
        private TextBox txtUser, txtEmail, txtPass;
        private Button btnRegister;
        private LinkLabel lnkBackToLogin;
        private Label lblClose;

        public RegisterForm()
        {
            try { InitializeComponent(); } catch { }
            SetupDesign();
        }

        private void SetupDesign()
        {
            this.Text = "Ark AI - Kayıt Ol";
            // İsteğin üzerine boyutu büyüttük (450x600 daha ferah oldu)
            this.Size = new Size(450, 600);
            this.BackColor = Color.FromArgb(32, 33, 36);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            // X Kapatma Butonu
            lblClose = new Label() { Text = "X", ForeColor = Color.White, Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(410, 15), Cursor = Cursors.Hand, AutoSize = true };
            lblClose.Click += (s, e) => Application.Exit();

            Label lblTitle = new Label() { Text = "HESAP OLUŞTUR", ForeColor = Color.White, Font = new Font("Segoe UI", 22, FontStyle.Bold), Location = new Point(75, 50), AutoSize = true };

            Label lblSub = new Label() { Text = "Ark AI dünyasına katılmak için bilgileri doldurun.", ForeColor = Color.Gray, Font = new Font("Segoe UI", 9), Location = new Point(75, 95), AutoSize = true };

            // Kutuları dikeyde biraz daha açtık
            txtUser = CreateStyledTextBox("Kullanıcı Adı", 150, false);
            txtEmail = CreateStyledTextBox("E-Posta Adresi", 230, false);
            txtPass = CreateStyledTextBox("Şifre", 310, true);

            btnRegister = new Button()
            {
                Text = "KAYIT OL VE KOD GÖNDER",
                Size = new Size(300, 55),
                Location = new Point(75, 400),
                BackColor = Color.FromArgb(66, 133, 244),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += BtnRegister_Click;

            // Giriş Sayfasına Dönme Linki (Senin istediğin vazgeçme seçeneği)
            lnkBackToLogin = new LinkLabel()
            {
                Text = "Zaten bir hesabım var? Giriş Yap",
                Location = new Point(130, 480),
                LinkColor = Color.LightGray,
                ActiveLinkColor = Color.White,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lnkBackToLogin.LinkClicked += (s, e) => {
                new LoginForm().Show();
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lblClose, lblTitle, lblSub, txtUser, txtEmail, txtPass, btnRegister, lnkBackToLogin });
        }

        private TextBox CreateStyledTextBox(string placeholder, int y, bool isPassword)
        {
            TextBox tb = new TextBox() { Size = new Size(300, 40), Location = new Point(75, y), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12), Text = placeholder };
            tb.Enter += (s, e) => { if (tb.Text == placeholder) { tb.Text = ""; tb.ForeColor = Color.White; if (isPassword) tb.PasswordChar = '●'; } };
            tb.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = placeholder; tb.ForeColor = Color.Gray; if (isPassword) tb.PasswordChar = '\0'; } };
            return tb;
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            if (txtUser.Text == "Kullanıcı Adı" || txtEmail.Text == "E-Posta Adresi" || txtPass.Text == "Şifre")
            {
                MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string verificationCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            string userEmail = txtEmail.Text.Trim(); // Trim ekle

            using (var conn = Database.GetConnection())
            {
                try
                {
                    conn.Open();
                    string sql = "INSERT INTO users (username, email, password, verification_code, is_verified) VALUES (@user, @mail, @pass, @code, 0)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@user", txtUser.Text);
                    cmd.Parameters.AddWithValue("@mail", userEmail);
                    cmd.Parameters.AddWithValue("@pass", txtPass.Text);
                    cmd.Parameters.AddWithValue("@code", verificationCode);

                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        EmailService.SendVerificationCode(userEmail, verificationCode);
                        MessageBox.Show("Kayıt başarılı! Doğrulama ekranına yönlendiriliyorsunuz.", "Başarılı");

                        VerificationForm vf = new VerificationForm(userEmail);
                        vf.Show();
                        this.Hide();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bu e-posta veya kullanıcı adı zaten kullanımda olabilir.\nHata: " + ex.Message);
                }
            }
        }
    }
}