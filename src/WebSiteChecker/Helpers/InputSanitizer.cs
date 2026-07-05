namespace WebSiteChecker.Helpers;

public static class InputSanitizer
{
    public static bool ContainsHeaderInjectionChars(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return input.Any(c => c is '\r' or '\n' or '\0');
    }

    public static string SanitizeForEmailText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return new string(input.Where(c => c is not ('\r' or '\n' or '\0')).ToArray());
    }
}
