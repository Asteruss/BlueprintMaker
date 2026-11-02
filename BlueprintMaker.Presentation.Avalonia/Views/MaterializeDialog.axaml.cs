using Avalonia.Controls;
using Avalonia.Interactivity;
using BlueprintMaker.Presentation.Avalonia.Helpers;

namespace BlueprintMaker.Presentation.Avalonia.Views;

public partial class MaterializeDialog : Window
{
    public MaterializeDialog() : this(new MaterializeOptions(20, false)) { }

    public MaterializeDialog(MaterializeOptions initial)
    {
        InitializeComponent();
        WidthBox.Value = (decimal)initial.Width;
        OverwriteSwitch.IsChecked = initial.OverwriteExisting;
        Opened += (_, _) => WidthBox.Focus();
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        if (WidthBox.Value is not { } width || width <= 0) return;   // пустое поле — окно остаётся открытым

        Close(new MaterializeOptions((double)width, OverwriteSwitch.IsChecked == true));
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close((MaterializeOptions?)null);
}