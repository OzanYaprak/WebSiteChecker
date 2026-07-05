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
├── ui-settings.json    (tema tercihi)
└── monitor-state.json
```

## Güvenlik

Uygulama, kullanıcı girdisi ve ağ istekleri için savunma katmanları içerir. Aşağıdaki önlemler `Helpers/` ve `Services/` altındaki bileşenlerle uygulanmıştır.

### SSRF koruması (Server-Side Request Forgery)

Site URL'leri yalnızca genel internet adreslerine yönlendirilebilir. İç ağ ve yerel hedeflere istek gönderilmesi engellenir.

| Kontrol | Açıklama |
|---------|----------|
| `UrlSafetyValidator` | Kayıt ve istek öncesi URL doğrulama |
| Özel IP engeli | `10.x`, `172.16–31.x`, `192.168.x`, `127.x`, `169.254.x`, `0.0.0.0` |
| Host engeli | `localhost`, `*.local`, `metadata.google.internal` |
| DNS doğrulama | Host adı çözümlendikten sonra dönen IP'ler de kontrol edilir |
| Redirect koruması | `HttpHealthChecker` otomatik yönlendirmeyi manuel takip eder; her hop'ta URL yeniden doğrulanır (max 10 yönlendirme) |

**Reddedilen örnek URL'ler:** `http://127.0.0.1`, `http://192.168.1.1`, `http://169.254.169.254`, `http://localhost`

Harici bir URL iç ağa yönlendirse bile (redirect), ikinci adımda engellenir.

### Kaynak limitleri (DoS önleme)

| Limit | Değer |
|-------|-------|
| Maksimum site sayısı | 50 |
| Kontrol aralığı | 5 – 86.400 sn (1 gün) |
| Zaman aşımı | 1 – 60 sn |
| Eşzamanlı HTTP kontrolü | 10 |

Limitler `SiteLimits` ve `SiteInputValidator` ile site ekleme/düzenleme sırasında uygulanır. `WebsiteMonitorService` eşzamanlı istekleri `SemaphoreSlim` ile sınırlar.

### SMTP ve e-posta güvenliği

| Önlem | Açıklama |
|-------|----------|
| DPAPI şifreleme | SMTP şifresi `smtp-password.dat` dosyasında Windows DPAPI ile şifrelenir |
| Header injection engeli | Site adında `\r`, `\n`, `\0` karakterleri reddedilir (`InputSanitizer`) |
| E-posta sanitizasyonu | Bildirim konu ve gövdesindeki metinler gönderim öncesi temizlenir |

### Hata mesajı sızıntısı

HTTP kontrol hatalarında kullanıcıya genel mesaj gösterilir (`Bağlantı hatası`, `İstek zaman aşımına uğradı`). Teknik detaylar yalnızca debug çıktısına yazılır; UI ve e-postada iç yapı bilgisi paylaşılmaz.

### Mevcut güvenli varsayılanlar

- URL şeması yalnızca `http` / `https` (`UrlValidator`)
- Windows başlangıç kaydı yalnızca `HKCU\...\Run` (`StartupRegistryHelper`)
- SMTP ayarları ve site listesi `%AppData%` altında kullanıcıya özel dizinde saklanır

### Güvenlik testleri

Projede saldırı senaryolarını doğrulayan birim testleri bulunur:

```bash
dotnet test tests/WebSiteChecker.SecurityTests/WebSiteChecker.SecurityTests.csproj
```

Test kapsamı:

- SSRF URL engelleme (`UrlSafetyValidator`)
- SMTP header injection karakterleri (`InputSanitizer`)
- Site giriş limitleri (`SiteInputValidator`, `SiteLimits`)

### Manuel güvenlik kontrol listesi

Uygulamayı çalıştırarak şunları doğrulayabilirsiniz:

1. Site URL olarak `http://127.0.0.1` ekleyin → reddedilmeli
2. `http://192.168.0.1` ekleyin → reddedilmeli
3. Site adına `\r\n` içeren metin girin → reddedilmeli
4. 51. siteyi eklemeyi deneyin → limit uyarısı gelmeli
5. `smtp-password.dat` dosyasını başka bir Windows kullanıcısıyla açmayı deneyin → DPAPI engellemeli

## Tray Davranışı

- Pencereyi kapatmak (X) uygulamayı sonlandırmaz; tray ikonunda kalır.
- Tray menüsü: **Aç**, **Duraklat / Devam**, **Çıkış**
- Tamamen kapatmak için tray menüsünden **Çıkış** seçin.

## Bildirim Kuralları

- Site erişilemez olunca **bir kez** down maili gönderilir.
- Site hâlâ erişilemezken tekrar mail gönderilmez.
- Site tekrar erişilebilir olunca (ayar açıksa) recovery maili gönderilir.
- Aynı site kısa süre içinde tekrar down olursa, bekleme süresi (varsayılan 30 dk) dolmadan yeni down maili gönderilmez.
