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
