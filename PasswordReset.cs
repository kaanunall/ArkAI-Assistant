using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ArkAI
{
    public partial class PasswordReset : Form
    {
        private TextBox txtEmail, txtCode, txtNewPass;
        private Button btnAction;
        private Label lblClose;
        private bool isCodeSent = false;

        public PasswordReset() { SetupDesign(); }

        private void SetupDesign()
        {
            this.Size = new Size(450, 600);
            this.BackColor = Color.FromArgb(32, 33, 36);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            lblClose = new Label() { Text = "X", ForeColor = Color.White, Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(410, 15), Cursor = Cursors.Hand, AutoSize = true };
            lblClose.Click += (s, e) => { new LoginForm().Show(); this.Close(); };

            Label lblTitle = new Label() { Text = "ŞİFRE YENİLEME", ForeColor = Color.White, Font = new Font("Segoe UI", 22, FontStyle.Bold), Location = new Point(75, 60), AutoSize = true };

            txtEmail = CreateStyledTextBox("E-Posta Adresiniz", 160, false);
            txtCode = CreateStyledTextBox("Onay Kodu (6 Hane)", 240, false);
            txtCode.Enabled = false;
            txtNewPass = CreateStyledTextBox("Yeni Şifreniz", 320, true);
            txtNewPass.Enabled = false;

            btnAction = new Button()
            {
                Text = "SIFIRLAMA KODU GÖNDER",
                Size = new Size(300, 60),
                Location = new Point(75, 420),
                BackColor = Color.FromArgb(219, 68, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += BtnAction_Click;

            this.Controls.AddRange(new Control[] { lblClose, lblTitle, txtEmail, txtCode, txtNewPass, btnAction });
        }

        private TextBox CreateStyledTextBox(string placeholder, int y, bool isPassword)
        {
            TextBox tb = new TextBox() { Size = new Size(300, 40), Location = new Point(75, y), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12), Text = placeholder };
            tb.Enter += (s, e) => { if (tb.Text == placeholder) { tb.Text = ""; tb.ForeColor = Color.White; if (isPassword) tb.PasswordChar = '●'; } };
            tb.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = placeholder; tb.ForeColor = Color.Gray; if (isPassword) tb.PasswordChar = '\0'; } };
            return tb;
        }

        private void BtnAction_Click(object sender, EventArgs e)
        {
            if (!isCodeSent)
            {
                string resetCode = Guid.NewGuid().ToString().Substring(0, 6);
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE users SET verification_code=@c WHERE email=@m";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@c", resetCode); cmd.Parameters.AddWithValue("@m", txtEmail.Text);
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        EmailService.SendVerificationCode(txtEmail.Text, resetCode);
                        ArkMessage.Show("Kod gönderildi!");
                        txtCode.Enabled = true; txtNewPass.Enabled = true;
                        btnAction.Text = "ŞİFREYİ GÜNCELLE"; btnAction.BackColor = Color.FromArgb(15, 157, 88); isCodeSent = true;
                    }
                }
            }
            else
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE users SET password=@p WHERE email=@m AND verification_code=@c";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@p", txtNewPass.Text); cmd.Parameters.AddWithValue("@m", txtEmail.Text); cmd.Parameters.AddWithValue("@c", txtCode.Text);
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        ArkMessage.Show("Şifre güncellendi!"); new LoginForm().Show(); this.Close();
                    }
                    else { ArkMessage.Show("Hatalı kod!"); }
                }
            }
        }
    }
}