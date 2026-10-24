using BlueprintMaker.Application;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.ObjectModel;
using System.Reactive;

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