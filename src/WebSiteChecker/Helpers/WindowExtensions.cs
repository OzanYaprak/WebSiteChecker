using System.Linq;
using System.Windows;

namespace WebSiteChecker.Helpers;

public static class WindowExtensions
{
    public static bool? ShowDialogCentered(this Window dialog)
    {
        var owner = FindDialogOwner(dialog);
        if (owner is not null)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog.ShowDialog();
    }

    private static Window? FindDialogOwner(Window dialog)
    {
        var active = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive && w != dialog);

        if (active is not null)
            return active;

        var main = Application.Current.MainWindow;
        return main is not null && main.IsVisible && main != dialog ? main : null;
    }
}
