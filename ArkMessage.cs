using System;
using System.Drawing;
using System.Windows.Forms;

namespace ArkAI
{
    public static class ArkMessage
    {
        public static void Show(string message, string title = "Ark AI")
        {
            Form msgForm = new Form();
            msgForm.Size = new Size(350, 180);
            msgForm.BackColor = Color.FromArgb(45, 45, 48); // Koyu gri
            msgForm.ForeColor = Color.White;
            msgForm.FormBorderStyle = FormBorderStyle.None;
            msgForm.StartPosition = FormStartPosition.CenterScreen;

            // Kenarlık için Panel
            Panel pnlBorder = new Panel() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            msgForm.Controls.Add(pnlBorder);

            Label lblTitle = new Label() { Text = title, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DeepSkyBlue, Location = new Point(10, 10), AutoSize = true };
            Label lblText = new Label() { Text = message, Font = new Font("Segoe UI", 10), Location = new Point(20, 50), Size = new Size(310, 60), TextAlign = ContentAlignment.MiddleCenter };

            Button btnOk = new Button()
            {
                Text = "TAMAM",
                Size = new Size(100, 35),
                Location = new Point(125, 120),
                BackColor = Color.FromArgb(66, 133, 244),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => msgForm.Close();

            pnlBorder.Controls.AddRange(new Control[] { lblTitle, lblText, btnOk });
            msgForm.ShowDialog();
        }
    }
}