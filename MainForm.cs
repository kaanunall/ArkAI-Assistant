#nullable disable
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Speech.Synthesis;
using Vosk;
using Newtonsoft.Json;

namespace ArkAI
{
    public partial class MainForm : Form
    {
        private WebView2 webMap;
        private string youtubeApiKey = "";

        private YoutubeClient ytClient = new YoutubeClient();
        private IWavePlayer waveOut;
        private WaveStream audioReader;
        private VolumeSampleProvider volumeProvider;
        private System.Windows.Forms.Timer syncTimer;

        private Model voskModel;
        private VoskRecognizer rec;
        private WaveInEvent waveIn;

        private SpeechSynthesizer synth = new SpeechSynthesizer();

        private bool isWaitingForSongName = false;
        private bool isWaitingForLocation = false;
        private bool isTalking = false;

        private bool isEyesClosedAlertTriggered = false;
        private bool isCriticalFatigueTriggered = false;

        private bool isWaitingForFatigueChoice = false;

        // =================================================================================
        // SOHBET MODU DEĞİŞKENLERİ VE İÇERİKLERİ
        // =================================================================================
        private bool isWaitingForRiddleAnswer = false;
        private string currentRiddleAnswer = "";

        private bool isPlayingWordChain = false;
        private char wordChainLastChar = ' ';

        // YENİ: Vosk'un daha iyi anlayacağı "Zıt Anlamlısını Söyle" oyunu
        private bool isPlayingOppositeWord = false;
        private string currentOppositeWordAnswer = "";

        private bool isWaitingForMusicSuggest = false;

        private Random rnd = new Random();

        // Uykuyu açacak ilginç bilgiler
        private string[] funFacts = {
            "Biliyor muydunuz? İnsan beyni uyanıkken küçük bir ampulü yakacak kadar elektrik üretir. Zihninizi açık tutun, ampulünüz sönmesin!",
            "Şu anki hızınızla dünya etrafında tam bir tur atmak yaklaşık kırk gün sürerdi. Tabii okyanusları aşabilseydik.",
            "Kahve içtikten sonra on beş dakika uyumak, beynin kafeini en verimli şekilde kullanmasını sağlar. Buna kahve uykusu denir.",
            "Dünyadaki en uzun trafik sıkışıklığı Çin'de tam on iki gün sürmüştür. Neyse ki bizim yolumuz açık!",
            "Müzik dinlemek dopamin salgılatır ve uykuyu açar. İsterseniz hareketli bir şarkı açabilirim, sadece söylemeniz yeterli."
        };

        // Zihni çalıştıracak bilmeceler
        private (string question, string answer)[] riddles = {
            ("Ağzı var dili yok, nefesi var canı yok. Nedir bu?", "kaval"),
            ("Gökte gördüm köprü, rengi yedi türlü. Nedir bu?", "gökkuşağı"),
            ("Çarşıdan aldım bir tane, eve geldim bin tane. Nedir bu?", "nar"),
            ("Ben giderim o gider, içimde tık tık eder. Nedir bu?", "kalp"),
            ("Ateşe girer yanmaz, suya girer ıslanmaz. Nedir bu?", "güneş")
        };

        // Kelime Zinciri için başlangıç kelimeleri
        private string[] startWords = { "araba", "tekerlek", "yolcu", "harita", "radyo" };

        // YENİ: Zıt anlamlı kelimeler (Vosk bunları %100 doğru anlar)
        private (string word, string opposite)[] oppositeWords = {
            ("siyah", "beyaz"), ("büyük", "küçük"), ("sıcak", "soğuk"), ("hızlı", "yavaş"), ("gece", "gündüz"), ("uzun", "kısa")
        };

        // Zihin açıcı sohbet soruları
        private string[] chatQuestions = {
            "Eğer dünyadaki tüm yolları bir renge boyayabilseydiniz, hangi rengi seçerdiniz?",
            "Şu an istediğiniz herhangi bir arabayı kullanma şansınız olsaydı, hangisini seçerdiniz?",
            "Yolculuk yaparken en çok dağ manzaralarını mı seversiniz yoksa deniz kıyısını mı?",
            "Eğer zaman yolculuğu yapan bir arabada olsaydık, hangi yıla gitmek isterdiniz?"
        };
        // =================================================================================

        private Panel pnlAppCenterContainer;
        private WebView2 webAppCenter;
        private WebView2 webHiddenCamera;
        private Button btnBackToLauncher;
        private Button btnCloseAppCenter;

        public MainForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;

            syncTimer = new System.Windows.Forms.Timer { Interval = 500 };
            syncTimer.Tick += SyncTimer_Tick;

            try
            {
                synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult, 0, new System.Globalization.CultureInfo("tr-TR"));
                synth.Volume = 100;
            }
            catch { }

            synth.SpeakStarted += (s, e) =>
            {
                isTalking = true;
                if (volumeProvider != null)
                {
                    volumeProvider.Volume = 0.15f;
                }
            };

            synth.SpeakCompleted += (s, e) =>
            {
                isTalking = false;
                if (volumeProvider != null)
                {
                    volumeProvider.Volume = 1.0f;
                }
            };

            InitModernSystem();
            SetupVosk();
        }

        private void SetupVosk()
        {
            try
            {
                Vosk.Vosk.SetLogLevel(-1);
                voskModel = new Model("model");
                rec = new VoskRecognizer(voskModel, 16000.0f);
                waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };

                waveIn.DataAvailable += (s, e) =>
                {
                    if (isTalking) return;

                    if (rec.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        dynamic j = JsonConvert.DeserializeObject(rec.Result());
                        if (j?.text != null)
                        {
                            string command = j.text.ToString().Trim().ToLower();
                            if (!string.IsNullOrEmpty(command))
                            {
                                this.Invoke((MethodInvoker)delegate { ProcessVoiceCommand(command); });
                            }
                        }
                    }
                };

                waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Vosk sistemi başlatılamadı: " + ex.Message);
            }
        }

        private void ProcessVoiceCommand(string command)
        {
            // YENİ: EVRENSEL İPTAL / ÇIKIŞ KOMUTU
            // Eğer herhangi bir oyun/sohbet modundaysak ve kullanıcı iptal derse her şeyi sıfırla
            if (command.Contains("iptal") || command.Contains("vazgeçtim") || command.Contains("yeter") || command.Contains("oyunu bitir") || command.Contains("kapat"))
            {
                if (isWaitingForFatigueChoice || isWaitingForRiddleAnswer || isPlayingOppositeWord || isPlayingWordChain || isWaitingForMusicSuggest)
                {
                    isWaitingForFatigueChoice = false;
                    isWaitingForRiddleAnswer = false;
                    isPlayingOppositeWord = false;
                    isPlayingWordChain = false;
                    isWaitingForMusicSuggest = false;

                    synth.SpeakAsyncCancelAll();
                    synth.SpeakAsync("Tamamdır, oyun ve sohbet modundan çıktım. Sizi dinliyorum.");
                    return; // İptal ettikten sonra başka işlem yapma
                }
            }

            // =================================================================================
            // Yorgunluk Sonrası Sürücü Kararı Ana Menüsü
            // =================================================================================
            if (isWaitingForFatigueChoice)
            {
                if (command.Contains("sohbet") || command.Contains("konuş") || command.Contains("muhabbet") || command.Contains("anlat") || command.Contains("oyun"))
                {
                    isWaitingForFatigueChoice = false;
                    synth.SpeakAsync("Harika. Uykunuzu açmak için birçok seçeneğim var. 'Bilgi ver' derseniz ilginç şeyler anlatırım. 'Bilmece sor' derseniz zihninizi test ederim. 'Oyun oynayalım' derseniz kelime oyunları oynarız. Veya sadece 'müzik öner' deyin. Ne yapalım?");
                }
                else if (command.Contains("kenara") || command.Contains("çek") || command.Contains("dinlen") || command.Contains("konum") || command.Contains("tesis") || command.Contains("benzin") || command.Contains("evet"))
                {
                    isWaitingForFatigueChoice = false;
                    synth.SpeakAsync("Anlaşıldı. En yakın mola yerine rotanızı oluşturuyorum. Lütfen dikkatlice sağa çekin.");
                    RunJavaScript($"searchPlace('benzin', true);");
                }
                else if (command.Contains("hayır") || command.Contains("istemiyorum") || command.Contains("iyiyim") || command.Contains("devam"))
                {
                    isWaitingForFatigueChoice = false;
                    synth.SpeakAsync("Anlaşıldı. Lütfen dikkatli sürün. Ben buradayım, ne zaman isterseniz 'oyun oynayalım' veya 'sohbet edelim' diyebilirsiniz.");
                }
                return;
            }

            // =================================================================================
            // OYUN VE SOHBET MODLARI İŞLEYİŞİ
            // =================================================================================
            if (isWaitingForRiddleAnswer)
            {
                if (command.Contains(currentRiddleAnswer))
                {
                    synth.SpeakAsync("Tebrikler, doğru bildiniz! Zihniniz pırıl pırıl çalışıyor. Başka bir oyun için 'bilmece sor' diyebilirsiniz.");
                    isWaitingForRiddleAnswer = false;
                }
                else if (command.Contains("bilmiyorum") || command.Contains("pes") || command.Contains("nedir") || command.Contains("söyle"))
                {
                    synth.SpeakAsync($"Cevap {currentRiddleAnswer} olacaktı. Canınız sağ olsun. Yola odaklanmaya devam edelim.");
                    isWaitingForRiddleAnswer = false;
                }
                else if (!command.Contains("müzik") && !command.Contains("harita"))
                {
                    synth.SpeakAsync("Maalesef yanlış. Tekrar düşünün veya pes deyin.");
                    return;
                }
                else { isWaitingForRiddleAnswer = false; }
            }

            if (isPlayingOppositeWord)
            {
                if (command.Contains(currentOppositeWordAnswer))
                {
                    synth.SpeakAsync("Harika, anında buldunuz! Uykunuz kesinlikle açılmış görünüyor.");
                    isPlayingOppositeWord = false;
                }
                else if (command.Contains("pes") || command.Contains("bilmiyorum") || command.Contains("zor"))
                {
                    synth.SpeakAsync($"Doğrusu {currentOppositeWordAnswer} olacaktı. Olsun, dikkatiniz toplandı bile.");
                    isPlayingOppositeWord = false;
                }
                else if (!command.Contains("müzik") && !command.Contains("harita"))
                {
                    synth.SpeakAsync("Hayır, o değil. Bir daha deneyin veya pes deyin.");
                    return;
                }
                else { isPlayingOppositeWord = false; }
            }

            if (isPlayingWordChain)
            {
                if (command.Contains("pes") || command.Contains("bulamadım"))
                {
                    synth.SpeakAsync("Peki, oyunu bitiriyorum. Gözleriniz açık olsun.");
                    isPlayingWordChain = false;
                }
                // DÜZELTME: Startswith ile daha güvenilir harf kontrolü
                else if (command.Length > 2 && command.Trim().StartsWith(wordChainLastChar.ToString()))
                {
                    string pWord = command.Trim().Split(' ')[0]; // İlk kelimeyi al
                    wordChainLastChar = pWord[pWord.Length - 1]; // Yeni son harf
                    synth.SpeakAsync($"Güzel. Yeni harfimiz {char.ToUpper(wordChainLastChar)}. Sırada sizsiniz, {char.ToUpper(wordChainLastChar)} harfi ile başlayan yeni bir kelime söyleyin.");
                    return;
                }
                else if (!command.Contains("müzik") && !command.Contains("harita"))
                {
                    synth.SpeakAsync($"Kelimeniz {char.ToUpper(wordChainLastChar)} harfi ile başlamalıydı. Tekrar deneyin veya pes deyin.");
                    return;
                }
                else { isPlayingWordChain = false; }
            }

            if (isWaitingForMusicSuggest)
            {
                if (command.Contains("evet") || command.Contains("aç") || command.Contains("çal") || command.Contains("olur"))
                {
                    isWaitingForMusicSuggest = false;
                    synth.SpeakAsync("Hemen hareketli bir müzik açıyorum.");
                    RunJavaScript($"searchYouTube('Hareketli araba şarkıları', true, true);");
                }
                else
                {
                    isWaitingForMusicSuggest = false;
                    synth.SpeakAsync("Peki, sessizliği tercih ettiniz.");
                }
                return;
            }

            // =================================================================================
            // NORMAL KOMUTLAR
            // =================================================================================
            if (isWaitingForSongName)
            {
                isWaitingForSongName = false;
                synth.SpeakAsync(command + " başlatılıyor.");
                RunJavaScript($"searchYouTube('{command}', true, true);");
                return;
            }

            if (isWaitingForLocation)
            {
                isWaitingForLocation = false;
                synth.SpeakAsync(command + " için rota oluşturuluyor.");
                RunJavaScript($"searchPlace('{command}', true);");
                return;
            }

            // SÜRÜCÜ İNSİYATİFİ TETİKLEYİCİLERİ
            if (command.Contains("uykum geldi") || command.Contains("çok yoruldum") || command.Contains("uykum var") || command.Contains("gözlerim kapanıyor"))
            {
                isWaitingForFatigueChoice = true;
                synth.SpeakAsync("Anlıyorum, sürüş güvenliği her şeyden önemli. İsterseniz en yakın mola yerine rota oluşturabilirim, ya da uykunuzu açmak için sizinle oyun oynayabilirim. Hangisini tercih edersiniz?");
                return;
            }

            // ALT SOHBET MODLARI
            if (command.Contains("bilgi ver") || command.Contains("ilginç bir şey") || command.Contains("bana bir şey anlat"))
            {
                synth.SpeakAsync(funFacts[rnd.Next(funFacts.Length)]);
                return;
            }
            else if (command.Contains("bilmece sor") || command.Contains("bana bilmece"))
            {
                var r = riddles[rnd.Next(riddles.Length)];
                currentRiddleAnswer = r.answer;
                isWaitingForRiddleAnswer = true;
                synth.SpeakAsync(r.question);
                return;
            }
            else if (command.Contains("oyun oynayalım") || command.Contains("oyun oyna") || command.Contains("zihni çalıştır"))
            {
                int gameType = rnd.Next(2);
                if (gameType == 0)
                {
                    // DÜZELTME: Zıt anlamlı kelime oyunu başlatılır
                    var ow = oppositeWords[rnd.Next(oppositeWords.Length)];
                    currentOppositeWordAnswer = ow.opposite;
                    isPlayingOppositeWord = true;
                    synth.SpeakAsync($"Zıt anlamlısını bulma oyunu! Kelimeniz: {ow.word}. Bu kelimenin zıt anlamlısı nedir?");
                }
                else
                {
                    string sw = startWords[rnd.Next(startWords.Length)];
                    wordChainLastChar = sw[sw.Length - 1];
                    isPlayingWordChain = true;
                    synth.SpeakAsync($"Kelime zinciri oynuyoruz. Benim kelimem: {sw}. Şimdi siz {char.ToUpper(wordChainLastChar)} harfi ile başlayan bir kelime söyleyin.");
                }
                return;
            }
            else if (command.Contains("müzik öner") || command.Contains("hareketli bir şey"))
            {
                isWaitingForMusicSuggest = true;
                synth.SpeakAsync("Uykunuzu açmak için ritmik bir şarkı karışımı açmamı ister misiniz?");
                return;
            }
            else if (command.Contains("sohbet et") || command.Contains("soru sor"))
            {
                synth.SpeakAsync(chatQuestions[rnd.Next(chatQuestions.Length)]);
                return;
            }

            // TEMEL KOMUTLAR
            if (command.Contains("hey") || command.Contains("yapay zeka") || command.Contains("sistemi aç"))
            {
                Form1 aiForm = new Form1();
                aiForm.Show();
            }
            else if (command.Contains("konumuna git") || command.Contains("haritada bul") || command.Contains("rota oluştur"))
            {
                string locationName = command.Replace("konumuna git", "")
                                             .Replace("haritada bul", "")
                                             .Replace("rota oluştur", "")
                                             .Replace("için", "")
                                             .Trim();

                if (string.IsNullOrEmpty(locationName))
                {
                    isWaitingForLocation = true;
                    synth.SpeakAsync("Nereye gitmek istiyorsunuz?");
                }
                else
                {
                    synth.SpeakAsync(locationName + " için rota oluşturuluyor.");
                    RunJavaScript($"searchPlace('{locationName}', true);");
                }
            }
            else if (command.Contains("müzik aç") || command.Contains("şarkı aç") || command.Contains("şarkı çal") || command.Contains("müzik çal"))
            {
                string songName = command.Replace("şarkı aç", "")
                                         .Replace("müzik aç", "")
                                         .Replace("şarkı çal", "")
                                         .Replace("müzik çal", "")
                                         .Trim();

                if (string.IsNullOrEmpty(songName))
                {
                    isWaitingForSongName = true;
                    synth.SpeakAsync("Hangi şarkıyı açmamı istersiniz?");
                }
                else
                {
                    synth.SpeakAsync(songName + " başlatılıyor.");
                    RunJavaScript($"searchYouTube('{songName}', true, true);");
                }
            }
            else if (command.Contains("durdur") || command.Contains("oynat") || command.Contains("kapat"))
            {
                TogglePlayback();
            }
            else if (command.Contains("haritaya dön"))
            {
                if (pnlAppCenterContainer.Visible) CloseAppCenter();
            }
            else if (command.Contains("hava durumu") || command.Contains("hava nasıl") || command.Contains("dışarısı nasıl"))
            {
                synth.SpeakAsync("Sensörlerime göre dışarıda hava şu an on beş derece ve yolculuk için oldukça uygun.");
            }
            else if (command.Contains("nasılsın") || command.Contains("naber") || command.Contains("durum raporu"))
            {
                synth.SpeakAsync("Bütün sistemlerim stabil çalışıyor, teşekkür ederim. Sürüşe hazırız.");
            }
            else if (command.Contains("kimsin") || command.Contains("adın ne") || command.Contains("ismin ne"))
            {
                synth.SpeakAsync("Benim adım Ark. Gelişmiş kişisel sürüş asistanınızım.");
            }
            else if (command.Contains("teşekkür") || command.Contains("sağ ol") || command.Contains("teşekkürler"))
            {
                synth.SpeakAsync("Rica ederim. Görevim size yardımcı olmak ve güvenli bir sürüş sağlamak.");
            }
            else if (command.Contains("günaydın") || command.Contains("merhaba") || command.Contains("selam"))
            {
                synth.SpeakAsync("Merhaba efendim. Sisteme hoş geldiniz.");
            }
        }

        private async void InitModernSystem()
        {
            webMap = new WebView2 { Dock = DockStyle.Fill };
            this.Controls.Add(webMap);

            pnlAppCenterContainer = new Panel { Dock = DockStyle.Fill, Visible = false };

            Panel pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(10, 11, 20) };

            btnCloseAppCenter = new Button { Text = "❌ Haritaya Dön", Dock = DockStyle.Right, Width = 150, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(200, 40, 40), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnCloseAppCenter.FlatAppearance.BorderSize = 0;
            btnCloseAppCenter.Click += (s, e) => CloseAppCenter();

            btnBackToLauncher = new Button { Text = "🏠 Ana Menü", Dock = DockStyle.Left, Width = 150, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(30, 30, 40), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnBackToLauncher.FlatAppearance.BorderSize = 0;
            btnBackToLauncher.Click += (s, e) => LoadAppLauncher();

            pnlTopBar.Controls.Add(btnBackToLauncher);
            pnlTopBar.Controls.Add(btnCloseAppCenter);

            webAppCenter = new WebView2 { Dock = DockStyle.Fill };

            pnlAppCenterContainer.Controls.Add(webAppCenter);
            pnlAppCenterContainer.Controls.Add(pnlTopBar);

            this.Controls.Add(pnlAppCenterContainer);

            webHiddenCamera = new WebView2
            {
                Size = new Size(640, 480),
                Location = new Point(-3000, -3000),
                Visible = true
            };
            this.Controls.Add(webHiddenCamera);
            webHiddenCamera.SendToBack();

            var envMap = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Path.GetTempPath(), "ArkAI_Map"));
            var envApp = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Path.GetTempPath(), "ArkAI_App"));

            var envCamOptions = new CoreWebView2EnvironmentOptions("--enable-media-stream --use-fake-ui-for-media-stream --unsafely-treat-insecure-origin-as-secure=http://arkai.camera");
            var envCam = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Path.GetTempPath(), "ArkAI_Cam"), envCamOptions);

            webMap.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = Path.Combine(Path.GetTempPath(), "ArkAI_Map") };
            webAppCenter.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = Path.Combine(Path.GetTempPath(), "ArkAI_App") };
            webHiddenCamera.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = Path.Combine(Path.GetTempPath(), "ArkAI_Cam") };

            try
            {
                await webMap.EnsureCoreWebView2Async(envMap);
                webMap.CoreWebView2.Settings.UserAgent = "ArkAI_CarSystem_v1";
                webMap.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                webMap.CoreWebView2.SetVirtualHostNameToFolderMapping("assets.local", assetsPath, CoreWebView2HostResourceAccessKind.Allow);
                webMap.NavigateToString(GetLiquidHtml());

                await webAppCenter.EnsureCoreWebView2Async(envApp);
                webAppCenter.CoreWebView2.WebMessageReceived += WebAppCenter_WebMessageReceived;
                LoadAppLauncher();

                await webHiddenCamera.EnsureCoreWebView2Async(envCam);
                string camPath = AppDomain.CurrentDomain.BaseDirectory;
                webHiddenCamera.CoreWebView2.SetVirtualHostNameToFolderMapping("arkai.camera", camPath, CoreWebView2HostResourceAccessKind.Allow);

                webHiddenCamera.CoreWebView2.PermissionRequested += (s, arg) => { arg.State = CoreWebView2PermissionState.Allow; };

                webHiddenCamera.CoreWebView2.WebMessageReceived += (s, e) => {
                    string msg = e.TryGetWebMessageAsString();

                    if (msg == "SLEEP")
                    {
                        // DÜZELTME: Sadece asistan suskunsa kısa uyarı ver, sohbeti BÖLME!
                        if (!isTalking)
                        {
                            synth.SpeakAsyncCancelAll();
                            synth.SpeakAsync("Dikkat! Lütfen yola odaklanın.");
                        }
                    }
                    else if (msg == "FATIGUE_5SEC")
                    {
                        if (!isEyesClosedAlertTriggered)
                        {
                            isEyesClosedAlertTriggered = true;

                            System.Console.Beep(1000, 500);
                            System.Console.Beep(1000, 500);
                            System.Console.Beep(1000, 800);

                            RunJavaScript("document.getElementById('hud-panel').style.borderColor = 'red';");
                            RunJavaScript("document.getElementById('hud-panel').style.boxShadow = '0 0 50px rgba(255,0,0,1)';");

                            synth.SpeakAsyncCancelAll();
                            synth.SpeakAsync("Uyku hali tespit edildi! Lütfen hemen sağa çekin ve gözlerinizi açın.");
                        }
                    }
                    else if (msg == "FATIGUE_10SEC")
                    {
                        if (!isCriticalFatigueTriggered)
                        {
                            isCriticalFatigueTriggered = true;

                            System.Console.Beep(2000, 1000);
                            synth.SpeakAsyncCancelAll();
                            synth.SpeakAsync("Sürüş güvenliği kritik seviyede! Size en yakın mola yerine acil durum rotası oluşturuluyor, lütfen aracı acilen sağa çekin.");
                            RunJavaScript($"searchPlace('benzin', true);");
                        }
                    }
                    else if (msg == "EYES_OPENED")
                    {
                        if (isEyesClosedAlertTriggered)
                        {
                            isEyesClosedAlertTriggered = false;
                            isCriticalFatigueTriggered = false;

                            RunJavaScript("document.getElementById('hud-panel').style.borderColor = '#00f2ff';");
                            RunJavaScript("document.getElementById('hud-panel').style.boxShadow = '0 10px 30px rgba(0,0,0,0.5)';");

                            string aiResponse = "Gözlerinizi açtığınıza sevindim. Dinlenmek için kenara mı çekelim, yoksa uyanık kalmanız için sizinle oyun mu oynayalım?";

                            isWaitingForFatigueChoice = true;

                            synth.SpeakAsyncCancelAll();
                            synth.SpeakAsync(aiResponse);
                        }
                    }
                };

                string camHtml = GetHiddenCameraHtml();
                File.WriteAllText(Path.Combine(camPath, "camera.html"), camHtml, Encoding.UTF8);
                webHiddenCamera.CoreWebView2.Navigate("http://arkai.camera/camera.html");
            }
            catch (Exception ex) { MessageBox.Show("Sistem Yükleme Hatası: " + ex.Message); }
        }

        private void OpenAppCenter()
        {
            pnlAppCenterContainer.Visible = true;
            pnlAppCenterContainer.BringToFront();
            LoadAppLauncher();
        }

        private void CloseAppCenter()
        {
            pnlAppCenterContainer.Visible = false;
            if (webAppCenter.CoreWebView2 != null) webAppCenter.CoreWebView2.Navigate("about:blank");
        }

        private void LoadAppLauncher()
        {
            if (webAppCenter.CoreWebView2 != null)
            {
                webAppCenter.NavigateToString(GetLauncherHtml());
            }
        }

        private void WebAppCenter_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string appUrl = e.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(appUrl) && webAppCenter.CoreWebView2 != null)
            {
                webAppCenter.CoreWebView2.Navigate(appUrl);
            }
        }

        public void RunJavaScript(string script)
        {
            if (webMap != null && webMap.CoreWebView2 != null)
            {
                webMap.ExecuteScriptAsync(script);
            }
        }

        private void SyncTimer_Tick(object sender, EventArgs e)
        {
            if (audioReader != null && waveOut != null && waveOut.PlaybackState == PlaybackState.Playing)
            {
                try
                {
                    double current = audioReader.CurrentTime.TotalSeconds;
                    double total = audioReader.TotalTime.TotalSeconds;
                    if (total > 0)
                    {
                        webMap.ExecuteScriptAsync($"updateProgress({current.ToString().Replace(",", ".")}, {total.ToString().Replace(",", ".")});");
                    }
                }
                catch { }
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string rawData = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(rawData)) return;

            if (rawData.StartsWith("PLAY_ID:"))
            {
                string videoId = rawData.Replace("PLAY_ID:", "");
                _ = PlayMusicBypass(videoId);
                return;
            }

            this.Invoke((MethodInvoker)delegate
            {
                switch (rawData)
                {
                    case "APP_CENTER_OPEN":
                        OpenAppCenter();
                        break;
                    case "APP_CENTER_FORCE_CLOSE":
                        if (pnlAppCenterContainer.Visible) CloseAppCenter();
                        break;
                    case "AI_Open":
                        Form1 aiForm = new Form1();
                        aiForm.Show();
                        break;
                    case "Profile_Click":
                        ProfileForm pf = new ProfileForm();
                        pf.Show();
                        this.Hide();
                        break;
                    case "Music_Click":
                        webMap.ExecuteScriptAsync("toggleMusicPanel();");
                        break;
                    case "Map_Home":
                        webMap.ExecuteScriptAsync("map.flyTo([41.0082, 28.9784], 16);");
                        break;
                    case "TOGGLE_PAUSE":
                        TogglePlayback();
                        break;
                    case "Settings_Click":
                        CarSettings cs = new CarSettings(this);
                        cs.Show();
                        break;
                }
            });
        }

        private async Task PlayMusicBypass(string videoId)
        {
            try
            {
                syncTimer.Stop();
                if (waveOut != null) { waveOut.Stop(); waveOut.Dispose(); waveOut = null; }
                if (audioReader != null) { audioReader.Dispose(); audioReader = null; }
                volumeProvider = null;

                var manifest = await ytClient.Videos.Streams.GetManifestAsync(videoId);
                var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                audioReader = new MediaFoundationReader(streamInfo.Url);

                volumeProvider = new VolumeSampleProvider(audioReader.ToSampleProvider());
                volumeProvider.Volume = 1.0f;

                waveOut = new WaveOutEvent();
                waveOut.Init(volumeProvider);
                waveOut.Play();
                syncTimer.Start();
            }
            catch (Exception ex) { Console.WriteLine("C# Play Error: " + ex.Message); }
        }

        private void TogglePlayback()
        {
            if (waveOut == null) return;
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                syncTimer.Stop();
                webMap.ExecuteScriptAsync("setBtnState(false);");
            }
            else
            {
                waveOut.Play();
                syncTimer.Start();
                webMap.ExecuteScriptAsync("setBtnState(true);");
            }
        }

        private string GetHiddenCameraHtml()
        {
            return @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <script src='https://cdn.jsdelivr.net/npm/@mediapipe/face_mesh'></script>
                <script src='https://cdn.jsdelivr.net/npm/@mediapipe/camera_utils'></script>
            </head>
            <body>
                <video id='v' playsinline style='width: 640px; height: 480px;'></video>
                <script>
                    const video = document.getElementById('v');
                    let sleepStartTime = null; 
                    let fatigueSent = false;
                    let criticalFatigueSent = false;
                    let sleepCounter = 0; 

                    async function start() {
                        try {
                            const stream = await navigator.mediaDevices.getUserMedia({ video: true });
                            video.srcObject = stream;
                            await video.play();

                            const faceMesh = new FaceMesh({locateFile: (file) => `https://cdn.jsdelivr.net/npm/@mediapipe/face_mesh/${file}`});
                            faceMesh.setOptions({ maxNumFaces: 1, refineLandmarks: true });
                            
                            faceMesh.onResults(res => {
                                if(res.multiFaceLandmarks && res.multiFaceLandmarks.length > 0) {
                                    const p = res.multiFaceLandmarks[0];
                                    const dist = Math.abs(p[159].y - p[145].y); 
                                    
                                    if(dist < 0.015) { // Göz kapalı
                                        if(!sleepStartTime) sleepStartTime = Date.now();
                                        let duration = (Date.now() - sleepStartTime) / 1000;
                                        
                                        // 10 Saniye
                                        if(duration >= 10.0 && !criticalFatigueSent) {
                                            window.chrome.webview.postMessage('FATIGUE_10SEC');
                                            criticalFatigueSent = true;
                                        }
                                        // 5 Saniye
                                        else if(duration >= 5.0 && duration < 10.0 && !fatigueSent) {
                                            window.chrome.webview.postMessage('FATIGUE_5SEC');
                                            fatigueSent = true;
                                        } 
                                        // 1,2,3,4 Saniye uyarıları
                                        else if (duration >= sleepCounter + 1 && duration < 5.0) {
                                            sleepCounter++;
                                            window.chrome.webview.postMessage('SLEEP');
                                        }
                                    } else { // Göz açık
                                        if (sleepStartTime !== null && (fatigueSent || criticalFatigueSent)) {
                                            window.chrome.webview.postMessage('EYES_OPENED');
                                        }
                                        
                                        sleepStartTime = null;
                                        fatigueSent = false;
                                        criticalFatigueSent = false;
                                        sleepCounter = 0;
                                    }
                                }
                            });

                            const camera = new Camera(video, {
                                onFrame: async () => { await faceMesh.send({image: video}); },
                                width: 640, height: 480
                            });
                            camera.start();

                        } catch (err) { console.error('Kamera Hatası: ', err); }
                    }

                    window.onload = start;
                </script>
            </body>
            </html>";
        }

        private string GetLauncherHtml()
        {
            return @"
            <html><head>
                <style>
                    body { margin: 0; padding: 0; background: url('https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?q=80&w=2564&auto=format&fit=crop') no-repeat center center fixed; background-size: cover; font-family: 'Segoe UI', sans-serif; display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100vh; overflow: hidden; }
                    .overlay { position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: rgba(10, 11, 20, 0.75); backdrop-filter: blur(25px); z-index: -1; }
                    h1 { color: #00f2ff; font-weight: 300; letter-spacing: 5px; font-size: 40px; text-shadow: 0 0 20px rgba(0, 242, 255, 0.5); margin-bottom: 50px; z-index: 1; }
                    
                    .app-grid { display: flex; gap: 40px; z-index: 1; flex-wrap: wrap; justify-content: center; max-width: 900px; }
                    
                    .app-card { width: 160px; height: 160px; background: rgba(255, 255, 255, 0.05); border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 30px; display: flex; flex-direction: column; align-items: center; justify-content: center; cursor: pointer; transition: 0.4s cubic-bezier(0.19, 1, 0.22, 1); text-decoration: none; box-shadow: 0 10px 30px rgba(0,0,0,0.5); }
                    .app-card:hover { transform: translateY(-10px) scale(1.05); background: rgba(0, 242, 255, 0.1); border-color: #00f2ff; box-shadow: 0 15px 40px rgba(0, 242, 255, 0.4); }
                    
                    .app-icon { font-size: 60px; margin-bottom: 15px; text-shadow: 0 5px 15px rgba(0,0,0,0.5); }
                    .app-name { color: white; font-size: 16px; font-weight: 600; letter-spacing: 1px; }

                    .yt-icon { color: #ff0000; }
                    .trt-icon { color: #00a8ff; }
                    .weather-icon { color: #ffdd00; }
                    .spot-icon { color: #1db954; }
                </style>
            </head><body>
                <div class='overlay'></div>
                <h1>ARK APP CENTER</h1>
                <div class='app-grid'>
                    <div class='app-card' onclick=""launchApp('https://www.youtube.com')"">
                        <div class='app-icon yt-icon'>▶</div><div class='app-name'>Video</div>
                    </div>
                    <div class='app-card' onclick=""launchApp('https://www.trthaber.com/canli-yayin-izle.html')"">
                        <div class='app-icon trt-icon'>📺</div><div class='app-name'>Canlı TV</div>
                    </div>
                    <div class='app-card' onclick=""launchApp('https://weather.com/tr-TR/weather/today/l/Istanbul')"">
                        <div class='app-icon weather-icon'>⛅</div><div class='app-name'>Hava Durumu</div>
                    </div>
                    <div class='app-card' onclick=""launchApp('https://open.spotify.com/')"">
                        <div class='app-icon spot-icon'>🎵</div><div class='app-name'>Şarkı</div>
                    </div>
                </div>
                <script>
                    function launchApp(url) {
                        window.chrome.webview.postMessage(url);
                    }
                </script>
            </body></html>";
        }

        private string GetLiquidHtml()
        {
            return @"
            <html><head>
                <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
                <link rel='stylesheet' href='https://unpkg.com/leaflet-routing-machine@latest/dist/leaflet-routing-machine.css' />
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                <script src='https://unpkg.com/leaflet-routing-machine@latest/dist/leaflet-routing-machine.js'></script>
                <style>
                    body { margin: 0; overflow: hidden; font-family: 'Segoe UI', sans-serif; background: #08080c; color: white; }
                    
                    ::-webkit-scrollbar { width: 8px; }
                    ::-webkit-scrollbar-track { background: rgba(0, 0, 0, 0.3); border-radius: 10px; }
                    ::-webkit-scrollbar-thumb { background: #00f2ff; border-radius: 10px; box-shadow: 0 0 5px #00f2ff; }
                    ::-webkit-scrollbar-thumb:hover { background: #0088ff; }

                    #map { height: 100vh; width: 100vw; z-index: 1; }
                    .dark-tile { filter: invert(100%) hue-rotate(180deg) brightness(95%) contrast(90%) saturate(20%); }
                    .leaflet-routing-container { display: none !important; }

                    #search-container { position: absolute; top: 30px; left: 30px; z-index: 1000; width: 400px; pointer-events: auto; }
                    #mapSearch { 
                        width: 100%; padding: 18px 25px; 
                        background: rgba(13, 14, 23, 0.9); 
                        border: 2px solid rgba(0, 242, 255, 0.2); 
                        border-radius: 12px; color: white; outline: none; 
                        backdrop-filter: blur(25px); font-size: 15px;
                        transition: 0.3s;
                    }
                    #mapSearch:focus { border-color: #00f2ff; box-shadow: 0 0 15px rgba(0, 242, 255, 0.3); }

                    #search-results { 
                        margin-top: 8px; background: rgba(13, 14, 23, 0.95); 
                        border-radius: 12px; max-height: 350px; overflow-y: auto; 
                        display: none; border: 1px solid rgba(0, 242, 255, 0.1); 
                    }

                    .ui-overlay { position: absolute; width: 100%; height: 100%; top: 0; left: 0; z-index: 100; pointer-events: none; }
                    .interactive { pointer-events: auto; }

                    #app-center-btn {
                        position: absolute; bottom: 40px; left: 30px; width: 65px; height: 65px;
                        background: rgba(15, 16, 28, 0.7); backdrop-filter: blur(30px);
                        border: 1px solid rgba(0, 242, 255, 0.3); border-radius: 20px;
                        display: flex; align-items: center; justify-content: center;
                        cursor: pointer; z-index: 1000; transition: 0.3s; box-shadow: 0 5px 15px rgba(0,0,0,0.5);
                    }
                    #app-center-btn:hover { background: rgba(0, 242, 255, 0.15); border-color: #00f2ff; box-shadow: 0 0 15px rgba(0, 242, 255, 0.5); transform: translateY(-2px); }

                    #safety-warning {
                        position: absolute; top: 120px; left: 50%; transform: translateX(-50%);
                        background: rgba(255, 0, 0, 0.85); color: white; padding: 12px 25px;
                        border-radius: 12px; font-weight: bold; font-size: 16px; letter-spacing: 1px;
                        display: none; z-index: 2000; box-shadow: 0 0 30px rgba(255,0,0,0.8);
                        backdrop-filter: blur(10px); border: 1px solid #ff4d4d;
                    }
                    
                    #hud-panel {
                        position: absolute; top: 30px; left: 50%; transform: translateX(-50%); 
                        background: rgba(10, 11, 20, 0.85); backdrop-filter: blur(25px); 
                        border: 1px solid #00f2ff; border-radius: 15px; padding: 15px 40px; 
                        display: none; gap: 40px; 
                        z-index: 2000; 
                        box-shadow: 0 10px 30px rgba(0,0,0,0.5); pointer-events: none; transition: 0.3s;
                    }
                    
                    #music-panel { 
                        position: absolute; top: 30px; right: -450px; 
                        width: 400px; height: calc(100vh - 250px); 
                        background: rgba(10, 11, 20, 0.85); 
                        backdrop-filter: blur(40px); 
                        border: 1px solid rgba(0, 242, 255, 0.2); 
                        border-radius: 20px; transition: 0.6s cubic-bezier(0.19, 1, 0.22, 1); 
                        padding: 25px; display: flex; flex-direction: column; 
                        z-index: 1000; box-sizing: border-box;
                    }
                    #music-panel.open { right: 30px; }
                    #musicInput { 
                        width: 100%; padding: 15px; 
                        background: rgba(255, 255, 255, 0.03); 
                        border: 1px solid rgba(0, 242, 255, 0.2); 
                        border-radius: 10px; color: white; outline: none; 
                    }
                    #music-results { flex-grow: 1; overflow-y: auto; margin-top: 20px; padding-right: 5px; }

                    .song-card { 
                        display: flex; align-items: center; padding: 12px; 
                        margin-bottom: 12px; background: rgba(255, 255, 255, 0.02); 
                        border-radius: 12px; cursor: pointer; transition: 0.3s; 
                        border-left: 0px solid #00f2ff;
                    }
                    .song-card:hover { background: rgba(0, 242, 255, 0.08); border-left: 4px solid #00f2ff; transform: scale(1.02); }

                    .bottom-menu { 
                        position: absolute; bottom: 40px; left: 50%; 
                        transform: translateX(-50%); width: 750px; height: 90px; 
                        background: rgba(15, 16, 28, 0.7); 
                        backdrop-filter: blur(30px); border-radius: 100px; 
                        border: 1px solid rgba(0, 242, 255, 0.15); 
                        display: flex; align-items: center; justify-content: space-around;
                        box-shadow: 0 10px 40px rgba(0,0,0,0.5);
                    }
                    .nav-icon { width: 32px; height: 32px; cursor: pointer; opacity: 0.4; transition: 0.4s; }
                    .nav-icon.active, .nav-icon:hover { opacity: 1; filter: drop-shadow(0 0 8px #00f2ff); }

                    .center-orb { 
                        width: 100px; height: 100px; 
                        background: radial-gradient(circle, #00f2ff 0%, #0088ff 100%); 
                        border-radius: 50%; display: flex; align-items: center; 
                        justify-content: center; cursor: pointer; 
                        box-shadow: 0 0 40px rgba(0, 242, 255, 0.5); 
                        transform: translateY(-20px); border: 4px solid #08080c;
                    }
                </style>
            </head>
            <body>
                <div id='search-container' class='interactive'>
                    <input type='text' id='mapSearch' placeholder='Yolculuk nereye?' oninput='searchPlace(this.value, false)'>
                    <div id='search-results'></div>
                </div>

                <div id='map'></div>
                <div class='ui-overlay'>

                    <div id='safety-warning'>Sürüş Güvenliği: Uygulamalar Kapatıldı!</div>

                    <div id='app-center-btn' class='interactive' onclick='triggerAppCenter()'>
                        <svg viewBox='0 0 24 24' width='30' fill='#00f2ff'><path d='M4 4h6v6H4zm10 0h6v6h-6zM4 14h6v6H4zm10 0h6v6h-6z'/></svg>
                    </div>

                    <div id='hud-panel'>
                        <div style='text-align: center;'>
                            <div style='font-size: 11px; color: #888; letter-spacing: 1px;'>HIZ</div>
                            <div style='font-size: 26px; font-weight: bold; color: white;'><span id='hud-speed'>0</span><span style='font-size:12px; color:#888; margin-left:3px;'>km/h</span></div>
                        </div>
                        <div style='text-align: center;'>
                            <div style='font-size: 11px; color: #888; letter-spacing: 1px;'>YAKIT</div>
                            <div style='font-size: 26px; font-weight: bold; color: #00f2ff;'>%<span id='hud-fuel'>84</span></div>
                        </div>
                        <div style='text-align: center;'>
                            <div style='font-size: 11px; color: #888; letter-spacing: 1px;'>MOTOR ISI</div>
                            <div style='font-size: 26px; font-weight: bold; color: #ff4d4d;'><span id='hud-temp'>22</span><span style='font-size:16px;'>°C</span></div>
                        </div>
                    </div>

                    <div id='music-panel' class='interactive'>
                        <h2 style='color:#00f2ff; margin:0 0 20px 0; font-weight:300; letter-spacing:2px;'>ARK AI MUSIC</h2>
                        <input type='text' id='musicInput' placeholder='Şarkı veya Sanatçı ara...' onkeydown='if(event.key==""Enter"") { searchYouTube(this.value, true); }'>
                        <div id='music-results' onscroll='checkScroll(this)'></div>
                    </div>

                    <div id='music-mini-player' class='interactive' style='position: absolute; bottom: 150px; right: 30px; width: 380px; height: 110px; background: rgba(13,14,23,0.95); backdrop-filter: blur(25px); border: 1px solid rgba(0,242,255,0.3); border-radius: 18px; display: none; align-items: center; padding: 20px;'>
                        <img id='mini-img' src='' style='width:70px; height:70px; border-radius:12px; object-fit:cover;'>
                        <div style='margin-left:20px; flex:1; overflow:hidden;'>
                            <h4 id='mini-title' style='margin:0; font-size:14px; color:#fff; white-space:nowrap; overflow:hidden; text-overflow:ellipsis;'>Hazır</h4>
                            <div style='width:100%; height:4px; background:rgba(255,255,255,0.1); margin-top:12px; border-radius:10px;'>
                                <div id='progress-bar' style='width:0%; height:100%; background: #00f2ff; box-shadow: 0 0 10px #00f2ff; transition: width 0.3s linear;'></div>
                            </div>
                        </div>
                        <div class='ctrl-btn' onclick='sendAction(""TOGGLE_PAUSE"")' id='playBtn' style='width:50px; height:50px; background:rgba(0,242,255,0.1); border-radius:50%; display:flex; align-items:center; justify-content:center; cursor:pointer; margin-left:15px; border:1px solid rgba(0,242,255,0.2);'>
                            <svg id='playIcon' viewBox='0 0 24 24' width='24' fill='#00f2ff'><path d='M8 5v14l11-7z'/></svg>
                        </div>
                    </div>

                    <div class='bottom-menu interactive'>
                        <img src='http://assets.local/map.png' class='nav-icon active' onclick='sendAction(""Map_Home"")'>
                        <img src='http://assets.local/music.png' class='nav-icon' onclick='sendAction(""Music_Click"")'>
                        <div class='center-orb' onclick='sendAction(""AI_Open"")'><img src='http://assets.local/ai.png' style='width:50px; filter: brightness(0) invert(1);'></div>
                        <img src='http://assets.local/profile.png' class='nav-icon' onclick='sendAction(""Profile_Click"")'>
                        <img src='http://assets.local/settings.png' class='nav-icon' onclick='sendAction(""Settings_Click"")'>
                    </div>
                </div>

                <script>
                    var userPos = [41.0082, 28.9784];
                    var map = L.map('map', {zoomControl:false, attributionControl: false}).setView(userPos, 16);
                    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { className: 'dark-tile' }).addTo(map);

                    var userMarker = L.marker(userPos, { icon: L.divIcon({ className: 'user-location-marker' }) }).addTo(map);
                    var routingControl = null;

                    var fuel = 84.0; var temp = 22.0; var simInterval = null; window.speedLimit = 120; var currentSpeed = 0;

                    function triggerAppCenter() {
                        if (currentSpeed > 15) {
                            showSafetyWarning();
                        } else {
                            sendAction('APP_CENTER_OPEN'); 
                        }
                    }

                    function showSafetyWarning() {
                        var warn = document.getElementById('safety-warning');
                        warn.style.display = 'block';
                        setTimeout(() => { warn.style.display = 'none'; }, 3000); 
                    }

                    function startVehicleSimulation() {
                        if(simInterval) return;
                        
                        document.getElementById('hud-panel').style.display = 'flex';
                        
                        simInterval = setInterval(() => {
                            fuel -= 0.05; temp += 0.15;
                            if(temp > 90) temp = 90;
                            if(fuel < 0) fuel = 0;
                            
                            currentSpeed += (Math.random() * 12 - 3);
                            if(currentSpeed < 0) currentSpeed = 0;
                            if(currentSpeed > 180) currentSpeed -= 10;

                            document.getElementById('hud-speed').innerText = Math.floor(currentSpeed);
                            document.getElementById('hud-fuel').innerText = fuel.toFixed(1);
                            document.getElementById('hud-temp').innerText = temp.toFixed(0);

                            if(currentSpeed > window.speedLimit) {
                                document.getElementById('hud-panel').style.boxShadow = '0 0 30px rgba(255,0,0,0.8)';
                                document.getElementById('hud-panel').style.borderColor = 'red';
                            } else {
                                document.getElementById('hud-panel').style.boxShadow = '0 10px 30px rgba(0,0,0,0.5)';
                                document.getElementById('hud-panel').style.borderColor = '#00f2ff';
                            }

                            if (currentSpeed > 15) {
                                sendAction('APP_CENTER_FORCE_CLOSE');
                            }

                        }, 1000);
                    }

                    function stopVehicleSimulation() {
                        clearInterval(simInterval);
                        simInterval = null;
                        document.getElementById('hud-panel').style.display = 'none';
                        currentSpeed = 0; 
                    }

                    async function searchPlace(val, autoSelect = false) {
                        const resDiv = document.getElementById('search-results');
                        if (val.length < 3) { resDiv.style.display = 'none'; return; }
                        try {
                            const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${val}&lat=${userPos[0]}&lon=${userPos[1]}&limit=5&bounded=1&viewbox=${userPos[1]-0.01},${userPos[0]+0.01},${userPos[1]+0.01},${userPos[0]-0.01}`);
                            const data = await res.json();
                            resDiv.innerHTML = '';
                            
                            if (autoSelect) {
                                if(data.length > 0) {
                                    document.getElementById('mapSearch').value = data[0].display_name;
                                    setRoute(data[0].lat, data[0].lon);
                                    resDiv.style.display = 'none';
                                }
                            } else {
                                resDiv.style.display = 'block';
                                data.forEach(item => {
                                    const div = document.createElement('div');
                                    div.style = 'padding:15px; border-bottom:1px solid rgba(255,255,255,0.05); cursor:pointer; font-size:13px;';
                                    div.innerText = item.display_name;
                                    div.onclick = () => {
                                        setRoute(item.lat, item.lon);
                                        resDiv.style.display = 'none';
                                        document.getElementById('mapSearch').value = item.display_name;
                                    };
                                    resDiv.appendChild(div);
                                });
                            }
                        } catch(e) {}
                    }

                    function setRoute(lat, lon) {
                        if (routingControl) map.removeControl(routingControl);
                        routingControl = L.Routing.control({
                            waypoints: [L.latLng(userPos[0], userPos[1]), L.latLng(lat, lon)],
                            lineOptions: { styles: [{ color: '#00f2ff', opacity: 0.8, weight: 6 }] },
                            createMarker: function() { return null; },
                            addWaypoints: false,
                            routeWhileDragging: false
                        }).addTo(map);
                        map.flyTo([lat, lon], 15);
                    }

                    let nextPageToken = '';
                    let currentQuery = '';
                    let isSearching = false;

                    async function searchYouTube(query, isNewSearch = false, autoPlay = false) {
                        if(isSearching || !query) return;
                        isSearching = true;
                        currentQuery = query;
                        
                        if (autoPlay) {
                            var pnl = document.getElementById('music-panel');
                            if (!pnl.classList.contains('open')) {
                                pnl.classList.add('open');
                            }
                        }

                        const resultsContainer = document.getElementById('music-results');
                        if(isNewSearch) { resultsContainer.innerHTML = '<center>Sistem taranıyor...</center>'; nextPageToken = ''; }
                        try {
                            const url = `https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=20&q=${encodeURIComponent(query)}&type=video&videoCategoryId=10&pageToken=${nextPageToken}&key=" + youtubeApiKey + @"`;
                            const res = await fetch(url);
                            const data = await res.json();
                            if(isNewSearch) resultsContainer.innerHTML = '';
                            nextPageToken = data.nextPageToken || '';
                            
                            if (autoPlay && data.items.length > 0) {
                                let firstItem = data.items[0];
                                if (firstItem.id.videoId) {
                                    document.getElementById('mini-title').innerText = firstItem.snippet.title;
                                    document.getElementById('mini-img').src = firstItem.snippet.thumbnails.high.url;
                                    document.getElementById('music-mini-player').style.display = 'flex';
                                    setBtnState(true);
                                    sendAction('PLAY_ID:' + firstItem.id.videoId);
                                }
                            }

                            data.items.forEach(item => {
                                if(!item.id.videoId) return;
                                const card = document.createElement('div');
                                card.className = 'song-card';
                                card.innerHTML = `<img src='${item.snippet.thumbnails.medium.url}'><div style='flex:1; overflow:hidden;'><div style='font-weight:bold; font-size:14px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis;'>${item.snippet.title}</div><div style='color:#00f2ff; font-size:11px; opacity:0.6; margin-top:4px;'>${item.snippet.channelTitle}</div></div>`;
                                card.onclick = () => {
                                    document.getElementById('mini-title').innerText = item.snippet.title;
                                    document.getElementById('mini-img').src = item.snippet.thumbnails.high.url;
                                    document.getElementById('music-mini-player').style.display = 'flex';
                                    setBtnState(true);
                                    sendAction('PLAY_ID:' + item.id.videoId);
                                };
                                resultsContainer.appendChild(card);
                            });
                        } catch(e) { console.error(e); }
                        isSearching = false;
                    }

                    function checkScroll(e) {
                        if (e.scrollTop + e.clientHeight >= e.scrollHeight - 50 && nextPageToken && !isSearching) {
                            searchYouTube(currentQuery, false);
                        }
                    }

                    function updateProgress(curr, total) {
                        const pBar = document.getElementById('progress-bar');
                        if(pBar && total > 0) {
                            const percent = (curr / total) * 100;
                            pBar.style.width = percent + '%';
                        }
                    }

                    function setBtnState(isPlaying) {
                        const icon = document.getElementById('playIcon');
                        if(!icon) return;
                        icon.innerHTML = isPlaying ? '<path d=""M6 19h4V5H6v14zm8-14v14h4V5h-4z\""/>' : '<path d=""M8 5v14l11-7z\""/>';
                    }

                    function toggleMusicPanel() { document.getElementById('music-panel').classList.toggle('open'); }
                    function sendAction(m) { window.chrome.webview.postMessage(m); }
                </script>
            </body></html>";
        }
    }
}