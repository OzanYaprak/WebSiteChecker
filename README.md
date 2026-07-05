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
2. **SMTP Ayarları** menüsünden mail sunucunuzu yapılandırın.
3. **Test Maili Gönder** ile ayarları doğrulayın.
4. **Site Ekle** ile izlemek istediğiniz siteleri tanımlayın.

## SMTP Ayarları

Uygulama site erişilemediğinde e-posta göndermek için SMTP kullanır. Ayarlar **SMTP Ayarları** penceresinden veya `%AppData%\WebSiteChecker\smtp-settings.json` dosyasından yapılandırılabilir. Şifre ayrı olarak `smtp-password.dat` dosyasında DPAPI ile şifrelenmiş biçimde saklanır.

### Genel alanlar

| Alan | Açıklama |
|------|----------|
| SMTP Sunucu | Mail sunucusu adresi (ör. `smtp.gmail.com`) |
| Port | Genelde `587` (STARTTLS) veya `465` (SSL) |
| SSL kullan | Sunucuya göre işaretleyin; Gmail için **Evet** |
| Kullanıcı adı | SMTP kimlik doğrulama e-posta adresi |
| Şifre | SMTP şifresi veya uygulama parolası |
| Gönderen | Bildirimlerin gönderileceği e-posta adresi |
| Alıcı | Bildirimlerin iletileceği e-posta adresi |
| Uyarı bekleme (dk) | Aynı site için tekrar down maili bekleme süresi (varsayılan: 30) |
| Site tekrar erişilebilir olunca mail gönder | Recovery bildirimi açık/kapalı |

### Gmail (Google) ile test

Uygulama Google hesabınızla **OAuth ile giriş yapmaz**; SMTP üzerinden mail gönderir. Bu nedenle normal Google şifreniz çalışmaz — **Uygulama Parolası** gerekir.

#### Gmail SMTP değerleri

| Alan | Değer |
|------|-------|
| SMTP Sunucu | `smtp.gmail.com` |
| Port | `587` |
| SSL kullan | Evet |
| Kullanıcı adı | Gmail adresiniz (ör. `sizin@gmail.com`) |
| Şifre | 16 haneli Google uygulama parolası |
| Gönderen | Gmail adresiniz |
| Alıcı | Bildirim alacağınız adres (test için aynı Gmail olabilir) |
| Uyarı bekleme (dk) | `30` (isteğe bağlı) |
| Site tekrar erişilebilir olunca mail gönder | Evet (isteğe bağlı) |

#### Uygulama parolası oluşturma

1. Google hesabınızda [2 adımlı doğrulamayı](https://myaccount.google.com/signinoptions/two-step-verification) açın.
2. [Google Uygulama Parolaları](https://myaccount.google.com/apppasswords) sayfasına gidin.
3. **Mail** veya **Diğer (özel ad)** seçeneğiyle 16 haneli parola oluşturun (ör. `abcd efgh ijkl mnop`).
4. Bu parolayı normal Google şifreniz yerine SMTP **Şifre** alanına yapıştırın.

#### Uygulama içinden kurulum

1. Ana ekranda **SMTP Ayarları**'na tıklayın.
2. **Gmail (Google) ön ayarı** butonuna basın (sunucu, port ve SSL otomatik dolar).
3. **Kullanıcı adı**, **Gönderen** ve **Alıcı** alanlarına Gmail adresinizi yazın.
4. **Şifre** alanına uygulama parolasını yapıştırın.
5. **Kaydet**'e basın.
6. Ana ekranda **Test Maili Gönder** ile doğrulayın.

#### Örnek `smtp-settings.json` (Gmail)

Proje içindeki örnek dosya: `config/smtp-settings.gmail.example.json`

```json
{
  "host": "smtp.gmail.com",
  "port": 587,
  "useSsl": true,
  "username": "sizin@gmail.com",
  "fromAddress": "sizin@gmail.com",
  "toAddress": "sizin@gmail.com",
  "alertCooldownMinutes": 30,
  "sendRecoveryEmail": true
}
```

> **Not:** Şifre bu JSON dosyasına yazılmaz. Şifreyi yalnızca uygulama içindeki **SMTP Ayarları** penceresinden kaydedin; `%AppData%\WebSiteChecker\smtp-password.dat` dosyasında şifrelenmiş olarak saklanır.

#### Sık karşılaşılan hatalar

| Hata | Olası neden | Çözüm |
|------|-------------|-------|
| Kimlik doğrulama başarısız | Normal Google şifresi kullanıldı | Uygulama parolası oluşturup onu kullanın |
| Bağlantı reddedildi | Yanlış port veya SSL ayarı | Port `587`, SSL **açık** olmalı |
| Gönderen adresi reddedildi | Gönderen, giriş yapılan Gmail ile uyuşmuyor | Gönderen ve kullanıcı adını aynı Gmail yapın |

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
