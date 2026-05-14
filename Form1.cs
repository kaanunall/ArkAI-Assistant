#nullable disable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Speech.Synthesis;
using Vosk;
using NAudio.Wave;

namespace ArkAI
{
    public partial class Form1 : Form
    {
        // --- SİSTEM DEĞİŞKENLERİ ---
        private SpeechSynthesizer reader = new SpeechSynthesizer();
        private Model model;
        private VoskRecognizer rec;
        private WaveInEvent waveIn;
        private RichTextBox rtbChat;
        private System.Windows.Forms.Timer animTimer;

        // --- TASARIM DEĞİŞKENLERİ ---
        private float orbPulse = 0;
        private bool isListening = false;
        private bool isTalking = false;
        private List<string> chatHistory = new List<string>();

        [DllImport("user32.dll")] internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        [StructLayout(LayoutKind.Sequential)] internal struct WindowCompositionAttributeData { public WindowCompositionAttribute Attribute; public IntPtr Data; public int SizeOfData; }
        internal enum WindowCompositionAttribute { WCA_ACCENT_POLICY = 19 }
        internal enum AccentState { ACCENT_ENABLE_ACRYLICBLURBEHIND = 4 }
        [StructLayout(LayoutKind.Sequential)] internal struct AccentPolicy { public AccentState AccentState; public int AccentFlags; public int GradientColor; public int AnimationId; }

        public Form1()
        {
            this.Size = new Size(1100, 750);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(10, 10, 15); // Derin Uzay Siyahı
            this.DoubleBuffered = true;

            EnableBlur();
            InitUI();
            SetupVoice();
            SetupVosk();

            animTimer = new System.Windows.Forms.Timer() { Interval = 20 };
            animTimer.Tick += (s, e) => { orbPulse += 0.1f; this.Invalidate(); };
            animTimer.Start();
        }

        private void EnableBlur()
        {
            var accent = new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND, GradientColor = (0x99 << 24) | (0x0A0A0F & 0xFFFFFF) };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData { Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY, SizeOfData = accentStructSize, Data = accentPtr };
            SetWindowCompositionAttribute(this.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        private void InitUI()
        {
            rtbChat = new RichTextBox
            {
                Size = new Size(800, 380),
                Location = new Point(150, 120),
                BackColor = Color.FromArgb(15, 15, 20),
                ForeColor = Color.FromArgb(200, 255, 255),
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 11),
                ReadOnly = true,
                Padding = new Padding(10)
            };
            this.Controls.Add(rtbChat);

            Label btnClose = new Label { Text = "✕", ForeColor = Color.White, Location = new Point(1050, 20), Cursor = Cursors.Hand, AutoSize = true, Font = new Font("Arial", 16) };
            btnClose.Click += (s, e) => this.Hide(); // Sadece gizle, programı tamamen kapatmasın
            this.Controls.Add(btnClose);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Cyber Çerçeve Çizimi (HUD Tasarımı)
            using (Pen p = new Pen(Color.FromArgb(40, 0, 255, 255), 2))
            {
                g.DrawLine(p, 20, 20, 100, 20); g.DrawLine(p, 20, 20, 20, 100);
                g.DrawLine(p, Width - 100, 20, Width - 20, 20); g.DrawLine(p, Width - 20, 20, Width - 20, 100);
                g.DrawLine(p, 20, Height - 100, 20, Height - 20); g.DrawLine(p, 20, Height - 20, 100, Height - 20);
                g.DrawLine(p, Width - 100, Height - 20, Width - 20, Height - 20); g.DrawLine(p, Width - 20, Height - 100, Width - 20, Height - 20);
            }

            // 2. Modern Durum Bilgileri
            DrawStatus(g);

            // 3. Ana AI Çekirdeği (Merkez Alt)
            int centerX = this.Width / 2;
            int centerY = 620;

            Color coreColor = isListening ? Color.DeepPink : (isTalking ? Color.SpringGreen : Color.Cyan);
            int pulseAttr = (int)(Math.Sin(orbPulse) * 15);

            for (int i = 1; i <= 3; i++)
            {
                int r = 100 + (i * 25) + pulseAttr;
                using (Pen pen = new Pen(Color.FromArgb(100 / i, coreColor), 2))
                {
                    pen.DashStyle = DashStyle.Dash;
                    g.DrawEllipse(pen, centerX - r / 2, centerY - r / 2, r, r);
                }
            }

            Rectangle coreRect = new Rectangle(centerX - 40, centerY - 40, 80, 80);
            using (PathGradientBrush pgb = new PathGradientBrush(GetEllipsePath(coreRect)))
            {
                pgb.CenterColor = Color.White;
                pgb.SurroundColors = new Color[] { coreColor };
                g.FillEllipse(pgb, coreRect);
            }

            string statusText = isListening ? "<< DINLEME MODU AKTIF >>" : (isTalking ? "<< ARK YANIT VERIYOR >>" : "SYSTEM READY");
            g.DrawString(statusText, new Font("Consolas", 10, FontStyle.Bold), new SolidBrush(coreColor), centerX - 75, centerY + 80);
        }

        private void DrawStatus(Graphics g)
        {
            g.DrawString(DateTime.Now.ToString("HH:mm:ss"), new Font("Segoe UI Light", 35), Brushes.White, 50, 40);
            g.DrawString(DateTime.Now.ToString("dd.MM.yyyy | dddd").ToUpper(), new Font("Consolas", 10), Brushes.Cyan, 55, 95);

            Rectangle weatherRect = new Rectangle(820, 50, 230, 100);
            using (LinearGradientBrush lgb = new LinearGradientBrush(weatherRect, Color.FromArgb(30, 0, 255, 255), Color.Transparent, 45f))
            {
                g.FillRoundedRectangle(lgb, weatherRect, 15);
                g.DrawRoundedRectangle(new Pen(Color.FromArgb(80, 0, 255, 255)), weatherRect, 15);
            }
            g.DrawString("24°C", new Font("Segoe UI", 25), Brushes.White, 835, 60);
            g.DrawString("ISTANBUL / CLEAR", new Font("Consolas", 9, FontStyle.Bold), Brushes.DeepSkyBlue, 838, 105);
        }

        private GraphicsPath GetEllipsePath(Rectangle rect) { GraphicsPath path = new GraphicsPath(); path.AddEllipse(rect); return path; }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (new Rectangle(this.Width / 2 - 80, 540, 160, 160).Contains(e.Location))
            {
                isListening = true;
                try { waveIn?.StartRecording(); } catch { }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isListening = false;
            try { waveIn?.StopRecording(); } catch { }
        }

        private void SetupVosk()
        {
            try
            {
                model = new Model("model");
                rec = new VoskRecognizer(model, 16000.0f);
                waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
                waveIn.DataAvailable += (s, e) =>
                {
                    if (rec.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        dynamic j = JsonConvert.DeserializeObject(rec.Result());
                        if (j?.text != null && !string.IsNullOrEmpty(j.text.ToString()))
                            this.Invoke(new MethodInvoker(() => SoruSorVeCevapAl(j.text.ToString())));
                    }
                };
            }
            catch { }
        }

        private void SetupVoice()
        {
            reader.SpeakStarted += (s, e) => { isTalking = true; this.Invalidate(); };
            reader.SpeakCompleted += (s, e) => { isTalking = false; this.Invalidate(); };
            foreach (var v in reader.GetInstalledVoices()) if (v.VoiceInfo.Culture.Name.Contains("tr-TR")) { reader.SelectVoice(v.VoiceInfo.Name); break; }
        }

        private async void SoruSorVeCevapAl(string msg)
        {
            AppendChat("Siz", msg);

            // YENİ: Gemini yerine kendi yerel zekamıza soruyoruz
            string res = await AskLocalBrain(msg);

            AppendChat("Ark AI", res);
            reader.SpeakAsync(res);
        }

        private void AppendChat(string s, string m)
        {
            rtbChat.Invoke(new MethodInvoker(() => {
                string time = DateTime.Now.ToString("HH:mm");
                rtbChat.SelectionAlignment = s == "Siz" ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                rtbChat.SelectionColor = s == "Siz" ? Color.White : Color.Cyan;
                rtbChat.AppendText($"\n[{time}] {s}:\n");
                rtbChat.SelectionColor = Color.FromArgb(200, 200, 200);
                rtbChat.AppendText($"{m}\n");
                rtbChat.ScrollToCaret();
            }));
        }

        // =================================================================================
        // YENİ EKLENEN: YEREL YAPAY ZEKA BEYNİ (LOCAL AI BRAIN)
        // İstediğin kadar kelime ve cevap ekleyebilirsin!
        // =================================================================================
        private async Task<string> AskLocalBrain(string input)
        {
            // Yapay zeka düşünüyormuş gibi ufak bir bekleme süresi (Animasyonlar aksın diye)
            await Task.Delay(500);

            // Gelen sesi küçük harfe çevir ki eşleştirmesi kolay olsun
            string lowerInput = input.ToLower();

            // KELİME KONTROLLERİ VE CEVAPLAR
            if (lowerInput.Contains("merhaba") || lowerInput.Contains("selam"))
            {
                return "Merhaba efendim, sistemler aktif. Size nasıl yardımcı olabilirim?";
            }
            else if (lowerInput.Contains("naber") || lowerInput.Contains("nasılsın"))
            {
                return "Bütün sistemlerim stabil çalışıyor, teşekkür ederim. Sürüşe hazırız.";
            }
            else if (lowerInput.Contains("kimsin") || lowerInput.Contains("adın ne"))
            {
                return "Ben Ark. Sizin kişisel yapay zeka ve sürüş asistanınızım.";
            }
            else if (lowerInput.Contains("hava") || lowerInput.Contains("sıcaklık"))
            {
                return "Sensörlerime göre dışarıda hava 24 derece ve açık görünüyor.";
            }
            else if (lowerInput.Contains("müzik") || lowerInput.Contains("şarkı") || lowerInput.Contains("oynat"))
            {
                return "Müzik sistemine ana ekrandan erişebilirsiniz efendim. Keyifli dinlemeler.";
            }
            else if (lowerInput.Contains("teşekkür") || lowerInput.Contains("sağol"))
            {
                return "Rica ederim, görevim size yardımcı olmak.";
            }
            else if (lowerInput.Contains("hız") || lowerInput.Contains("limit"))
            {
                return "Şu anki güvenlik limitimiz 120 kilometre saat olarak ayarlanmıştır.";
            }
            else
            {
                // Bilinmeyen bir şey söylenirse verilecek standart cevap
                return "Bu komutu henüz veritabanımda bulamadım efendim. Lütfen tekrar eder misiniz?";
            }
        }
        // =================================================================================

        public void YorgunlukTespitEdildi()
        {
            this.Invoke(new MethodInvoker(() => {
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                AppendChat("SİSTEM", "!!! KRİTİK YORGUNLUK ALGILANDI !!!");

                // Yorgunluk için yerel sert uyarı
                string res = "Dikkat! Uyku hali tespit edildi! Lütfen aracın kontrolünü sağlayın ve en kısa sürede mola verin!";

                reader.Rate = 2; // Sesi hızlandır
                reader.SpeakAsync(res);
                reader.Rate = 0; // Normale döndür
            }));
        }
    }

    // --- MODERN GÖRSEL ARAÇLAR ---
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush b, Rectangle r, int radius)
        {
            using (GraphicsPath path = GetRoundedPath(r, radius)) g.FillPath(b, path);
        }
        public static void DrawRoundedRectangle(this Graphics g, Pen p, Rectangle r, int radius)
        {
            using (GraphicsPath path = GetRoundedPath(r, radius)) g.DrawPath(p, path);
        }
        private static GraphicsPath GetRoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}