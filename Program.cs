// Bu kodun tamamını projenizdeki Program.cs dosyasına yapıştırın.
// Diğer tüm form dosyalarını (Form1.cs, Form1.Designer.cs) sildiğinizden emin olun.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TekDosyaWinFormsChat
{
    /// <summary>
    /// Programın ana giriş noktası.
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChatForm());
        }
    }

    /// <summary>
    /// Tüm chat mantığını ve form tasarımını içeren ana sınıf.
    /// </summary>
    public class ChatForm : Form
    {
        // --- Arayüz Kontrolleri ---
        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtNickname;
        private TextBox txtChatLog;
        private TextBox txtMessage;
        private Button btnStartServer;
        private Button btnConnect;
        private Button btnSend;

        // --- Ağ Değişkenleri ---
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;
        private readonly List<TcpClient> clientList = new List<TcpClient>();

        public ChatForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Tek Dosya Chat Uygulaması";
            this.Size = new Size(650, 500);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblIp = new Label { Text = "IP Adresi:", Location = new Point(12, 15) };
            txtIp = new TextBox { Text = "127.0.0.1", Location = new Point(12, 35), Size = new Size(120, 20) };
            Label lblPort = new Label { Text = "Port:", Location = new Point(142, 15) };
            txtPort = new TextBox { Text = "12345", Location = new Point(142, 35), Size = new Size(60, 20) };
            Label lblNickname = new Label { Text = "Takma Ad:", Location = new Point(212, 15) };
            txtNickname = new TextBox { Text = "Kullanici" + new Random().Next(1, 100), Location = new Point(212, 35), Size = new Size(120, 20) };

            btnStartServer = new Button { Text = "Sunucu Olarak Başlat", Location = new Point(350, 33), Size = new Size(130, 23) };
            btnConnect = new Button { Text = "İstemci Olarak Bağlan", Location = new Point(490, 33), Size = new Size(130, 23) };
            txtChatLog = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Location = new Point(12, 70), Size = new Size(610, 320) };
            txtMessage = new TextBox { Location = new Point(12, 400), Size = new Size(500, 20), Enabled = false };
            btnSend = new Button { Text = "Gönder", Location = new Point(522, 398), Size = new Size(100, 23), Enabled = false };

            btnStartServer.Click += BtnStartServer_Click;
            btnConnect.Click += BtnConnect_Click;
            btnSend.Click += BtnSend_Click;
            this.FormClosing += ChatForm_FormClosing;

            this.Controls.Add(lblIp); this.Controls.Add(txtIp);
            this.Controls.Add(lblPort); this.Controls.Add(txtPort);
            this.Controls.Add(lblNickname); this.Controls.Add(txtNickname);
            this.Controls.Add(btnStartServer); this.Controls.Add(btnConnect);
            this.Controls.Add(txtChatLog); this.Controls.Add(txtMessage);
            this.Controls.Add(btnSend);
        }

        // --- OLAY METOTLARI (EVENT HANDLERS) ---

        private void BtnStartServer_Click(object sender, EventArgs e)
        {
            btnStartServer.Enabled = false;
            btnConnect.Enabled = false;
            txtIp.Enabled = false;
            txtPort.Enabled = false;
            txtNickname.Enabled = false;
            this.Text = "CHAT SUNUCUSU";
            Task.Run(() => StartServer());
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNickname.Text))
            {
                MessageBox.Show("Lütfen bir takma ad girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            btnStartServer.Enabled = false;
            btnConnect.Enabled = false;
            txtIp.Enabled = false;
            txtPort.Enabled = false;
            txtNickname.Enabled = false;
            this.Text = $"CHAT İSTEMCİSİ - {txtNickname.Text}";
            Task.Run(() => ConnectToServer());
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            if (stream != null && !string.IsNullOrEmpty(txtMessage.Text))
            {
                string fullMessage = $"{txtNickname.Text}: {txtMessage.Text}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(fullMessage);
                try
                {
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    AppendTextToChatLog($"Ben: {txtMessage.Text}");
                    txtMessage.Clear();
                }
                catch (Exception ex)
                {
                    AppendTextToChatLog($"Hata: Mesaj gönderilemedi. {ex.Message}");
                }
            }
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            client?.Close();
            server?.Stop();
        }

        // --- AĞ MANTIĞI (NETWORKING LOGIC) ---

        private async Task StartServer()
        {
            try
            {
                int port = int.Parse(txtPort.Text);
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                AppendTextToChatLog($"Sunucu başlatıldı. Port {port} dinleniyor...");

                while (true)
                {
                    TcpClient connectedClient = await server.AcceptTcpClientAsync();
                    clientList.Add(connectedClient);
                    AppendTextToChatLog("Yeni bir istemci bağlandı.");
                    Task.Run(() => HandleClient(connectedClient));
                }
            }
            catch (Exception ex) // DEĞİŞTİRİLDİ: Hata yakalama bloğu güncellendi.
            {
                string errorMessage = $"Sunucu hatası: {ex.Message}";
                AppendTextToChatLog(errorMessage);
                // Kullanıcıya hatayı bir mesaj kutusu ile göster.
                ShowErrorMessage(errorMessage);
                // Hata sonrası arayüzü tekrar eski haline getir.
                ResetInitialControls();
            }
        }

        private async Task HandleClient(TcpClient tcpClient)
        {
            NetworkStream clientStream = tcpClient.GetStream();
            byte[] buffer = new byte[4096];
            try
            {
                while (true)
                {
                    int bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    BroadcastMessage(message, tcpClient);
                }
            }
            catch { /* Bağlantı koptu */ }
            finally
            {
                clientList.Remove(tcpClient);
                tcpClient.Close();
                AppendTextToChatLog("Bir istemcinin bağlantısı kesildi.");
                BroadcastMessage("Sistem: Bir kullanıcı sohbetten ayrıldı.", null);
            }
        }

        private void BroadcastMessage(string message, TcpClient sender)
        {
            AppendTextToChatLog(message); // Sunucu kendi ekranında da mesajları görsün
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            foreach (var connectedClient in clientList)
            {
                if (connectedClient != sender)
                {
                    try { connectedClient.GetStream().Write(messageBytes, 0, messageBytes.Length); }
                    catch { /* Hata olursa o istemciyi görmezden gel */ }
                }
            }
        }

        private async Task ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(txtIp.Text, int.Parse(txtPort.Text));
                stream = client.GetStream();

                AppendTextToChatLog("Sunucuya başarıyla bağlandı!");
                SetSendEnabled(true);
                await ReceiveMessages();
            }
            catch (Exception ex) // DEĞİŞTİRİLDİ: Hata yakalama bloğu güncellendi.
            {
                string errorMessage = $"Bağlantı hatası: {ex.Message}";
                AppendTextToChatLog(errorMessage);
                // Kullanıcıya hatayı bir mesaj kutusu ile göster.
                ShowErrorMessage(errorMessage);
                // Hata sonrası arayüzü tekrar eski haline getir.
                ResetInitialControls();
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[4096];
            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        AppendTextToChatLog("Sunucu ile bağlantı kesildi.");
                        break;
                    }
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AppendTextToChatLog(message);
                }
            }
            catch (Exception ex)
            {
                AppendTextToChatLog($"Sunucu ile bağlantı koptu: {ex.Message}");
            }
            finally
            {
                SetSendEnabled(false);
                client.Close();
            }
        }

        // --- YARDIMCI METOTLAR (HELPER METHODS) ---

        private void AppendTextToChatLog(string text)
        {
            if (txtChatLog.InvokeRequired)
            {
                txtChatLog.Invoke(new Action<string>(AppendTextToChatLog), text);
            }
            else
            {
                txtChatLog.AppendText(text + Environment.NewLine);
            }
        }

        private void SetSendEnabled(bool enabled)
        {
            if (txtMessage.InvokeRequired || btnSend.InvokeRequired)
            {
                this.Invoke(new Action<bool>(SetSendEnabled), enabled);
            }
            else
            {
                txtMessage.Enabled = enabled;
                btnSend.Enabled = enabled;
            }
        }

        // YENİ EKLENDİ: Hata durumunda başlangıç kontrollerini tekrar aktif eden metot.
        private void ResetInitialControls()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ResetInitialControls));
            }
            else
            {
                btnStartServer.Enabled = true;
                btnConnect.Enabled = true;
                txtIp.Enabled = true;
                txtPort.Enabled = true;
                txtNickname.Enabled = true;
                this.Text = "Tek Dosya Chat Uygulaması";
            }
        }

        // YENİ EKLENDİ: Thread-safe (farklı iş parçacıklarından güvenle çağrılabilir) hata mesajı kutusu gösteren metot.
        private void ShowErrorMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(ShowErrorMessage), message);
            }
            else
            {
                MessageBox.Show(this, message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}