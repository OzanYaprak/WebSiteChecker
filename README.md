# WebSiteChecker

> **Dahili dokümantasyon** — Bu README yalnızca proje sahibi tarafından okunur. Private kullanım; hassas yapılandırma ve güvenlik notları içerir.

Birden fazla web sitesini periyodik olarak kontrol eden ve erişilemediğinde e-posta ile bildiren Windows masaüstü uygulaması.

**Hedef platform:** Windows 10/11 · **Çerçeve:** .NET 8 (WPF)

## Özellikler

- Birden fazla site tanımlama (URL, kontrol aralığı, zaman aşımı, beklenen HTTP kodu)
- Arka planda otomatik HTTP kontrolü (HEAD, gerekirse GET)
- Site erişilemediğinde SMTP ile e-posta bildirimi
- Site tekrar erişilebilir olunca bilgilendirme maili
- Kurumsal (Sağlık Bakanlığı) ve Gmail SMTP ön ayarları
- Açık / koyu tema
- Sistem tepsisinde (tray) çalışma — pencere kapatılsa bile izleme devam eder
- VPN veya kurum içi ağ hedefleri için isteğe bağlı özel ağ izni

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

Güvenlik testleri:

```bash
dotnet test tests/WebSiteChecker.SecurityTests/WebSiteChecker.SecurityTests.csproj
```

## Proje Yapısı

```
WebSiteChecker/
├── config/                          # Örnek SMTP yapılandırmaları (şifre içermez)
├── src/WebSiteChecker/
│   ├── Helpers/                     # URL doğrulama, SMTP, e-posta şablonları
│   ├── Models/                      # Site, SMTP ve durum modelleri
│   ├── Services/                    # HTTP kontrol, izleme, yapılandırma deposu
│   ├── ViewModels/                  # Ana pencere MVVM
│   └── Views/                       # Site ekleme/düzenleme, SMTP ayarları
└── tests/WebSiteChecker.SecurityTests/  # Güvenlik birim testleri
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

> **Gönderen / kullanıcı eşleşmesi:** SMTP sunucusuna hangi hesapla giriş yapılıyorsa **Gönderen** ve **Kullanıcı adı** alanları da aynı e-posta olmalıdır. Alıcı farklı bir adres olabilir.


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
```

Tam yol örneği:

```
C:\Users\<Windows-kullanıcı-adın>\AppData\Roaming\WebSiteChecker\
```

Klasörü hızlı açmak: **Win + R** → `%AppData%\WebSiteChecker` → Enter

```
%AppData%\WebSiteChecker\
├── sites.json              # İzlenen siteler (ad, URL, aralık, timeout vb.)
├── smtp-settings.json      # SMTP sunucu ayarları (şifre YOK — düz metin JSON)
├── smtp-password.dat       # Yalnızca SMTP şifresi (DPAPI ile şifreli bayt dizisi)
├── ui-settings.json        # Tema tercihi
└── monitor-state.json      # Down/recovery bildirim durumu (cooldown takibi)
```

**Önemli:** Gerçek veriler proje klasöründe (`Desktop\...\WebSiteChecker`) değil; yalnızca `%AppData%` altında. `config/*.example.json` dosyaları sadece örnek şablon, çalışma zamanında kullanılmaz.

### SMTP kimlik bilgileri nerede tutulur?

SMTP bilgileri bilinçli olarak **iki ayrı dosyaya** bölünmüştür:

| Ne | Dosya | Format | Şifre var mı? |
|----|-------|--------|---------------|
| Sunucu, port, SSL, kullanıcı adı, gönderen, alıcı, cooldown | `smtp-settings.json` | Okunabilir JSON | Hayır |
| SMTP parolası / uygulama parolası | `smtp-password.dat` | DPAPI şifreli binary | Evet (şifreli) |

Kod tarafı: `ConfigStore.cs` — `_smtpPath` ve `_passwordPath`

**Kaydetme akışı** (`SmtpSettingsWindow` → Kaydet):

1. Formdaki sunucu bilgileri → `SaveSmtpSettings()` → `smtp-settings.json`
2. Şifre alanı değiştiyse → `SaveSmtpPassword()` → `smtp-password.dat`
3. Kayıttan sonra `PasswordBox` temizlenir; şifre UI'da bellekte tutulmaz
4. Şifre alanına dokunmadan kaydedersen mevcut `smtp-password.dat` olduğu gibi kalır

**Şifreleme detayı:**

- API: `ProtectedData.Protect` / `Unprotect` (`System.Security.Cryptography`)
- Kapsam: `DataProtectionScope.CurrentUser` — yalnızca şifreyi kaydeden Windows kullanıcısı, aynı makinede çözebilir
- `smtp-password.dat` dosyasını Not Defteri ile açarsan okunabilir metin görmezsin; anlamsız baytlar görürsün
- Başka Windows kullanıcısı veya başka PC bu dosyayı açsa şifreyi okuyamaz

**Mail gönderirken:** `SmtpEmailNotifier` → `LoadSmtpSettings()` + `LoadSmtpPassword()` ile ikisini birleştirir, SMTP oturumu açar, iş bitince bellekten gider.

Bu klasör yalnızca oturum açmış Windows kullanıcısı tarafından okunabilir; `smtp-password.dat` başka bir kullanıcı hesabıyla açılamaz.

## Özel Ağ İzni (VPN / Kurum İçi)

Varsayılan olarak uygulama yalnızca genel internet adreslerine istek gönderir. VPN veya kurum içi ağdaki siteleri izlemek için site eklerken **Özel ağlara izin ver** seçeneğini açın.

| Durum | Davranış |
|-------|----------|
| Kapalı (varsayılan) | `127.0.0.1`, `192.168.x`, `10.x` vb. engellenir |
| Açık | Özel IP ve yerel hostname'lere istek gönderilebilir |

> Bu seçeneği yalnızca güvenilir iç siteler için kullanın. Açıkken SSRF koruması gevşetilir.

## Güvenlik

Uygulama, kullanıcı girdisi ve ağ istekleri için savunma katmanları içerir. Aşağıdaki önlemler `Helpers/` ve `Services/` altındaki bileşenlerle uygulanmıştır.

### SSRF koruması (Server-Side Request Forgery)

Site URL'leri varsayılan olarak yalnızca genel internet adreslerine yönlendirilebilir. İç ağ ve yerel hedeflere istek gönderilmesi engellenir.

| Kontrol | Açıklama |
|---------|----------|
| `UrlSafetyValidator` | Kayıt ve istek öncesi URL doğrulama |
| Özel IPv4 engeli | `10.x`, `172.16–31.x`, `192.168.x`, `127.x`, `169.254.x`, `100.64–127.x` (CGNAT), `192.0.0.0/24`, `198.18–19.x`, `0.0.0.0`, multicast/rezerve `224+` |
| IPv6 engeli | `::1`, `::`, link-local (`fe80::/10`), unique local (`fc00::/7`), multicast (`ff00::/8`), NAT64 (`64:ff9b::/96`) |
| IPv4-mapped bypass engeli | `::ffff:127.0.0.1` gibi adresler kontrol öncesi IPv4'e indirgenir |
| Host engeli | `localhost`, `localhost.` (sondaki nokta), `*.local`, `metadata.google.internal` |
| DNS doğrulama | Host adı çözümlendikten sonra dönen tüm IP'ler kontrol edilir |
| DNS rebinding koruması | `HttpHealthChecker` TCP bağlantısı kurulurken IP'leri tekrar doğrular; ön kontrol ile bağlantı anındaki DNS cevabı farklı olsa bile iç ağa erişim engellenir |
| Redirect koruması | Otomatik yönlendirme manuel takip edilir; her hop'ta URL yeniden doğrulanır (en fazla 10 yönlendirme) |

**Reddedilen örnek URL'ler:** `http://127.0.0.1`, `http://192.168.1.1`, `http://169.254.169.254`, `http://localhost`, `http://[::ffff:127.0.0.1]`, `http://localhost.`

Harici bir URL iç ağa yönlendirse bile (redirect veya DNS rebinding), bağlantı kurulmadan engellenir.

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
| DPAPI şifreleme | SMTP şifresi `smtp-password.dat` dosyasında Windows DPAPI (`DataProtectionScope.CurrentUser`) ile şifrelenir |
| Fırsatçı STARTTLS | SSL kapalı olsa bile sunucu STARTTLS destekliyorsa kimlik bilgileri şifreli kanal üzerinden gönderilir (`StartTlsWhenAvailable`) |
| Gönderen doğrulama | Gönderen adresi ile SMTP kullanıcı adının aynı hesap olması zorunludur |
| Header injection engeli | Site adında `\r`, `\n`, `\0` karakterleri reddedilir (`InputSanitizer`) |
| E-posta sanitizasyonu | Bildirim konusu ve gövdesindeki kullanıcı metinleri gönderim öncesi temizlenir |
| HTML encoding | E-posta şablonundaki dinamik alanlar `HtmlEncode` ile kaçışlanır (`EmailTemplateBuilder`) |

### Hata mesajı sızıntısı

| Katman | Davranış |
|--------|----------|
| HTTP kontrol | Kullanıcıya genel mesaj (`Bağlantı hatası`, `İstek zaman aşımına uğradı`); teknik detay yalnızca debug çıktısında |
| Yakalanmamış UI hataları | Stack trace kullanıcıya gösterilmez; yalnızca hata mesajı gösterilir, detay Debug'a yazılır |
| E-posta bildirimleri | İç yapı veya dosya yolu bilgisi paylaşılmaz |

### Mevcut güvenli varsayılanlar

- URL şeması yalnızca `http` / `https` (`UrlValidator`)
- SMTP ayarları ve site listesi `%AppData%` altında kullanıcıya özel dizinde saklanır
- Örnek yapılandırma dosyaları (`config/`) şifre içermez; gerçek parolalar yalnızca uygulama içinden kaydedilir

### Güvenlik testleri

Projede saldırı senaryolarını doğrulayan birim testleri bulunur:

```bash
dotnet test tests/WebSiteChecker.SecurityTests/WebSiteChecker.SecurityTests.csproj
```

Test kapsamı:

- SSRF URL engelleme (`UrlSafetyValidator`)
- Özel ağ izni davranışı (`allowPrivateNetworks`)
- SMTP bağlantı ve gönderen doğrulama (`SmtpConnectionHelper`)
- SMTP header injection karakterleri (`InputSanitizer`)
- E-posta HTML encoding (`EmailTemplateBuilder`)
- Site giriş limitleri (`SiteInputValidator`, `SiteLimits`)

### Manuel güvenlik kontrol listesi

Uygulamayı çalıştırarak şunları doğrulayabilirsiniz:

1. Site URL olarak `http://127.0.0.1` ekleyin → reddedilmeli
2. `http://192.168.0.1` ekleyin → reddedilmeli
3. `http://[::ffff:127.0.0.1]` ekleyin → reddedilmeli
4. `http://localhost.` (sondaki nokta ile) ekleyin → reddedilmeli
5. Site adına `\r\n` içeren metin girin → reddedilmeli
6. 51. siteyi eklemeyi deneyin → limit uyarısı gelmeli
7. `smtp-password.dat` dosyasını başka bir Windows kullanıcısıyla açmayı deneyin → DPAPI engellemeli
8. Özel ağ izni kapalıyken iç IP'li site → reddedilmeli; izin açıkken → kabul edilmeli

## Tray Davranışı

- Pencereyi kapatmak (X) uygulamayı sonlandırmaz; tray ikonunda kalır.
- Tray menüsü: **Aç**, **Duraklat / Devam**, **Çıkış**
- Tamamen kapatmak için tray menüsünden **Çıkış** seçin.

## Bildirim Kuralları

- Site erişilemez olunca **bir kez** down maili gönderilir.
- Site hâlâ erişilemezken tekrar mail gönderilmez.
- Site tekrar erişilebilir olunca (ayar açıksa) recovery maili gönderilir.
- Aynı site kısa süre içinde tekrar down olursa, bekleme süresi (varsayılan 30 dk) dolmadan yeni down maili gönderilmez.

---

## Dahili Notlar (Cursor oturumu özeti)

Bu bölüm güvenlik incelemesi ve SMTP saklama konuşmalarından derlenmiştir. İleride hatırlamak için burada duruyor.

### Güvenlik incelemesinde kapatılan açıklar

| # | Sorun | Risk | Çözüm | Değişen dosya |
|---|-------|------|-------|---------------|
| 1 | IPv4-mapped IPv6 bypass (`::ffff:127.0.0.1`) | SSRF ile localhost'a erişim | Kontrol öncesi `MapToIPv4()` | `UrlSafetyValidator.cs` |
| 2 | Eksik özel IP aralıkları (CGNAT, benchmark, multicast, NAT64) | İç ağ/metadata hedeflerine istek | Genişletilmiş `IsBlockedIpAddress` | `UrlSafetyValidator.cs` |
| 3 | `localhost.` sondaki nokta bypass | Host engeli aşımı | `Host.TrimEnd('.')` | `UrlSafetyValidator.cs` |
| 4 | DNS rebinding (TOCTOU) | Ön kontrol güvenli IP, bağlantıda 127.0.0.1 | `ConnectCallback` ile bağlantı anında IP doğrulama | `HttpHealthChecker.cs` |
| 5 | SSL kapalıyken düz metin SMTP | Kimlik bilgisi sniffing | `StartTlsWhenAvailable` | `SmtpConnectionHelper.cs` |
| 6 | UI'da stack trace gösterimi | İç yapı / yol sızıntısı | Yalnızca `Message`, detay Debug'a | `App.xaml.cs` |

### İncelenip sağlam bulunanlar (dokunulmadı)

- SMTP şifresinin DPAPI (`CurrentUser`) ile ayrı dosyada tutulması
- E-posta HTML'inde `WebUtility.HtmlEncode`
- Site adında CRLF/header injection kontrolü (`InputSanitizer`)
- Redirect zincirinde her hop'ta URL yeniden doğrulama (max 10)
- Port, aralık, site sayısı ve eşzamanlılık limitleri
- Gönderen / SMTP kullanıcı adı eşleşme zorunluluğu

### Bilinçli olarak yapılmayan

- DPAPI'ye ek **entropy** eklenmedi — mevcut `smtp-password.dat` dosyaları çözülemez hale gelirdi; sıfırdan şifre girmek gerekirdi.

### Kurumsal SMTP (Sağlık) — pratik not

Ön ayarda **SSL kullan = Hayır** görünür; kod artık `StartTlsWhenAvailable` kullanıyor. `eposta.saglik.gov.tr` STARTTLS sunuyorsa bağlantı şifrelenir. Sunucu sertifikası geçersizse test mailinde sertifika hatası alabilirsin — o durumda haber ver, ayrı ele alınır.

### Yedekleme / taşıma

Başka PC'ye veya kullanıcıya taşırken:

- `smtp-settings.json` → doğrudan kopyalanabilir
- `smtp-password.dat` → **kopyalansa bile** farklı kullanıcı/PC'de DPAPI çözmez; SMTP Ayarları'ndan şifreyi yeniden girmen gerekir
- `sites.json`, `monitor-state.json`, `ui-settings.json` → kopyalanabilir

### Hızlı komutlar (hatırlatma)

```bash
# Çalıştır
cd src/WebSiteChecker && dotnet run

# Güvenlik testleri
dotnet test tests/WebSiteChecker.SecurityTests/WebSiteChecker.SecurityTests.csproj

# Release publish
dotnet publish src/WebSiteChecker -c Release -r win-x64 --self-contained false
```
