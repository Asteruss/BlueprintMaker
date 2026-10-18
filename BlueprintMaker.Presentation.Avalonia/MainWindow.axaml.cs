using Avalonia.Controls;
using Avalonia.Input;
using BlueprintMaker.Presentation.Avalonia.ViewModels;
using System;
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
        private void ElementPath_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control ctrl && ctrl.DataContext is SelectableElement el)
            {
                // Вызываем команду выбора, передавая сам элемент
                el.SelectCommand.Execute(el).Subscribe(
                    _ => { },
                    ex => Console.WriteLine($"Ошибка команды: {ex.Message}")
                );

                // Отмечаем событие как обработанное, чтобы холст не снял выделение
                e.Handled = true;
            }
        }
    }
}