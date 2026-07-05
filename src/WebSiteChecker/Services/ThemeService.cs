using System.Windows;
using System.Windows.Media;
using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public class ThemeService
{
    private readonly ConfigStore _configStore;
    private ResourceDictionary? _paletteDictionary;

    public bool IsDarkMode { get; private set; }

    public event EventHandler? ThemeChanged;

    public ThemeService(ConfigStore configStore)
    {
        _configStore = configStore;
    }

    public void Initialize()
    {
        IsDarkMode = _configStore.LoadUiSettings().IsDarkMode;
        ApplyTheme(IsDarkMode);
    }

    public void ToggleTheme()
    {
        SetTheme(!IsDarkMode);
    }

    public void SetTheme(bool isDarkMode)
    {
        IsDarkMode = isDarkMode;
        ApplyTheme(isDarkMode);

        var settings = _configStore.LoadUiSettings();
        settings.IsDarkMode = isDarkMode;
        _configStore.SaveUiSettings(settings);

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyTheme(bool isDarkMode)
    {
        var app = Application.Current;
        var merged = app.Resources.MergedDictionaries;

        if (_paletteDictionary is not null)
            merged.Remove(_paletteDictionary);

        _paletteDictionary = new ResourceDictionary
        {
            Source = new Uri(
                isDarkMode ? "Themes/DarkPalette.xaml" : "Themes/LightPalette.xaml",
                UriKind.Relative)
        };

        merged.Insert(0, _paletteDictionary);
        RefreshOpenWindows();
    }

    private static void RefreshOpenWindows()
    {
        var app = Application.Current;
        if (app is null)
            return;

        var background = app.TryFindResource("AppBackgroundBrush") as Brush;

        foreach (Window window in app.Windows)
        {
            if (background is not null)
                window.Background = background;

            window.InvalidateVisual();
        }
    }
}
