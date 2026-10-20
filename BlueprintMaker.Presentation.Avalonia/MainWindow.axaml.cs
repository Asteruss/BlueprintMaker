using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Rules;
using BlueprintMaker.Presentation.Avalonia.ViewModels;
using System;
using CorePoint = BlueprintMaker.Core.Geometry.Point;

namespace BlueprintMaker.Presentation.Avalonia;

public partial class MainWindow : Window
{
    private Impost? _draggingImpost;
    private NodeViewModel? _draggingNode;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        this.PointerMoved += MainWindow_PointerMoved;
        this.PointerReleased += MainWindow_PointerReleased;
    }

    private void ElementPath_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is SelectableElement el)
        {
            if (el is WireframeLine && DataContext is MainViewModel mvm)
            {
                var pos = e.GetPosition(DrawingCanvas);
                mvm.LastClickPoint = new CorePoint(pos.X, pos.Y);
            }

            if (el is ImpostSegment impSeg)
            {
                var pos = e.GetPosition(DrawingCanvas);
                if (DataContext is MainViewModel mvm2)
                    mvm2.LastClickPoint = new CorePoint(pos.X, pos.Y);

                _draggingImpost = impSeg.SourceImpost as Impost;
            }

            if (el.SelectCommand != null)
                el.SelectCommand.Execute(el).Subscribe(_ => { }, ex => Console.WriteLine(ex.Message));

            e.Handled = true;
        }
    }

    private void Node_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is NodeViewModel nodeVM)
        {
            _draggingNode = nodeVM;
            nodeVM.BeginDrag();   // запоминаем старую позицию/кривизну

            // Выделяем узел
            nodeVM.SelectCommand?.Execute(nodeVM).Subscribe(_ => { });

            e.Handled = true;
        }
    }

    private void MainWindow_PointerMoved(object? sender, PointerEventArgs e)
    {
        // Движение импоста (не node) — оставлено в code-behind, как было
        if (_draggingImpost != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var impost = _draggingImpost;
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

                    impost.UpdatePosition(relativePos);

                    if (DataContext is MainViewModel mvm && mvm.SelectedFrame != null)
                        mvm.SelectedFrame.Refresh();
                }
            }
        }

        // Движение node — делегировано в NodeViewModel.DragTo
        if (_draggingNode != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var corePos = new Core.Geometry.Point(pos.X, pos.Y);

            _draggingNode.DragTo(corePos);

            if (DataContext is MainViewModel mvm2 && mvm2.SelectedFrame != null)
                mvm2.SelectedFrame.Refresh();
        }
    }

    private void MainWindow_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Движение импоста (не node) — оставлено
        _draggingImpost = null;

        // Движение node — создаём Undo-команду и сохраняем в истории
        if (_draggingNode != null)
        {
            var cmd = _draggingNode.EndDrag();
            _draggingNode = null;

            if (cmd != null && DataContext is MainViewModel mvm)
            {
                mvm.ExecuteCommand(cmd);
                mvm.SelectedFrame?.Refresh();
            }
        }
    }
}