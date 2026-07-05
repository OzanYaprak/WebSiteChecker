using System.Windows;
using System.Windows.Media;
using WebSiteChecker.Models;

namespace WebSiteChecker.Views;

public partial class ThemedAlertWindow : Window
{
    public ThemedAlertWindow(string message, string title, AlertDialogType type, bool showYesNo = false)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfigureIcon(type);

        if (showYesNo)
        {
            OkButtonPanel.Visibility = Visibility.Collapsed;
            YesNoButtonPanel.Visibility = Visibility.Visible;
        }
    }

    private void ConfigureIcon(AlertDialogType type)
    {
        switch (type)
        {
            case AlertDialogType.Error:
                IconBorder.Background = (Brush)FindResource("DangerBrush");
                IconText.Text = "!";
                break;
            case AlertDialogType.Question:
                IconBorder.Background = (Brush)FindResource("WarningBrush");
                IconText.Text = "?";
                break;
            default:
                IconBorder.Background = (Brush)FindResource("PrimaryBrush");
                IconText.Text = "i";
                break;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
