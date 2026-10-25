using Avalonia.Media;
using BlueprintMaker.Domain.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class DimensionViewModel : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;
    [Reactive] private string _text;
    [Reactive] private double _textX;
    [Reactive] private double _textY;
    [Reactive] private double _angle;

    public DimensionViewModel(StreamGeometry geometry, string text,
        double textX, double textY, double angle,
        ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        _geometry = geometry;
        _text = text;
        _textX = textX;
        _textY = textY;
        _angle = angle;
        DisplayName = "Размер";
    }

    protected override void OnSelectedChanged() { }
}