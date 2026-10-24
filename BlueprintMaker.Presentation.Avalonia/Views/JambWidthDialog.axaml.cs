using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BlueprintMaker.Presentation.Avalonia.Views;

public partial class JambWidthDialog : Window
{
    public JambWidthDialog() : this(20) { }

    public JambWidthDialog(double initialWidth)
    {
        InitializeComponent();
        WidthBox.Value = (decimal)initialWidth;
        Opened += (_, _) => WidthBox.Focus();
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        double? width = WidthBox.Value.HasValue ? (double)WidthBox.Value.Value : null;
        Close(width);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}