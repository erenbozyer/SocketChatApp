// Bu kodun tamamını projenizdeki Program.cs dosyasına yapıştırın.
// Diğer tüm form dosyalarını (Form1.cs, Form1.Designer.cs) sildiğinizden emin olun.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TekDosyaWinFormsChat
{
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

    public class ChatForm : Form
    {
        // --- Arayüz Kontrolleri ---
        private TextBox txtIp, txtPort, txtNickname, txtChatLog, txtMessage;
        private Button btnStartServer, btnConnect, btnSend;

        // --- Ağ Değişkenleri ---
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;

        // YENİ: Sunucu tarafında, istemcileri ve takma adlarını saklamak için bir sözlük (Dictionary) yapısı.
        // Bu yapı, takma adların benzersiz olmasını kontrol etmek için kullanılacak.
        private readonly Dictionary<TcpClient, string> clientNicknames = new Dictionary<TcpClient, string>();
        private readonly object _lock = new object(); // Thread-safe sözlük erişimi için kilit nesnesi

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

            int portStartX = 12 + 120 + 10;
            int nicknameStartX = portStartX + 60 + 10;

            Label lblIp = new Label { Text = "IP Adresi:", Location = new Point(12, 15), AutoSize = true };
            txtIp = new TextBox { Text = "127.0.0.1", Location = new Point(12, 35), Size = new Size(120, 20) };
            Label lblPort = new Label { Text = "Port:", Location = new Point(portStartX, 15), AutoSize = true };
            txtPort = new TextBox { Text = "12345", Location = new Point(portStartX, 35), Size = new Size(60, 20) };
            Label lblNickname = new Label { Text = "Takma Ad:", Location = new Point(nicknameStartX, 15), AutoSize = true };
            txtNickname = new TextBox { Text = "Kullanici" + new Random().Next(1, 100), Location = new Point(nicknameStartX, 35), Size = new Size(120, 20) };
            btnStartServer = new Button { Text = "Sunucu Olarak Başlat", Location = new Point(350, 33), Size = new Size(130, 23) };
            btnConnect = new Button { Text = "İstemci Olarak Bağlan", Location = new Point(490, 33), Size = new Size(130, 23) };
            txtChatLog = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Location = new Point(12, 70), Size = new Size(610, 320) };
            txtMessage = new TextBox { Location = new Point(12, 400), Size = new Size(500, 20), Enabled = false };
            btnSend = new Button { Text = "Gönder", Location = new Point(522, 398), Size = new Size(100, 23), Enabled = false };

            btnStartServer.Click += BtnStartServer_Click;
            btnConnect.Click += BtnConnect_Click;
            btnSend.Click += BtnSend_Click;
            this.FormClosing += ChatForm_FormClosing;

            this.Controls.AddRange(new Control[] { lblIp, txtIp, lblPort, txtPort, lblNickname, txtNickname, btnStartServer, btnConnect, txtChatLog, txtMessage, btnSend });
        }

        // --- OLAY METOTLARI ---

        private void BtnStartServer_Click(object sender, EventArgs e)
        {
            SetControlsForConnection(false);
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
            SetControlsForConnection(false);
            this.Text = $"CHAT İSTEMCİSİ - {txtNickname.Text}";
            Task.Run(() => ConnectToServer());
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            if (stream != null && !string.IsNullOrEmpty(txtMessage.Text))
            {
                // Artık mesajı direkt göndermek yerine bir protokol kullanıyoruz: "MSG|mesaj metni"
                string messageToSend = $"MSG|{txtMessage.Text}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
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

        // --- AĞ MANTIĞI (SERVER) ---

        private async Task StartServer()
        {
            try
            {
                int port = int.Parse(txtPort.Text);
                server = new TcpListener(IPAddress.Any, port);
                server.Start();

                // YENİ: Sunucu başlangıç bilgilendirme mesajları.
                string localIp = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
                AppendTextToChatLog($"Sunucu {localIp}:{port} adresinde başlatıldı.");
                AppendTextToChatLog("Bu uygulama sunucu olarak ayarlanmıştır.");
                AppendTextToChatLog("Sohbete katılmak için başka bir istemci açıp bu sunucuya bağlanın.");
                AppendTextToChatLog("------------------------------------------------------");

                while (true)
                {
                    TcpClient connectedClient = await server.AcceptTcpClientAsync();
                    // Her istemci için ayrı bir dinleyici başlat
                    Task.Run(() => HandleClient(connectedClient));
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Sunucu hatası: {ex.Message}");
                ResetInitialControls();
            }
        }

        private async Task HandleClient(TcpClient tcpClient)
        {
            NetworkStream clientStream = tcpClient.GetStream();
            byte[] buffer = new byte[4096];
            string nickname = null;

            try
            {
                // YENİ: İstemciden gelen ilk mesajın takma ad olmasını bekle
                int bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                string initialMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Gelen mesaj "NICK|kullaniciadi" formatında mı diye kontrol et
                if (initialMessage.StartsWith("NICK|"))
                {
                    nickname = initialMessage.Substring(5);
                    lock (_lock)
                    {
                        if (clientNicknames.ContainsValue(nickname))
                        {
                            // Takma ad zaten alınmış, istemciye hata gönder ve bağlantıyı kapat.
                            byte[] errorMsg = Encoding.UTF8.GetBytes("CMD|NICK_TAKEN");
                            clientStream.Write(errorMsg, 0, errorMsg.Length);
                            tcpClient.Close();
                            return; // Bu istemci için işlemi sonlandır.
                        }
                        // Takma ad uygun, listeye ekle.
                        clientNicknames.Add(tcpClient, nickname);
                    }

                    // Herkese yeni kullanıcının katıldığını duyur.
                    string joinMessage = $"SİSTEM: '{nickname}' sohbete katıldı.";
                    AppendTextToChatLog(joinMessage);
                    BroadcastMessage(joinMessage, null); // null göndererek herkese gitmesini sağla
                }
                else
                {
                    // Protokole uymayan bağlantıyı kapat.
                    tcpClient.Close();
                    return;
                }

                // Normal mesajlaşma döngüsü
                while ((bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string incomingMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (incomingMessage.StartsWith("MSG|"))
                    {
                        string chatMessage = $"{nickname}: {incomingMessage.Substring(4)}";
                        AppendTextToChatLog(chatMessage);
                        BroadcastMessage(chatMessage, tcpClient);
                    }
                }
            }
            catch { /* Bağlantı koptu */ }
            finally
            {
                // İstemci ayrıldığında yapılacaklar
                if (nickname != null)
                {
                    lock (_lock)
                    {
                        clientNicknames.Remove(tcpClient);
                    }
                    string leaveMessage = $"SİSTEM: '{nickname}' sohbetten ayrıldı.";
                    AppendTextToChatLog(leaveMessage);
                    BroadcastMessage(leaveMessage, null);
                }
                tcpClient.Close();
            }
        }

        // Sunucudaki tüm istemcilere mesaj yayan metot
        private void BroadcastMessage(string message, TcpClient sender)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            lock (_lock)
            {
                foreach (var connectedClient in clientNicknames.Keys)
                {
                    if (connectedClient != sender)
                    {
                        try { connectedClient.GetStream().Write(messageBytes, 0, messageBytes.Length); }
                        catch { /* Hata olursa o istemciyi görmezden gel */ }
                    }
                }
            }
        }

        // --- AĞ MANTIĞI (CLIENT) ---

        private async Task ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(txtIp.Text, int.Parse(txtPort.Text));
                stream = client.GetStream();

                // YENİ: Bağlanır bağlanmaz sunucuya takma adımızı bildiriyoruz.
                byte[] nickMessage = Encoding.UTF8.GetBytes($"NICK|{txtNickname.Text}");
                await stream.WriteAsync(nickMessage, 0, nickMessage.Length);

                SetSendEnabled(true);
                await ReceiveMessages(); // Mesajları dinlemeye başla
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Bağlantı hatası: {ex.Message}");
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
                    if (bytesRead == 0) break; // Sunucu bağlantıyı kapattı

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // YENİ: Sunucudan özel komut gelip gelmediğini kontrol et
                    if (message == "CMD|NICK_TAKEN")
                    {
                        ShowErrorMessage("Bu takma ad zaten kullanılıyor. Lütfen farklı bir ad seçin.");
                        ResetInitialControls();
                        client.Close();
                        return;
                    }

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
                client?.Close();
            }
        }

        // --- YARDIMCI METOTLAR ---

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

        private void SetControlsForConnection(bool enabled)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(SetControlsForConnection), enabled);
                return;
            }
            btnStartServer.Enabled = enabled;
            btnConnect.Enabled = enabled;
            txtIp.Enabled = enabled;
            txtPort.Enabled = enabled;
            txtNickname.Enabled = enabled;
        }

        private void SetSendEnabled(bool enabled)
        {
            if (txtMessage.InvokeRequired)
            {
                this.Invoke(new Action<bool>(SetSendEnabled), enabled);
            }
            else
            {
                txtMessage.Enabled = enabled;
                btnSend.Enabled = enabled;
            }
        }

        private void ResetInitialControls()
        {
            SetControlsForConnection(true);
            if (this.InvokeRequired) this.Invoke(new Action(() => this.Text = "Tek Dosya Chat Uygulaması"));
            else this.Text = "Tek Dosya Chat Uygulaması";
        }

        private void ShowErrorMessage(string message)
        {
            if (this.InvokeRequired) this.Invoke(new Action<string>(ShowErrorMessage), message);
            else MessageBox.Show(this, message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}