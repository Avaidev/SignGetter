using System.Windows;

namespace Ds510SignGetter.Utilities;

public static class MessageService
{
    public static void ErrorMessage(string message) => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    public static void InformationMessage(string message) => MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    
    public static void WarningMessage(string message) => MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

    public static bool AskYesNoMessage(string message)
    {
        switch (MessageBox.Show(message, "Choose", MessageBoxButton.YesNo, MessageBoxImage.Question))
        {
            case MessageBoxResult.Yes:
                return true;
            case MessageBoxResult.No:
            default:
                return false;
        }
    }

    public static bool? AskYesNoCancelMessage(string message)
    {
        switch (MessageBox.Show(message, "Choose", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
        {
            case MessageBoxResult.Yes:
                return true;
            case MessageBoxResult.No:
                return false;
            case MessageBoxResult.Cancel:
            default:
                return null;
        }
    }
}