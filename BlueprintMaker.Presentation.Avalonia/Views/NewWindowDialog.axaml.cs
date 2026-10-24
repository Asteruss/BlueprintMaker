using Avalonia.Controls;
using Avalonia.Interactivity;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Presentation.Avalonia.ViewModels;

namespace BlueprintMaker.Presentation.Avalonia.Views;

public partial class NewWindowDialog : Window
{
    public NewWindowDialog()
    {
        InitializeComponent();
        DataContext = new NewWindowDialogViewModel();
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NewWindowDialogViewModel vm && vm.TryBuild(out var contour))
            Close(contour);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close((Contour?)null);
}