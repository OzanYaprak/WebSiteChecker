using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebSiteChecker.Helpers;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            // Stack trace gibi iç detayları kullanıcıya sızdırma; tanı için Debug'a yaz
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {args.Exception}");
            MessageBox.Show(
                $"Beklenmeyen bir hata oluştu:\n{args.Exception.Message}",
                "WebSiteChecker",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            InitializeHost();
            InitializeTrayIcon();

            _mainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow = _mainWindow;

            var startMinimized = e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);
            if (!startMinimized)
                ShowMainWindow();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Uygulama başlatılamadı:\n{ex.Message}",
                "WebSiteChecker",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void InitializeHost()
    {
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

        _host.StartAsync().GetAwaiter().GetResult();
        Services.GetRequiredService<ThemeService>().Initialize();
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
                ToolTipText = "Web Pages Health Status Control Panel",
                Visibility = Visibility.Visible,
                ContextMenu = CreateTrayContextMenu(),
                MenuActivation = PopupActivationMode.RightClick
            };
        }

        _notifyIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
    }

    private ContextMenu CreateTrayContextMenu()
    {
        var menu = new ContextMenu
        {
            Background = System.Windows.SystemColors.MenuBrush,
            Foreground = System.Windows.SystemColors.MenuTextBrush
        };

        // ModernTheme'deki global TextBlock stilinin menüyü görünmez yapmasını engelle
        var textBlockStyle = new Style(typeof(TextBlock));
        textBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, System.Windows.SystemColors.MenuTextBrush));
        textBlockStyle.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily(BrandAssets.FontFamilyWpf)));
        menu.Resources.Add(typeof(TextBlock), textBlockStyle);

        var menuItemStyle = new Style(typeof(MenuItem));
        menuItemStyle.Setters.Add(new Setter(MenuItem.ForegroundProperty, System.Windows.SystemColors.MenuTextBrush));
        menuItemStyle.Setters.Add(new Setter(MenuItem.FontFamilyProperty, new System.Windows.Media.FontFamily(BrandAssets.FontFamilyWpf)));
        menu.Resources.Add(typeof(MenuItem), menuItemStyle);

        menu.Items.Add(CreateTrayMenuItem("Aç", (_, _) => ShowMainWindow()));
        menu.Items.Add(CreateTrayMenuItem("Duraklat / Devam", (_, _) => ToggleMonitorPause()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateTrayMenuItem("Çıkış", (_, _) => Shutdown()));

        return menu;
    }

    private static MenuItem CreateTrayMenuItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem
        {
            Header = header,
            Foreground = System.Windows.SystemColors.MenuTextBrush
        };
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

        if (!_mainWindow.IsVisible)
            _mainWindow.Show();

        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.ShowInTaskbar = true;
        _mainWindow.Activate();
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
        _mainWindow.Focus();
    }

    private void ToggleMonitorPause()
    {
        if (_host is null)
            return;

        var monitor = Services.GetRequiredService<IWebsiteMonitorService>();
        if (monitor.IsPaused)
            monitor.Resume();
        else
            monitor.Pause();
    }
}
