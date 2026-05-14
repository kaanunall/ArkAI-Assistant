using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ArkAI
{
    public partial class VerificationForm : Form
    {
        private string userEmail;
        private TextBox txtCode;
        private Button btnVerify;
        private Label lblClose;

        public VerificationForm(string email)
        {
            this.userEmail = email;
            try { InitializeComponent(); } catch { }
            SetupDesign();
        }

        private void SetupDesign()
        {
            this.Size = new Size(450, 600);
            this.BackColor = Color.FromArgb(32, 33, 36);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            lblClose = new Label() { Text = "X", ForeColor = Color.White, Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(410, 15), Cursor = Cursors.Hand, AutoSize = true };
            lblClose.Click += (s, e) => this.Close();

            Label lblTitle = new Label() { Text = "KOD DOĞRULAMA", ForeColor = Color.White, Font = new Font("Segoe UI", 22, FontStyle.Bold), Location = new Point(75, 60), AutoSize = true };

            Label lblInfo = new Label()
            {
                Text = userEmail + "\nadresine gönderilen 6 haneli kodu aşağıya girin:",
                ForeColor = Color.DarkGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(75, 120),
                Size = new Size(300, 60),
                Font = new Font("Segoe UI", 10)
            };

            txtCode = new TextBox()
            {
                Size = new Size(300, 50),
                Location = new Point(75, 220),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.Gray,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                MaxLength = 6,
                Text = "000000"
            };

            txtCode.Enter += (s, e) => { if (txtCode.Text == "000000") { txtCode.Text = ""; txtCode.ForeColor = Color.White; } };
            txtCode.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtCode.Text)) { txtCode.Text = "000000"; txtCode.ForeColor = Color.Gray; } };

            btnVerify = new Button()
            {
                Text = "HESABI ONAYLA VE BAŞLAT",
                Size = new Size(300, 60),
                Location = new Point(75, 320),
                BackColor = Color.FromArgb(0, 190, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnVerify.FlatAppearance.BorderSize = 0;
            btnVerify.Click += BtnVerify_Click;

            this.Controls.AddRange(new Control[] { lblClose, lblTitle, lblInfo, txtCode, btnVerify });
        }

        private void BtnVerify_Click(object sender, EventArgs e)
        {
            // Boşlukları temizleyerek kontrol et
            string inputCode = txtCode.Text.Trim();
            string cleanEmail = userEmail.Trim();

            if (inputCode == "000000" || inputCode.Length < 6)
            {
                ArkMessage.Show("Lütfen 6 haneli kodu girin!");
                return;
            }

            using (var conn = Database.GetConnection())
            {
                try
                {
                    conn.Open();
                    // 1. KODU KONTROL ET
                    string sql = "SELECT COUNT(*) FROM users WHERE email=@mail AND verification_code=@code";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@mail", cleanEmail);
                    cmd.Parameters.AddWithValue("@code", inputCode);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        // 2. ONAY DURUMUNU GÜNCELLE (Parametreyi buraya eklemeyi unutma!)
                        string updateSql = "UPDATE users SET is_verified=1 WHERE email=@mail";
                        MySqlCommand updateCmd = new MySqlCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("@mail", cleanEmail); // Hatanın ana kaynağı burasıydı
                        updateCmd.Parameters.Clear(); // Önceki parametreleri temizle
                        updateCmd.Parameters.AddWithValue("@mail", cleanEmail);
                        updateCmd.ExecuteNonQuery();

                        ArkMessage.Show("Doğrulama başarılı!");
                        new LoginForm().Show();
                        this.Close();
                    }
                    else { ArkMessage.Show("Hatalı kod!"); }
                }
                catch (Exception ex) { ArkMessage.Show("Detaylı Hata: " + ex.Message); }
            }
        }
    }
}