# WebSiteChecker

Birden fazla web sitesini periyodik olarak kontrol eden ve erişilemediğinde e-posta ile bildiren Windows masaüstü uygulaması.

## Özellikler

- Birden fazla site tanımlama (URL, kontrol aralığı, zaman aşımı, beklenen HTTP kodu)
- Arka planda otomatik HTTP kontrolü (HEAD, gerekirse GET)
- Site erişilemediğinde SMTP ile e-posta bildirimi
- Site tekrar erişilebilir olunca bilgilendirme maili
- Sistem tepsisinde (tray) çalışma — pencere kapatılsa bile izleme devam eder
- Windows ile otomatik başlatma seçeneği

## Gereksinimler

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Kurulum ve Çalıştırma

```bash
cd src/WebSiteChecker
dotnet run
```

Release derlemesi:

```bash
dotnet publish src/WebSiteChecker -c Release -r win-x64 --self-contained false
```

## İlk Kurulum

1. Uygulamayı başlatın.
2. **SMTP Ayarları** menüsünden mail sunucunuzu yapılandırın:
   - **SMTP Sunucu:** örn. `mail.example.com`
   - **Port:** genelde `587` (STARTTLS) veya `465` (SSL)
   - **SSL kullan:** sunucunuza göre işaretleyin
   - **Kullanıcı adı / Şifre:** SMTP kimlik bilgileriniz
   - **Gönderen / Alıcı:** bildirim maili adresleri
3. **Test Maili Gönder** ile ayarları doğrulayın.
4. **Site Ekle** ile izlemek istediğiniz siteleri tanımlayın.

## Veri Konumu

Ayarlar ve site listesi şu klasörde saklanır:

```
%AppData%\WebSiteChecker\
├── sites.json
├── smtp-settings.json
├── smtp-password.dat   (DPAPI ile şifrelenmiş)
└── monitor-state.json
```

## Tray Davranışı

- Pencereyi kapatmak (X) uygulamayı sonlandırmaz; tray ikonunda kalır.
- Tray menüsü: **Aç**, **Duraklat / Devam**, **Çıkış**
- Tamamen kapatmak için tray menüsünden **Çıkış** seçin.

## Bildirim Kuralları

- Site erişilemez olunca **bir kez** down maili gönderilir.
- Site hâlâ erişilemezken tekrar mail gönderilmez.
- Site tekrar erişilebilir olunca (ayar açıksa) recovery maili gönderilir.
- Aynı site kısa süre içinde tekrar down olursa, bekleme süresi (varsayılan 30 dk) dolmadan yeni down maili gönderilmez.
