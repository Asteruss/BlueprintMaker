using Avalonia.Controls;
using Avalonia.Interactivity;
using BlueprintMaker.Presentation.Avalonia.Helpers;

namespace BlueprintMaker.Presentation.Avalonia.Views;

public partial class ChoiceDialog : Window
{
    public ChoiceDialog() : this("", "Да", "Нет", "Отмена") { }

    public ChoiceDialog(string message, string yes, string no, string cancel)
    {
        InitializeComponent();
        MessageText.Text = message;
        YesButton.Content = yes;

        NoButton.Content = no;
        NoButton.IsVisible = !string.IsNullOrEmpty(no);

        CancelButton.Content = cancel;
        CancelButton.IsVisible = !string.IsNullOrEmpty(cancel);

        if (string.IsNullOrEmpty(no) && string.IsNullOrEmpty(cancel))
            YesButton.IsCancel = true;
    }

    private void Yes_Click(object? sender, RoutedEventArgs e) => Close(ChoiceResult.Yes);
    private void No_Click(object? sender, RoutedEventArgs e) => Close(ChoiceResult.No);
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(ChoiceResult.Cancel);
}