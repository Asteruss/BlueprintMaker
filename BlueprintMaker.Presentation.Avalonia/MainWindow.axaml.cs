using Avalonia.Controls;
using Avalonia.Input;
using BlueprintMaker.Presentation.Avalonia.ViewModels;
using System.Diagnostics;

namespace BlueprintMaker.Presentation.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

    }
}