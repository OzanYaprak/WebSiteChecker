using System.IO;

namespace WebSiteChecker.Helpers;

public static class BrandAssets
{
    public const string FontFamilyCss = "'SF Pro Display', 'SF Pro Text', 'SF Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif";
    public const string FontFamilyWpf = "pack://application:,,,/Assets/Fonts/#SF Pro Display, SF Pro Text, SF Pro, Segoe UI";
    public const string LogoPackUri = "pack://application:,,,/Assets/saglik-bakanligi-logo.png";
    public const string OrganizationName = "T.C. Sağlık Bakanlığı";
    public const string DirectorateName = "Türkiye Hudut ve Sahiller Sağlık Genel Müdürlüğü";
    public const string DirectorateShortName = "HSSGM";
    public const string ApplicationName = "WebSiteChecker";
    public const string BrandAccent = "#00ADB5";
    public const string BrandAccentDark = "#222831";
    public const string BrandRed = BrandAccent;
    public const string BrandRedDark = BrandAccentDark;

    public static Stream OpenLogoStream()
    {
        var resourceStream = System.Windows.Application.GetResourceStream(new Uri(LogoPackUri, UriKind.Absolute))?.Stream;
        if (resourceStream is not null)
            return resourceStream;

        var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "saglik-bakanligi-logo.png");
        if (File.Exists(outputPath))
            return File.OpenRead(outputPath);

        throw new FileNotFoundException("Sağlık Bakanlığı logosu bulunamadı.", "saglik-bakanligi-logo.png");
    }
}
