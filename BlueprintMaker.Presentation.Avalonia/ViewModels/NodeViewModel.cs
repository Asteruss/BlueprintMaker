using BlueprintMaker.Domain.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class NodeViewModel : SelectableElement
{
    public PhysicalElement Element { get; }
    public int Index { get; }

    [Reactive] private double _x;
    [Reactive] private double _y;

    public NodeViewModel(PhysicalElement element, int index, BlueprintMaker.Core.Geometry.Point pos, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        Element = element;
        Index = index;
        _x = pos.X;
        _y = pos.Y;
        DisplayName = "Узел";
    }
}