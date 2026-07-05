using System.Windows;
using WebSiteChecker.Helpers;
using WebSiteChecker.Models;
using WebSiteChecker.Services;

namespace WebSiteChecker.Views;

public partial class SmtpSettingsWindow : Window
{
    private readonly ConfigStore _configStore;
    private bool _passwordChanged;

    public SmtpSettingsWindow(ConfigStore configStore)
    {
        InitializeComponent();
        _configStore = configStore;
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

        var existingPassword = _configStore.LoadSmtpPassword();
        if (!string.IsNullOrEmpty(existingPassword))
            PasswordBox.Password = "********";

        PasswordBox.PasswordChanged += (_, _) => _passwordChanged = true;
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

        var settings = new SmtpSettings
        {
            Host = HostTextBox.Text.Trim(),
            Port = port,
            UseSsl = UseSslCheckBox.IsChecked == true,
            Username = UsernameTextBox.Text.Trim(),
            FromAddress = FromTextBox.Text.Trim(),
            ToAddress = ToTextBox.Text.Trim(),
            AlertCooldownMinutes = cooldown,
            SendRecoveryEmail = RecoveryCheckBox.IsChecked == true
        };

        _configStore.SaveSmtpSettings(settings);

        if (_passwordChanged)
        {
            var password = PasswordBox.Password;
            _configStore.SaveSmtpPassword(string.IsNullOrWhiteSpace(password) ? null : password);
        }

        DialogResult = true;
        Close();
    }
}
