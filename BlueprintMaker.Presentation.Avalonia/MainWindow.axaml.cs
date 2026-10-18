using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.TextFormatting.Unicode;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;
using BlueprintMaker.Presentation.Avalonia.ViewModels;
using System;

namespace BlueprintMaker.Presentation.Avalonia;

public partial class MainWindow : Window
{
    private ImpostViewModel? _draggingImpost;
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
            if (el is ImpostViewModel impVM)
            {
                _draggingImpost = impVM;
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

            // Выделяем узел
            nodeVM.SelectCommand?.Execute(nodeVM).Subscribe(_ => { });

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

                    impost.UpdatePosition(relativePos);

                    if (DataContext is MainViewModel mvm && mvm.SelectedFrame != null)
                        mvm.SelectedFrame.Refresh();
                }
            }
        }

        if (_draggingNode != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var corePos = new Core.Geometry.Point(pos.X, pos.Y);

            if (_draggingNode.NodeType == NodeType.ImpostEnd)
            {
                var parent = _draggingNode.Element.Parent;
                if (parent != null)
                {
                    var boundary = parent.GetContour();
                    double minDist = 20.0;
                    Core.Geometry.Point? snappedPos = null; // Делаем nullable

                    for (int i = 0; i < boundary.Vertices.Count; i++)
                    {
                        var from = boundary.Vertices[i];
                        var to = boundary.Vertices[(i + 1) % boundary.Vertices.Count];
                        var proj = SegmentProjection.Project(from.Position, to.Position, from.Bulge, corePos);

                        if (proj.Distance < minDist)
                        {
                            minDist = proj.Distance;
                            snappedPos = proj.ClosestPoint;
                        }
                    }

                    if (snappedPos == null) return;

                    corePos = snappedPos.Value;
                }
            }

            _draggingNode.Element.MoveNode(_draggingNode.Index, corePos);

            if (DataContext is MainViewModel mvm2 && mvm2.SelectedFrame != null)
                mvm2.SelectedFrame.Refresh();
        }
    }

    private void MainWindow_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _draggingImpost = null;
        _draggingNode = null;

        // В будущем здесь нужно создать Undo-команду (MoveNodeCommand)
    }
}