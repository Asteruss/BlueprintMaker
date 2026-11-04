using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using BlueprintMaker.Application.Commands;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Rules;
using BlueprintMaker.Presentation.Avalonia.Helpers;
using BlueprintMaker.Presentation.Avalonia.ViewModels;
using BlueprintMaker.Presentation.Avalonia.Views;
using System;
using System.Reactive;
using AvaloniaPoint = global::Avalonia.Point;
using CorePoint = BlueprintMaker.Core.Geometry.Point;


namespace BlueprintMaker.Presentation.Avalonia;

public partial class MainWindow : Window
{
    private Impost? _draggingImpost;
    private NodeViewModel? _draggingNode;
    private double _dragStartRelativePos;
    private FrameDragHandle? _draggingHandle;
    private AvaloniaPoint _dragStartCanvasPos;
    private CorePoint _dragStartFrameMin;
    private bool _panning;
    private AvaloniaPoint _panStartPos;
    public MainWindow()
    {
        InitializeComponent();
        var mvm = new MainViewModel();
        DataContext = mvm;
        RegisterDialogs();
        this.PointerMoved += MainWindow_PointerMoved;
        this.PointerReleased += MainWindow_PointerReleased;

        DrawingCanvas.SizeChanged += (s, e) =>
        {
            mvm.UpdateCanvasSize(e.NewSize.Width, e.NewSize.Height);
        };

        DrawingCanvas.PointerWheelChanged += (s, e) =>
        {
            double delta = e.Delta.Y > 0 ? 0.1 : -0.1;
            AppSettings.Instance.Zoom = MathCompat.Clamp(AppSettings.Instance.Zoom + delta, 0.2, 5.0);
            e.Handled = true;
        };

        DrawingCanvas.PointerPressed += Canvas_PointerPressed;

        Closing += OnWindowClosing;
        Opened += async (_, _) =>
        {
            if (DataContext is MainViewModel vm) await vm.TryRecoverAutosaveAsync();
        };
    }

    private bool _closeConfirmed;

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || DataContext is not MainViewModel vm) return;

        if (!vm.HasUnsavedChanges)
        {
            vm.DeleteAutosave();
            return;
        }

        e.Cancel = true;   // решение примем после ответа пользователя
        if (await vm.ConfirmCanDiscardAsync())
        {
            vm.DeleteAutosave();
            _closeConfirmed = true;
            Close();
        }
    }

    private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            _panning = true;
            _panStartPos = e.GetPosition(DrawingCanvas);
            e.Handled = true;
            return;
        }

        if (!e.Handled && DataContext is MainViewModel mvm)
        {
            mvm.Unselect();
        }
    }

    private void ElementPath_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is SelectableElement el)
        {
            if (el is WireframeLine && DataContext is MainViewModel mvm)
            {
                var pos = GetFramePoint(e);
                mvm.LastClickPoint = new CorePoint(pos.X, pos.Y);
            }

            if (el is ImpostSegment impSeg)
            {
                var pos = GetFramePoint(e);
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
        if (_panning)
        {
            var pos = e.GetPosition(DrawingCanvas);
            AppSettings.Instance.PanX += pos.X - _panStartPos.X;
            AppSettings.Instance.PanY += pos.Y - _panStartPos.Y;
            _panStartPos = pos;
            return;
        }
        if (_draggingHandle != null)
        {
            var pos = GetFramePoint(e).ToAvaloniaPoint();
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
            var pos = GetFramePoint(e);
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
            var pos = GetFramePoint(e);
            var corePos = new CorePoint(pos.X, pos.Y);

            _draggingNode.DragTo(corePos);

            if (DataContext is MainViewModel mvm2 && mvm2.SelectedFrame != null)
                mvm2.SelectedFrame.Refresh();
        }
    }

    private void MainWindow_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _panning = false;
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
            _dragStartCanvasPos = GetFramePoint(e).ToAvaloniaPoint();
            var bounds = _draggingHandle.SourceFrame.GetContour().BoundingBox;
            _dragStartFrameMin = new CorePoint(bounds.MinX, bounds.MinY);
            e.Handled = true;
        }
    }

    private CorePoint GetFramePoint(PointerEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);
        double zoom = AppSettings.Instance.Zoom;
        if (zoom <= 0) zoom = 1.0;
        double panX = AppSettings.Instance.PanX;
        double panY = AppSettings.Instance.PanY;
        return new CorePoint(Math.Ceiling((pos.X - panX) / zoom), Math.Ceiling((pos.Y - panY) / zoom));
    }

    private CorePoint GetFramePoint(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);
        double zoom = AppSettings.Instance.Zoom;
        if (zoom <= 0) zoom = 1.0;
        double panX = AppSettings.Instance.PanX;
        double panY = AppSettings.Instance.PanY;
        return new CorePoint(Math.Ceiling((pos.X - panX) / zoom), Math.Ceiling((pos.Y - panY) / zoom));

    }

    private void ResetZoom_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        AppSettings.Instance.Zoom = 1.0;
    }

    private void RegisterDialogs()
    {
        if (DataContext is not MainViewModel vm) return;

        vm.AskJambWidth.RegisterHandler(async ctx =>
        {
            var dialog = new JambWidthDialog(ctx.Input);
            var result = await dialog.ShowDialog<double?>(this);
            ctx.SetOutput(result);
        });

        vm.AskNewWindow.RegisterHandler(async ctx =>
        {
            var dialog = new NewWindowDialog();
            var result = await dialog.ShowDialog<Contour?>(this);
            ctx.SetOutput(result);
        });

        var fileType = new FilePickerFileType("Окно BlueprintMaker") { Patterns = new[] { "*.bpmaker" } };

        vm.AskUnsaved.RegisterHandler(async ctx =>
        {
            var d = new ChoiceDialog(ctx.Input, "Сохранить", "Не сохранять", "Отмена");
            ctx.SetOutput(await d.ShowDialog<ChoiceResult>(this));
        });

        vm.AskRecover.RegisterHandler(async ctx =>
        {
            var d = new ChoiceDialog(ctx.Input, "Восстановить", "Удалить", "Позже");
            ctx.SetOutput(await d.ShowDialog<ChoiceResult>(this));
        });

        vm.AskOpenPath.RegisterHandler(async ctx =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Открыть окно",
                AllowMultiple = false,
                FileTypeFilter = new[] { fileType }
            });
            ctx.SetOutput(files.Count > 0 ? files[0].TryGetLocalPath() : null);
        });

        vm.AskSavePath.RegisterHandler(async ctx =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить окно",
                SuggestedFileName = ctx.Input,
                DefaultExtension = "bpmaker",
                FileTypeChoices = new[] { fileType },
                ShowOverwritePrompt = true
            });
            ctx.SetOutput(file?.TryGetLocalPath());
        });

        vm.AskMaterialize.RegisterHandler(async ctx =>
        {
            var dialog = new MaterializeDialog(ctx.Input);
            ctx.SetOutput(await dialog.ShowDialog<MaterializeOptions?>(this));
        });

        vm.ShowMessage.RegisterHandler(async ctx =>
        {
            var d = new ChoiceDialog(ctx.Input, "OK", "", "");
            await d.ShowDialog<ChoiceResult>(this);
            ctx.SetOutput(Unit.Default);
        });
    }


}