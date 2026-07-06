using System.IO;

namespace WebSiteChecker.Helpers;

public static class BrandAssets
{
    public const string FontFamilyCss = "'SF Pro Display', 'SF Pro Text', 'SF Pro', -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif";
    public const string FontFamilyWpf = "pack://application:,,,/Assets/Fonts/#SF Pro Display, SF Pro Text, SF Pro, Segoe UI";
    public const string LogoPackUri = "pack://application:,,,/Assets/hssgm-logo.png";
    public const string OrganizationName = "T.C. Sağlık Bakanlığı";
    public const string DirectorateName = "Türkiye Hudut ve Sahiller Sağlık Genel Müdürlüğü";
    public const string DirectorateShortName = "HSSGM";
    public const string ApplicationName = "WebSiteChecker";
    public const string BrandRed = "#C8102E";
    public const string BrandRedDark = "#9E0C24";

    public static Stream OpenLogoStream()
    {
        var resourceStream = System.Windows.Application.GetResourceStream(new Uri(LogoPackUri, UriKind.Absolute))?.Stream;
        if (resourceStream is not null)
            return resourceStream;

        var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "hssgm-logo.png");
        if (File.Exists(outputPath))
            return File.OpenRead(outputPath);

        throw new FileNotFoundException("HSSGM logosu bulunamadı.", "hssgm-logo.png");
    }
}
