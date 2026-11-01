using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Application;          
using BlueprintMaker.Application.Commands;  
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class NodeViewModel : SelectableElement
{
    public PhysicalElement Element { get; }
    public int Index { get; }
    /// <summary>Для ArcControl — индекс сегмента, к которому относится контрольная точка дуги.</summary>
    public int SegmentIndex { get; }
    public NodeType NodeType { get; }

    [Reactive] private double _x;
    [Reactive] private double _y;

    // Запоминается в BeginDrag — старая позиция/кривизна для Undo-команды в EndDrag
    private Point _dragStartPos;
    private double _dragStartBulge;
    private bool _isDragging;
    private IDerivationRule? _dragStartRule;

    public NodeViewModel(PhysicalElement element, int index, Point pos, ReactiveCommand<SelectableElement, Unit> selectCmd, NodeType type) : base(selectCmd)
    {
        Element = element;
        Index = index;
        SegmentIndex = index;
        NodeType = type;
        _x = pos.X;
        _y = pos.Y;
        DisplayName = $"Узел ({type})";
    }

    public IBrush FillColor => IsSelected ? Brushes.Blue :
        NodeType == NodeType.FrameCorner ? Brushes.Yellow :
        NodeType == NodeType.ImpostEnd ? Brushes.Orange :
        NodeType == NodeType.ArcControl ? Brushes.Green :
        Brushes.Red; // ImpostSplit

    /// <summary>
    /// Вызывается в начале drag (PointerPressed). Запоминает начальную позицию узла
    /// и (для ArcControl) начальный Bulge сегмента — для создания Undo-команды в EndDrag.
    /// </summary>

    public void BeginDrag()
    {
        _isDragging = true;
        _dragStartPos = new Point(X, Y);

        // Сохранить Rule ДО DragTo — DragTo вызывает MoveNode → FreezeGeometry → Rule = null
        _dragStartRule = Element.Rule;

        if (NodeType == NodeType.ArcControl)
        {
            var contour = Element.GetContour();
            _dragStartBulge = (SegmentIndex >= 0 && SegmentIndex < contour.Vertices.Count)
                ? contour.Vertices[SegmentIndex].Bulge : 0;
        }
    }

    /// <summary>
    /// Двигает узел в новую позицию и обновляет элемент напрямую (без истории —
    /// для интерактива во время drag). Вся логика — внутри метода
    /// </summary>
    public void DragTo(Point newPos)
    {
        X = newPos.X;
        Y = newPos.Y;

        switch (NodeType)
        {
            case NodeType.FrameCorner:
                Element.MoveNode(Index, newPos);
                break;

            case NodeType.ImpostEnd:
                var parent = Element.Parent;
                if (parent == null) return;

                var boundary = parent.GetContour();
                double minDist = 20.0;
                Point? snappedPos = null;

                for (int i = 0; i < boundary.Vertices.Count; i++)
                {
                    var from = boundary.Vertices[i];
                    var to = boundary.Vertices[(i + 1) % boundary.Vertices.Count];
                    var proj = SegmentProjection.Project(from.Position, to.Position, from.Bulge, newPos);

                    if (proj.Distance < minDist)
                    {
                        minDist = proj.Distance;
                        snappedPos = proj.ClosestPoint;
                    }
                }

                if (snappedPos == null) return;
                Element.MoveNode(Index, snappedPos.Value);
                break;

            case NodeType.ImpostSplit:
                Element.MoveNode(Index, newPos);
                break;

            case NodeType.ArcControl:
                Element.SetSegmentArcByControlPoint(SegmentIndex, newPos);
                break;
        }
    }

    /// <summary>
    /// Вызывается в конце drag (PointerReleased). Создаёт Undo-команду с
    /// начальной позицией/кривизной (от BeginDrag) и финальной позицией.
    /// Возвращает null, если drag не был начат
    /// </summary>
    public IUndoableCommand? EndDrag()
    {
        if (!_isDragging) return null;
        _isDragging = false;

        var finalPos = new Point(X, Y);

        switch (NodeType)
        {
            case NodeType.FrameCorner:
            case NodeType.ImpostEnd:
            case NodeType.ImpostSplit:
                return new MoveNodeCommand(Element, Index, _dragStartPos, finalPos, _dragStartRule);

            case NodeType.ArcControl:
                return new MoveArcControlPointCommand(Element, SegmentIndex, _dragStartBulge, finalPos);

            default:
                return null;
        }
    }

    protected override void OnSelectedChanged()
    {
        base.OnSelectedChanged();
        this.RaisePropertyChanged(nameof(FillColor));
    }
}