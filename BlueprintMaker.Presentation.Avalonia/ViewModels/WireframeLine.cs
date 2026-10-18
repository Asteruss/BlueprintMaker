using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class WireframeLine : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;
    public PhysicalElement SourceElement { get; }
    public int SegmentIndex { get; }

    public WireframeLine(PhysicalElement element, int index, ContourVertex from, ContourVertex to, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceElement = element;
        SegmentIndex = index;
        var contour = Contour.CreateOpen(new[] { from, to });
        _geometry = GeometryExtensions.ToAvaloniaGeometry(contour);
        DisplayName = "Линия";
    }

    public double StrokeThickness => GeometryConstants.DisplayWidth;
    public IBrush StrokeColor => IsSelected ? Brushes.Blue : Brushes.Black;

    protected override void OnSelectedChanged()
    {
        this.RaisePropertyChanged(nameof(StrokeColor));
    }
}