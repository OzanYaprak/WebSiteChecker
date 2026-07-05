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
        if (!int.TryParse(IntervalTextBox.Text, out var interval))
            interval = SiteLimits.MinIntervalSeconds;

        if (!int.TryParse(TimeoutTextBox.Text, out var timeout))
            timeout = SiteLimits.MinTimeoutSeconds;

        if (!int.TryParse(StatusCodeTextBox.Text, out var statusCode))
            statusCode = 200;

        ResultSite = new MonitoredSite
        {
            Id = _existingId ?? Guid.NewGuid(),
            Name = NameTextBox.Text.Trim(),
            Url = UrlTextBox.Text.Trim(),
            IntervalSeconds = interval,
            TimeoutSeconds = timeout,
            ExpectedStatusCode = statusCode,
            IsEnabled = EnabledCheckBox.IsChecked == true
        };

        if (!SiteInputValidator.TryValidate(ResultSite, out var error))
        {
            DialogHelper.ShowError(error!);
            ResultSite = null;
            return;
        }

        DialogResult = true;
        Close();
    }
}
