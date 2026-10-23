using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using BlueprintMaker.Application.Commands;
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
    private double _dragStartRelativePos;
    private FrameDragHandle? _draggingHandle;
    private global::Avalonia.Point _dragStartCanvasPos;
    private CorePoint _dragStartFrameMin;
    public MainWindow()
    {
        InitializeComponent();
        var mvm = new MainViewModel();
        DataContext = mvm;

        this.PointerMoved += MainWindow_PointerMoved;
        this.PointerReleased += MainWindow_PointerReleased;

        DrawingCanvas.SizeChanged += (s, e) =>
        {
            mvm.UpdateCanvasSize(e.NewSize.Width, e.NewSize.Height);
        };
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
                if (_draggingImpost?.Rule is ImpostPlacementRule r)
                    _dragStartRelativePos = r.RelativePosition;
            }

            el.SelectCommand?.Execute(el).Subscribe(_ => { }, ex => Console.WriteLine(ex.Message));

            e.Handled = true;
        }
    }

    private void Node_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is NodeViewModel nodeVM)
        {
            _draggingNode = nodeVM;
            nodeVM.BeginDrag();

            nodeVM.SelectCommand?.Execute(nodeVM).Subscribe(_ => { });

            e.Handled = true;
        }
    }

    private void MainWindow_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggingHandle != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var dx = pos.X - _dragStartCanvasPos.X;
            var dy = pos.Y - _dragStartCanvasPos.Y;
            _draggingHandle.SourceFrame.MoveBy(dx, dy);
            _dragStartCanvasPos = pos;

            if (DataContext is MainViewModel mvm && mvm.SelectedFrame != null)
                mvm.SelectedFrame.Refresh();
            return;
        }

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

        if (_draggingNode != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var corePos = new CorePoint(pos.X, pos.Y);

            _draggingNode.DragTo(corePos);

            if (DataContext is MainViewModel mvm2 && mvm2.SelectedFrame != null)
                mvm2.SelectedFrame.Refresh();
        }
    }

    private void MainWindow_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggingHandle != null)
        {
            var finalBounds = _draggingHandle.SourceFrame.GetContour().BoundingBox;
            var totalDx = finalBounds.MinX - _dragStartFrameMin.X;
            var totalDy = finalBounds.MinY - _dragStartFrameMin.Y;

            var cmd = new MoveFrameCommand(_draggingHandle.SourceFrame, totalDx, totalDy);
            if (DataContext is MainViewModel mvm)
            {
                mvm.RecordCommand(cmd);
                mvm.SelectedFrame?.Refresh();
            }
            _draggingHandle = null;
        }

        if (_draggingImpost != null)
        {
            if (_draggingImpost.Rule is ImpostPlacementRule r)
            {
                var cmd = new UpdateImpostPositionCommand(
                    _draggingImpost, _dragStartRelativePos, r.RelativePosition);
                if (DataContext is MainViewModel mvm)
                {
                    mvm.ExecuteCommand(cmd);
                    mvm.SelectedFrame?.Refresh();
                }
            }
            _draggingImpost = null;
        }

        if (_draggingNode != null)
        {
            var cmd = _draggingNode.EndDrag();
            _draggingNode = null;

            if (cmd != null && DataContext is MainViewModel mvm2)
            {
                mvm2.ExecuteCommand(cmd);
                mvm2.SelectedFrame?.Refresh();
            }
        }
    }

    private void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is FrameViewModel fvm && fvm.FrameDragHandle != null)
        {
            _draggingHandle = fvm.FrameDragHandle;
            _dragStartCanvasPos = e.GetPosition(DrawingCanvas);
            var bounds = _draggingHandle.SourceFrame.GetContour().BoundingBox;
            _dragStartFrameMin = new CorePoint(bounds.MinX, bounds.MinY);
            e.Handled = true;
        }
    }
    private void ResetZoom_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        AppSettings.Instance.Zoom = 1.0;
    }
}