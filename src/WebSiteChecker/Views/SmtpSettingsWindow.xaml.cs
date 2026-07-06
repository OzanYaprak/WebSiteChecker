using System.Windows;
using WebSiteChecker.Helpers;
using WebSiteChecker.Models;
using WebSiteChecker.Services;

namespace WebSiteChecker.Views;

public partial class SmtpSettingsWindow : Window
{
    private readonly ConfigStore _configStore;
    private bool _passwordChanged;
    private bool _hasStoredPassword;

    public SmtpSettingsWindow(ConfigStore configStore)
    {
        InitializeComponent();
        _configStore = configStore;
        PasswordBox.PasswordChanged += (_, _) => _passwordChanged = true;
        Loaded += (_, _) => MaxHeight = SystemParameters.WorkArea.Height - 32;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _configStore.LoadSmtpSettings();
        HostTextBox.Text = settings.Host;
        PortTextBox.Text = settings.Port.ToString();
        UseSslCheckBox.IsChecked = settings.UseSsl;
        UsernameTextBox.Text = settings.Username;
        FromTextBox.Text = settings.FromAddress;
        ToTextBox.Text = settings.ToAddress;
        CooldownTextBox.Text = settings.AlertCooldownMinutes.ToString();
        RecoveryCheckBox.IsChecked = settings.SendRecoveryEmail;

        _hasStoredPassword = !string.IsNullOrEmpty(_configStore.LoadSmtpPassword());
        PasswordBox.Password = string.Empty;
        _passwordChanged = false;
        UpdatePasswordHint();
    }

    private void UpdatePasswordHint()
    {
        if (_hasStoredPassword && !_passwordChanged)
        {
            PasswordHintTextBlock.Text = "Kayıtlı şifre mevcut. Değiştirmek için yeni şifre girin.";
            PasswordHintTextBlock.Visibility = Visibility.Visible;
            return;
        }

        PasswordHintTextBlock.Text = string.Empty;
        PasswordHintTextBlock.Visibility = Visibility.Collapsed;
    }

    private void SaglikGovTrPresetButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SmtpSettings
        {
            ToAddress = ToTextBox.Text.Trim()
        };
        SmtpPresets.ApplySaglikGovTr(settings, string.IsNullOrWhiteSpace(settings.ToAddress) ? null : settings.ToAddress);

        HostTextBox.Text = settings.Host;
        PortTextBox.Text = settings.Port.ToString();
        UseSslCheckBox.IsChecked = settings.UseSsl;
        UsernameTextBox.Text = settings.Username;
        FromTextBox.Text = settings.FromAddress;
        if (!string.IsNullOrEmpty(settings.ToAddress))
            ToTextBox.Text = settings.ToAddress;

        DialogHelper.ShowInfo(
            """
            Sağlık Bakanlığı SMTP ön ayarı uygulandı.

            Sunucu: eposta.saglik.gov.tr
            Port: 587
            SSL: Kapalı
            Kullanıcı / Gönderen: hssgm.noreply@saglik.gov.tr

            1. Alıcı alanına bildirim alacağınız e-posta adresini yazın
            2. SMTP şifresini Şifre alanına girin
            3. Kaydet'e basın ve "Test Maili Gönder" ile deneyin
            """,
            "Kurumsal SMTP");
    }

    private void GmailPresetButton_Click(object sender, RoutedEventArgs e)
    {
        var email = UsernameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(email))
            email = FromTextBox.Text.Trim();

        var settings = new SmtpSettings
        {
            ToAddress = ToTextBox.Text.Trim()
        };
        SmtpPresets.ApplyGmail(settings, string.IsNullOrEmpty(email) ? null : email);

        HostTextBox.Text = settings.Host;
        PortTextBox.Text = settings.Port.ToString();
        UseSslCheckBox.IsChecked = settings.UseSsl;

        if (!string.IsNullOrEmpty(settings.Username))
        {
            UsernameTextBox.Text = settings.Username;
            FromTextBox.Text = settings.FromAddress;
            if (!string.IsNullOrEmpty(settings.ToAddress))
                ToTextBox.Text = settings.ToAddress;
        }

        DialogHelper.ShowInfo(
            """
            Gmail SMTP ön ayarı uygulandı.

            Google hesabınızla giriş için normal şifre yerine Uygulama Parolası kullanın:

            1. https://myaccount.google.com/apppasswords adresine gidin
            2. İki adımlı doğrulamayı açın (kapalıysa)
            3. "Mail" veya "Diğer" için 16 haneli uygulama parolası oluşturun
            4. Kullanıcı adı / Gönderen / Alıcı alanlarına Gmail adresinizi yazın
            5. Oluşturduğunuz uygulama parolasını Şifre alanına yapıştırın
            6. Kaydet'e basın ve ana ekrandan "Test Maili Gönder" ile deneyin
            """,
            "Gmail Kurulumu");
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(HostTextBox.Text))
        {
            DialogHelper.ShowError("SMTP sunucu adresi boş olamaz.");
            return;
        }

        if (!int.TryParse(PortTextBox.Text, out var port) || port < 1 || port > 65535)
        {
            DialogHelper.ShowError("Geçerli bir port numarası girin.");
            return;
        }

        if (!int.TryParse(CooldownTextBox.Text, out var cooldown) || cooldown < 1)
        {
            DialogHelper.ShowError("Uyarı bekleme süresi en az 1 dakika olmalıdır.");
            return;
        }

        var fromAddress = FromTextBox.Text.Trim();
        var toAddress = ToTextBox.Text.Trim();
        var username = UsernameTextBox.Text.Trim();

        if (!SmtpConnectionHelper.TryParseMailbox(fromAddress, out _))
        {
            DialogHelper.ShowError("Geçerli bir gönderen e-posta adresi girin.");
            return;
        }

        if (!SmtpConnectionHelper.TryParseMailbox(toAddress, out _))
        {
            DialogHelper.ShowError("Geçerli bir alıcı e-posta adresi girin.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(username) && !SmtpConnectionHelper.TryParseMailbox(username, out _))
        {
            DialogHelper.ShowError("Geçerli bir SMTP kullanıcı adı (e-posta) girin.");
            return;
        }

        var settings = new SmtpSettings
        {
            Host = HostTextBox.Text.Trim(),
            Port = port,
            UseSsl = UseSslCheckBox.IsChecked == true,
            Username = username,
            FromAddress = fromAddress,
            ToAddress = toAddress,
            AlertCooldownMinutes = cooldown,
            SendRecoveryEmail = RecoveryCheckBox.IsChecked == true
        };

        try
        {
            SmtpConnectionHelper.ValidateSenderAlignment(settings);
        }
        catch (InvalidOperationException ex)
        {
            DialogHelper.ShowError(ex.Message);
            return;
        }

        _configStore.SaveSmtpSettings(settings);

        if (_passwordChanged)
        {
            var password = SmtpConnectionHelper.NormalizeAppPassword(PasswordBox.Password);
            if (string.IsNullOrEmpty(password))
            {
                DialogHelper.ShowError("SMTP şifresi boş olamaz.");
                return;
            }

            _configStore.SaveSmtpPassword(password);
            _hasStoredPassword = true;
            _passwordChanged = false;
            PasswordBox.Password = string.Empty;
            UpdatePasswordHint();
        }
        else if (!_hasStoredPassword)
        {
            DialogHelper.ShowError("SMTP şifresi boş. Şifreyi Şifre alanına girip tekrar kaydedin.");
            return;
        }

        DialogResult = true;
        Close();
    }
}
