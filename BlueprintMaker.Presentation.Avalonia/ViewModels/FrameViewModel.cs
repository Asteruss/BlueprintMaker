using Avalonia.Media;
using BlueprintMaker.Application;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using BlueprintMaker.Domain.Models.Rules;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using AvaloniaPoint = Avalonia.Point;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class FrameViewModel : SelectableElement
{
    public readonly Frame Frame;
    private readonly DocumentHistory _history;
    private readonly ReactiveCommand<SelectableElement, Unit> _selectCmd;
    private readonly ReactiveCommand<JambSlot, Unit> _addJambCmd;

    // Коллекции для холста
    public ObservableCollection<WireframeLine> Lines { get; } = new();
    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<BoardViewModel> Boards { get; } = new();
    public ObservableCollection<LightSpaceViewModel> Spaces { get; } = new();
    public ObservableCollection<ImpostViewModel> Imposts { get; } = new();
    public ObservableCollection<SashViewModel> Sashes { get; } = new();
    public ObservableCollection<JambViewModel> Jambs { get; } = new();
    public ObservableCollection<JambSlotViewModel> JambSlots { get; } = new();
    public ObservableCollection<DimensionViewModel> Dimensions { get; } = new();
    [Reactive] private FrameDragHandle _frameDragHandle;
    [Reactive] private bool _isMaterialized;

    /// <summary>Режим показа стрелок создания добора.</summary>
    [Reactive] private bool _showJambSlots = true;
    private IWindowNode? _jambTarget;
    public FrameAssembly Assembly => Frame.Assembly;
    public double Zoom => AppSettings.Instance.Zoom;

    public FrameViewModel(
        Frame frame,
        DocumentHistory history,
        ReactiveCommand<SelectableElement, Unit> selectCmd,
        ReactiveCommand<JambSlot, Unit> addJambCmd) : base(selectCmd)
    {
        Frame = frame;
        _history = history;
        _selectCmd = selectCmd;
        _addJambCmd = addJambCmd;
        DisplayName = "Рама";

        AppSettings.Instance.WhenAnyValue(x => x.Zoom).Subscribe(_ => this.RaisePropertyChanged(nameof(Zoom)));
        this.WhenAnyValue(x => x.ShowJambSlots).Subscribe(_ => RefreshJambSlots());

        Refresh();
    }

    public void Refresh()
    {
        Lines.Clear();
        Nodes.Clear();
        Boards.Clear();
        Spaces.Clear();
        Sashes.Clear();
        Imposts.Clear();
        Jambs.Clear();
        Children.Clear();
        FrameDragHandle = null; 
        Dimensions.Clear();

        BuildTree(Frame, this);
        RefreshJambSlots();
    }

    public void SetJambTarget(IWindowNode? node)
    {
        _jambTarget = node;
        RefreshJambSlots();
    }

    private void RefreshJambSlots()
    {
        JambSlots.Clear();
        if (_jambTarget == null) return;

        foreach (var slot in JambSlotBuilder.Build(_jambTarget))
            JambSlots.Add(new JambSlotViewModel(slot, _addJambCmd));
    }

    private void BuildTree(IWindowNode node, SelectableElement parentVM)
    {
        if (node is Sash sash)
        {
            var sashVM = new SashViewModel(sash, _selectCmd);
            parentVM.Children.Add(sashVM);
            Sashes.Add(sashVM);

            sashVM.RefreshBoards();

            parentVM = sashVM;
        }
        else if (node is Frame frame)
        {
            // Линии рамы
            var contour = frame.GetContour();
            for (int i = 0; i < contour.Vertices.Count; i++)
            {
                var from = contour.Vertices[i];
                var to = contour.Vertices[(i + 1) % contour.Vertices.Count];
                var line = new WireframeLine(frame, i, from, to, _selectCmd);
                parentVM.Children.Add(line);
                Lines.Add(line);

                var ctrl = ComputeArcControlPoint(from.Position, to.Position, from.Bulge);
                if (ctrl.HasValue && frame.Width == 0)
                {
                    var arcNode = new NodeViewModel(frame, i, ctrl.Value, _selectCmd, NodeType.ArcControl);
                    parentVM.Children.Add(arcNode);
                    Nodes.Add(arcNode);
                }
            }

            if (frame.Width > 0)
            {
                IsMaterialized = true;
                int boardIndex = 0;
                foreach (var board in frame.Assembly.Boards)
                {
                    var bvm = new BoardViewModel(board, _selectCmd);
                    parentVM.Children.Add(bvm);
                    Boards.Add(bvm);

                    var p = board.StartOuter;
                    var nvm = new NodeViewModel(frame, boardIndex, p, _selectCmd, NodeType.FrameCorner);
                    parentVM.Children.Add(nvm);
                    Nodes.Add(nvm);

                    boardIndex++;
                }
            }
            else
            {
                IsMaterialized = false;
                for (int i = 0; i < contour.Vertices.Count; i++)
                {
                    var p = contour.Vertices[i].Position;
                    var nvm = new NodeViewModel(frame, i, p, _selectCmd, NodeType.FrameCorner);
                    parentVM.Children.Add(nvm);
                    Nodes.Add(nvm);
                }
            }
            FrameDragHandle = new FrameDragHandle(frame, _selectCmd);
            CreateOverallDimensions(frame);
        }
        else if (node is LightSpace ls)
        {
            var lsVM = new LightSpaceViewModel(ls, _selectCmd);
            parentVM.Children.Add(lsVM);
            Spaces.Add(lsVM);
            parentVM = lsVM;
        }
        else if (node is Impost impost)
        {
            impost.ReprojectToParent();
            var impVM = new ImpostViewModel(impost, _selectCmd);
            parentVM.Children.Add(impVM);
            Imposts.Add(impVM);

            var impContour = impost.GetContour();
            for (int i = 0; i < impContour.Vertices.Count; i++)
            {
                var p = impContour.Vertices[i].Position;
                var nodeType = (i == 0 || i == impContour.Vertices.Count - 1) ? NodeType.ImpostEnd : NodeType.ImpostSplit;
                var nvm = new NodeViewModel(impost, i, p, _selectCmd, nodeType);
                parentVM.Children.Add(nvm);
                if (impost.Width == 0)
                    Nodes.Add(nvm);
            }

            for (int i = 0; i < impContour.Vertices.Count - 1; i++)
            {
                var from = impContour.Vertices[i];
                var to = impContour.Vertices[i + 1];
                var ctrl = ComputeArcControlPoint(from.Position, to.Position, from.Bulge);
                if (ctrl.HasValue && impost.Width == 0)
                {
                    var arcNode = new NodeViewModel(impost, i, ctrl.Value, _selectCmd, NodeType.ArcControl);
                    parentVM.Children.Add(arcNode);
                    Nodes.Add(arcNode);
                }
            }

            parentVM = impVM;
        }
        else if (node is Jamb jamb)
        {
            var jvm = new JambViewModel(jamb, _selectCmd);
            parentVM.Children.Add(jvm);
            Jambs.Add(jvm);
            return;
        }
        foreach (var child in node.Children)
            BuildTree(child, parentVM);
    }

    private void CreateOverallDimensions(Frame frame)
    {
        var bounds = frame.GetContour().BoundingBox;

        // Учесть внешние доборы
        foreach (var child in frame.Children)
        {
            if (child is Jamb jamb && jamb.Side == JambSide.Outer && jamb.Width > 0)
            {
                var jb = jamb.GetContour().BoundingBox;
                bounds = bounds.Union(new Core.Geometry.Point(jb.MinX, jb.MinY));
                bounds = bounds.Union(new Core.Geometry.Point(jb.MaxX, jb.MaxY));
            }
        }

        const double offset = 30;
        const double tickLen = 5;  // засечки 5px

        double bottomY = bounds.MaxY + offset;
        double bCenterX = (bounds.MinX + bounds.MaxX) / 2.0;

        var bGeom = new StreamGeometry();
        using (var ctx = bGeom.Open())
        {
            // Размерная линия
            ctx.BeginFigure(new AvaloniaPoint(bounds.MinX, bottomY), false);
            ctx.LineTo(new AvaloniaPoint(bounds.MaxX, bottomY));
            ctx.EndFigure(false);
            // Засечки — вертикальные, на концах
            ctx.BeginFigure(new AvaloniaPoint(bounds.MinX, bottomY - tickLen), false);
            ctx.LineTo(new AvaloniaPoint(bounds.MinX, bottomY + tickLen));
            ctx.EndFigure(false);
            ctx.BeginFigure(new AvaloniaPoint(bounds.MaxX, bottomY - tickLen), false);
            ctx.LineTo(new AvaloniaPoint(bounds.MaxX, bottomY + tickLen));
            ctx.EndFigure(false);
        }

        var bDim = new DimensionViewModel(
            bGeom,
            $"{bounds.Width:F0}",
            bCenterX,         // центр
            bottomY - 8,      // над линией
            0,                // горизонтальный
            _selectCmd);
        Children.Add(bDim);
        Dimensions.Add(bDim);

        double leftX = bounds.MinX - offset;
        double lCenterY = (bounds.MinY + bounds.MaxY) / 2.0;

        var lGeom = new StreamGeometry();
        using (var ctx = lGeom.Open())
        {
            // Размерная линия
            ctx.BeginFigure(new AvaloniaPoint(leftX, bounds.MinY), false);
            ctx.LineTo(new AvaloniaPoint(leftX, bounds.MaxY));
            ctx.EndFigure(false);
            // Засечки — горизонтальные, на концах
            ctx.BeginFigure(new AvaloniaPoint(leftX - tickLen, bounds.MinY), false);
            ctx.LineTo(new AvaloniaPoint(leftX + tickLen, bounds.MinY));
            ctx.EndFigure(false);
            ctx.BeginFigure(new AvaloniaPoint(leftX - tickLen, bounds.MaxY), false);
            ctx.LineTo(new AvaloniaPoint(leftX + tickLen, bounds.MaxY));
            ctx.EndFigure(false);
        }

        var lDim = new DimensionViewModel(
            lGeom,
            $"{bounds.Height:F0}",
            leftX - 8,        // слева от линии
            lCenterY,         // центр
            -90,              // против часовой
            _selectCmd);
        Children.Add(lDim);
        Dimensions.Add(lDim);
    }

    /// <summary>
    /// Контрольная точка дуги (середина дуги) по концам и Bulge.
    /// null, если сегмент прямой.
    /// </summary>
    private static Point? ComputeArcControlPoint(Point from, Point to, double bulge)
    {
        if (Math.Abs(bulge) < GeometryConstants.Tolerance) return null;

        var mid = new Point((from.X + to.X) / 2.0, (from.Y + to.Y) / 2.0);

        var ab = new Point(to.X - from.X, to.Y - from.Y);
        double abLen = Math.Sqrt(ab.X * ab.X + ab.Y * ab.Y);
        if (abLen < 1e-9) return null;

        var perp = new Point(ab.Y / abLen, -ab.X / abLen);
        double s = abLen * bulge / 2.0;

        return new Point(mid.X + perp.X * s, mid.Y + perp.Y * s);
    }
}