using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebSiteChecker.Services;
using WebSiteChecker.ViewModels;

namespace WebSiteChecker;

public partial class App : Application
{
    private IHost? _host;
    private MainWindow? _mainWindow;
    private TaskbarIcon? _notifyIcon;

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("Host is not initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        InitializeTrayIcon();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ConfigStore>();
                services.AddSingleton<ThemeService>();
                services.AddSingleton<HttpHealthChecker>();
                services.AddSingleton<SmtpEmailNotifier>();
                services.AddSingleton<AlertStateTracker>();
                services.AddSingleton<WebsiteMonitorService>();
                services.AddSingleton<IWebsiteMonitorService>(sp => sp.GetRequiredService<WebsiteMonitorService>());
                services.AddHostedService(sp => sp.GetRequiredService<WebsiteMonitorService>());
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        Services.GetRequiredService<ThemeService>().Initialize();

        _mainWindow = Services.GetRequiredService<MainWindow>();
        _mainWindow.Show();
    }

    private void InitializeTrayIcon()
    {
        var resourceStream = GetResourceStream(new Uri("pack://application:,,,/Assets/tray-icon.ico", UriKind.Absolute))?.Stream
            ?? throw new InvalidOperationException("Tray ikonu yüklenemedi.");

        using (resourceStream)
        using (var memoryStream = new MemoryStream())
        {
            resourceStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            _notifyIcon = new TaskbarIcon
            {
                Icon = new Icon(memoryStream),
                ToolTipText = "WebSiteChecker",
                Visibility = Visibility.Visible
            };
        }

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(CreateMenuItem("Aç", OpenMenuItem_Click));
        contextMenu.Items.Add(CreateMenuItem("Duraklat / Devam", TogglePauseMenuItem_Click));
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(CreateMenuItem("Çıkış", ExitMenuItem_Click));

        _notifyIcon.ContextMenu = contextMenu;
        _notifyIcon.TrayMouseDoubleClick += NotifyIcon_TrayMouseDoubleClick;
    }

    private static MenuItem CreateMenuItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        return item;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
            return;

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowMainWindow();
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowMainWindow();
    }

    private void TogglePauseMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_host is null)
            return;

        var monitor = Services.GetRequiredService<IWebsiteMonitorService>();
        if (monitor.IsPaused)
            monitor.Resume();
        else
            monitor.Pause();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }
}
