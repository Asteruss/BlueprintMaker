using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class NodeViewModel : SelectableElement
{
    public PhysicalElement Element { get; }
    public int Index { get; }
    public NodeType NodeType { get; }

    [Reactive] private double _x;
    [Reactive] private double _y;

    public NodeViewModel(PhysicalElement element, int index, Point pos, ReactiveCommand<SelectableElement, Unit> selectCmd, NodeType type) : base(selectCmd)
    {
        Element = element;
        Index = index;
        NodeType = type;
        _x = pos.X;
        _y = pos.Y;
        DisplayName = $"Узел ({type})";
    }

    // Разные цвета для разных узлов
    public IBrush FillColor => IsSelected ? Brushes.Blue :
        NodeType == NodeType.FrameCorner ? Brushes.Yellow :
        NodeType == NodeType.ImpostEnd ? Brushes.Orange :
        Brushes.Red; // ImpostSplit

    protected override void OnSelectedChanged()
    {
        base.OnSelectedChanged();
        this.RaisePropertyChanged(nameof(FillColor));
    }
}