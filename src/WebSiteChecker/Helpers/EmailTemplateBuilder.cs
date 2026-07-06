using System.Net;
using WebSiteChecker.Models;

namespace WebSiteChecker.Helpers;

public enum EmailAlertType
{
    Down,
    Recovery,
    Test
}

public static class EmailTemplateBuilder
{
    public const string LogoContentId = "hssgm-logo";

    private static string Font => $"font-family:{BrandAssets.FontFamilyCss};";

    private static string EmailStyles =>
        "<style>" +
        "body,table,td,th,p,h1,div,span{" + Font + "}" +
        "@media (prefers-color-scheme:dark){" +
        ".email-wrap{background-color:#0F172A!important;}" +
        ".email-card{background-color:#1E293B!important;border-color:#334155!important;}" +
        ".email-content{color:#F1F5F9!important;}" +
        ".email-summary{color:#CBD5E1!important;}" +
        ".email-footer{background-color:#172033!important;border-color:#334155!important;color:#94A3B8!important;}" +
        ".email-label{color:#94A3B8!important;border-color:#334155!important;}" +
        ".email-value{color:#F1F5F9!important;border-color:#334155!important;}" +
        ".email-logo-wrap{background-color:#FFFFFF!important;border-color:#E5E7EB!important;}" +
        "}" +
        "</style>";

    public static (string Html, string Plain) BuildDownAlert(
        string siteName,
        string siteUrl,
        DateTime checkedAtLocal,
        string? statusCode,
        string error,
        long? responseTimeMs)
    {
        var rows = new (string Label, string Value)[]
        {
            ("Site", siteName),
            ("URL", siteUrl),
            ("Kontrol zamanı", checkedAtLocal.ToString("dd.MM.yyyy HH:mm:ss")),
            ("Durum kodu", statusCode ?? "Yok"),
            ("Hata", error),
            ("Yanıt süresi", responseTimeMs.HasValue ? $"{responseTimeMs} ms" : "—")
        };

        return Build(
            EmailAlertType.Down,
            "Site Erişim Uyarısı",
            "İzlenen web sitesine erişilemiyor. Lütfen kontrol edin.",
            rows);
    }

    public static (string Html, string Plain) BuildRecoveryAlert(
        string siteName,
        string siteUrl,
        DateTime checkedAtLocal,
        int? statusCode,
        long? responseTimeMs)
    {
        var rows = new (string Label, string Value)[]
        {
            ("Site", siteName),
            ("URL", siteUrl),
            ("Kontrol zamanı", checkedAtLocal.ToString("dd.MM.yyyy HH:mm:ss")),
            ("Durum kodu", statusCode?.ToString() ?? "—"),
            ("Yanıt süresi", responseTimeMs.HasValue ? $"{responseTimeMs} ms" : "—")
        };

        return Build(
            EmailAlertType.Recovery,
            "Site Tekrar Erişilebilir",
            "İzlenen web sitesi yeniden erişilebilir durumda.",
            rows);
    }

    public static (string Html, string Plain) BuildTestEmail()
    {
        var rows = new (string Label, string Value)[]
        {
            ("Durum", "SMTP bağlantısı başarılı"),
            ("Gönderim zamanı", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")),
            ("Sistem", BrandAssets.ApplicationName)
        };

        return Build(
            EmailAlertType.Test,
            "Test E-postası",
            "SMTP ayarlarınız doğru yapılandırılmış görünüyor.",
            rows);
    }

    private static (string Html, string Plain) Build(
        EmailAlertType type,
        string title,
        string summary,
        IReadOnlyList<(string Label, string Value)> rows)
    {
        var accent = GetAccent(type);
        var badge = GetBadge(type);
        var plainRows = string.Join(
            Environment.NewLine,
            rows.Select(row => $"{row.Label}: {row.Value}"));

        var htmlRows = string.Join(
            string.Empty,
            rows.Select((row, index) =>
            {
                var isLast = index == rows.Count - 1;
                var divider = isLast ? string.Empty : "border-bottom:1px solid #E5E7EB;";
                return $"""
                    <tr>
                        <td class="email-label" style="{Font}width:36%;padding:14px 20px 14px 0;color:#6B7280;font-size:13px;line-height:1.5;vertical-align:middle;{divider}">{Encode(row.Label)}</td>
                        <td class="email-value" style="{Font}width:64%;padding:14px 0;color:#111827;font-size:14px;font-weight:600;line-height:1.5;vertical-align:middle;word-break:break-word;{divider}">{Encode(row.Value)}</td>
                    </tr>
                    """;
            }));

        var html = $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
                <meta charset="utf-8"/>
                <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
                <meta name="color-scheme" content="light dark"/>
                <meta name="supported-color-schemes" content="light dark"/>
                <title>{Encode(title)}</title>
                {EmailStyles}
            </head>
            <body class="email-wrap" style="margin:0;padding:0;background:#F3F4F6;{Font}color:#111827;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" class="email-wrap" style="background:#F3F4F6;padding:24px 12px;{Font}">
                    <tr>
                        <td align="center">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" class="email-card" style="max-width:640px;background:#FFFFFF;border-radius:16px;overflow:hidden;border:1px solid #E5E7EB;box-shadow:0 10px 30px rgba(17,24,39,0.08);{Font}">
                                <tr>
                                    <td class="email-logo-wrap" style="background:#FFFFFF;padding:24px 28px;text-align:center;border-bottom:1px solid #E5E7EB;{Font}">
                                        <img src="cid:{LogoContentId}" alt="" style="max-width:120px;width:50%;height:auto;display:block;margin:0 auto;"/>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="email-content" style="padding:28px;{Font}color:#111827;">
                                        <table role="presentation" cellspacing="0" cellpadding="0" style="margin-bottom:20px;{Font}">
                                            <tr>
                                                <td style="background:{accent.Background};color:{accent.Foreground};font-size:12px;font-weight:700;letter-spacing:0.04em;text-transform:uppercase;padding:8px 12px;border-radius:999px;{Font}">{Encode(badge)}</td>
                                            </tr>
                                        </table>
                                        <h1 class="email-content" style="margin:0 0 12px;font-size:24px;line-height:1.35;font-weight:700;color:#111827;{Font}">{Encode(title)}</h1>
                                        <p class="email-summary" style="margin:0 0 28px;font-size:15px;line-height:1.6;color:#4B5563;{Font}">{Encode(summary)}</p>
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-top:1px solid #E5E7EB;{Font}">
                                            {htmlRows}
                                        </table>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="email-footer" style="padding:18px 28px 24px;background:#FAFAFA;border-top:1px solid #E5E7EB;{Font}">
                                        <div style="font-size:12px;color:#6B7280;line-height:1.6;{Font}">
                                            Bu e-posta <strong style="color:{BrandAssets.BrandRed};{Font}">{Encode(BrandAssets.ApplicationName)}</strong> tarafından otomatik gönderilmiştir.<br/>
                                            Web sitesi izleme bildirimi
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        var plain = $"""
            {badge}

            {title}
            {summary}

            {plainRows}

            —
            {BrandAssets.ApplicationName} otomatik bildirimi
            """;

        return (html, plain);
    }

    private static (string Background, string Foreground) GetAccent(EmailAlertType type) => type switch
    {
        EmailAlertType.Down => ("#FEE2E2", BrandAssets.BrandRed),
        EmailAlertType.Recovery => ("#D1FAE5", "#047857"),
        EmailAlertType.Test => ("#DBEAFE", "#1D4ED8"),
        _ => ("#F3F4F6", "#374151")
    };

    private static string GetBadge(EmailAlertType type) => type switch
    {
        EmailAlertType.Down => "Erişim Sorunu",
        EmailAlertType.Recovery => "Geri Dönüş",
        EmailAlertType.Test => "Test Bildirimi",
        _ => "Bildirim"
    };

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
