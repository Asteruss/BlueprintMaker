using Avalonia.Media;
using Avalonia.Input;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Rules;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class ImpostSegment : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;

    public PhysicalElement SourceImpost { get; }
    public int SegmentIndex { get; }

    public ImpostSegment(PhysicalElement impost, int index, ContourVertex from, ContourVertex to, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceImpost = impost;
        SegmentIndex = index;
        DisplayName = "Сегмент импоста";

        var contour = Contour.CreateOpen(new[] { from, to });
        _geometry = GeometryExtensions.ToAvaloniaGeometry(contour);
    }

    public IBrush StrokeColor => IsSelected ? Brushes.Blue : Brushes.Black;
    public double StrokeThickness => GeometryConstants.DisplayWidth;

    public Cursor DragCursor => SourceImpost is Impost imp && imp.Rule is ImpostPlacementRule r
        ? (r.Orientation == AxisOrientation.Vertical
            ? new Cursor(StandardCursorType.SizeWestEast)
            : new Cursor(StandardCursorType.SizeNorthSouth))
        : Cursor.Default;

    protected override void OnSelectedChanged()
    {
        base.OnSelectedChanged();
        this.RaisePropertyChanged(nameof(StrokeColor));
    }
}