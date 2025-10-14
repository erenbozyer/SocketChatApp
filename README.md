# SocketChatApp
# ✨ C# WinForms Sohbet Uygulaması

Bu proje, **tek bir C# dosyası** içerisinde geliştirilmiş, veritabanı gerektirmeyen, çok kullanıcılı bir TCP soket sohbet uygulamasıdır. Hem sunucu (Server) hem de istemci (Client) olarak çalışabilir. C# ile soket programlama ve arayüz tasarımının kodla nasıl yapılabileceğini öğrenmek için harika bir başlangıç noktasıdır.

![Uygulama Ekran Görüntüsü](https://prnt.sc/P5OPmuF0uflt)

---

## 🚀 Özellikler

* **Sunucu & İstemci Mimarisi:** Uygulama hem sohbeti yöneten bir sunucu hem de sohbete katılan bir istemci olarak başlatılabilir.
* **Çoklu Kullanıcı Desteği:** Sunucu, aynı anda birden fazla istemcinin sohbet etmesine olanak tanır.
* **Benzersiz Kullanıcı Adı:** Sunucu, her kullanıcının benzersiz bir takma ada sahip olmasını sağlar. Aynı isimle ikinci bir bağlantı denemesi engellenir.
* **Sistem Mesajları:** Bir kullanıcı sohbete katıldığında veya ayrıldığında tüm kullanıcılara bilgilendirme mesajı gönderilir.
* **Veritabanı Yok:** Mesajlar hiçbir yere kaydedilmez, tüm sohbet anlık olarak bellekte (in-memory) yaşanır.
* **Hata Yönetimi:** Sunucuya bağlanılamaması gibi durumlarda kullanıcıya hata mesajı gösterilir ve arayüz kilitli kalmaz.

---

## 🔧 Nasıl Kullanılır?

Uygulamayı çalıştırmak çok basittir:

1.  **Sunucuyu Başlat:**
    * Uygulamayı ilk kez çalıştırın.
    * IP ve Port ayarlarını varsayılan olarak bırakın (`127.0.0.1` ve `12345`).
    * **"Sunucu Olarak Başlat"** butonuna tıklayın. Sunucu artık bağlantıları dinlemeye hazırdır.

2.  **İstemcileri Bağla:**
    * Uygulamanın `.exe` dosyasını tekrar çalıştırarak yeni pencereler açın.
    * Her yeni pencerede kendinize özel bir **Takma Ad** girin.
    * **"İstemci Olarak Bağlan"** butonuna tıklayın.
    * Artık sohbet etmeye başlayabilirsiniz!

> **💡 İpucu:** Tüm testleri kendi bilgisayarınızda yapıyorsanız, IP adresi olarak her zaman `127.0.0.1` (localhost) kullanabilirsiniz.

---

## 🛠️ Kullanılan Teknolojiler

* **C#**
* **Windows Forms (.NET Framework / .NET 8)**
* **System.Net.Sockets (TCP)**

---

---

Bu proje, öğrenme ve deneme amacıyla oluşturulmuştur. Keyifli kodlamalar!
