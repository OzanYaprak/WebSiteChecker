using System.Windows;
using WebSiteChecker.Helpers;
using WebSiteChecker.Models;

namespace WebSiteChecker.Views;

public partial class AddEditSiteWindow : Window
{
    private readonly Guid? _existingId;

    public MonitoredSite? ResultSite { get; private set; }

    public AddEditSiteWindow(MonitoredSite? existing = null)
    {
        InitializeComponent();
        _existingId = existing?.Id;

        if (existing is not null)
        {
            Title = "Site Düzenle";
            NameTextBox.Text = existing.Name;
            UrlTextBox.Text = existing.Url;
            IntervalTextBox.Text = existing.IntervalSeconds.ToString();
            TimeoutTextBox.Text = existing.TimeoutSeconds.ToString();
            StatusCodeTextBox.Text = existing.ExpectedStatusCode.ToString();
            EnabledCheckBox.IsChecked = existing.IsEnabled;
        }
        else
        {
            Title = "Site Ekle";
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var url = UrlTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            DialogHelper.ShowError("Site adı boş olamaz.");
            return;
        }

        if (!UrlValidator.IsValidHttpUrl(url, out var urlError))
        {
            DialogHelper.ShowError(urlError!);
            return;
        }

        if (!int.TryParse(IntervalTextBox.Text, out var interval) || interval < 5)
        {
            DialogHelper.ShowError("Kontrol aralığı en az 5 saniye olmalıdır.");
            return;
        }

        if (!int.TryParse(TimeoutTextBox.Text, out var timeout) || timeout < 1)
        {
            DialogHelper.ShowError("Zaman aşımı en az 1 saniye olmalıdır.");
            return;
        }

        if (!int.TryParse(StatusCodeTextBox.Text, out var statusCode) || statusCode < 100 || statusCode > 599)
        {
            DialogHelper.ShowError("Geçerli bir HTTP durum kodu girin (100-599).");
            return;
        }

        ResultSite = new MonitoredSite
        {
            Id = _existingId ?? Guid.NewGuid(),
            Name = name,
            Url = url,
            IntervalSeconds = interval,
            TimeoutSeconds = timeout,
            ExpectedStatusCode = statusCode,
            IsEnabled = EnabledCheckBox.IsChecked == true
        };

        DialogResult = true;
        Close();
    }
}
