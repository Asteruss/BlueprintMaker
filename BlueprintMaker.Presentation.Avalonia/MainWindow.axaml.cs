using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;
using BlueprintMaker.Presentation.Avalonia.ViewModels;
using System;

namespace BlueprintMaker.Presentation.Avalonia;

public partial class MainWindow : Window
{
    private ImpostViewModel? _draggingImpost;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        // Подписываемся на движение мыши и отпускание на уровне окна
        this.PointerMoved += MainWindow_PointerMoved;
        this.PointerReleased += MainWindow_PointerReleased;
    }

    private void ElementPath_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is SelectableElement el)
        {
            if (el is ImpostViewModel impVM)
            {
                _draggingImpost = impVM;
                e.Pointer.Capture(ctrl);
            }

            // Также выделяем элемент
            if (el.SelectCommand != null)
                el.SelectCommand.Execute(el).Subscribe(_ => { }, ex => Console.WriteLine(ex.Message));

            e.Handled = true;
        }
    }

    private void MainWindow_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggingImpost != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var impost = _draggingImpost.SourceImpost;
            var parent = impost.Parent;

            if (parent != null)
            {
                var bounds = parent.GetContour().BoundingBox;
                double relativePos = 0;

                if (impost.Rule is ImpostPlacementRule r)
                {
                    if (r.Orientation == AxisOrientation.Vertical)
                        relativePos = bounds.Width > 0 ? (pos.X - bounds.MinX) / bounds.Width : 0.5;
                    else
                        relativePos = bounds.Height > 0 ? (pos.Y - bounds.MinY) / bounds.Height : 0.5;

                    // Меняем положение в Домене
                    impost.UpdatePosition(relativePos);

                    // Мгновенно перерисовываем дерево и геометрию
                    if (DataContext is MainViewModel mvm && mvm.SelectedFrame != null)
                    {
                        mvm.SelectedFrame.Refresh();
                    }
                }
            }
        }
    }

    private void MainWindow_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggingImpost != null)
        {
            // Отпускаем захват мыши
            if (e.Pointer.Captured is Control ctrl)
                e.Pointer.Capture(null);

            _draggingImpost = null;

            // В будущем здесь нужно создать команду MoveImpostCommand для Undo/Redo
        }
    }
}