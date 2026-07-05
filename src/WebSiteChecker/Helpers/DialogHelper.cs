using System.Windows;
using WebSiteChecker.Models;
using WebSiteChecker.Views;

namespace WebSiteChecker.Helpers;

public static class DialogHelper
{
    private static void ShowThemed(string message, string title, AlertDialogType type, bool yesNo = false)
    {
        var dialog = new ThemedAlertWindow(message, title, type, yesNo);
        dialog.ShowDialogCentered();
    }

    public static void ShowError(string message, string title = "Hata")
    {
        ShowThemed(message, title, AlertDialogType.Error);
    }

    public static void ShowInfo(string message, string title = "Bilgi")
    {
        ShowThemed(message, title, AlertDialogType.Info);
    }

    public static bool Confirm(string message, string title = "Onay")
    {
        var dialog = new ThemedAlertWindow(message, title, AlertDialogType.Question, showYesNo: true);
        return dialog.ShowDialogCentered() == true;
    }
}
