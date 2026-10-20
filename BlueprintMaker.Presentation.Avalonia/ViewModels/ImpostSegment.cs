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

    public ContourVertex From { get; }
    public ContourVertex To { get; }

    /// <summary>Кривизна сегмента</summary>
    public double Bulge => From.Bulge;

    /// <summary>True, если сегмент — дуга</summary>
    public bool IsArc => Math.Abs(From.Bulge) > GeometryConstants.Tolerance;
    /// <summary>Контрольная точка дуги (середина дуги) или null, если прямая</summary>
    public Point? ControlPoint
    {
        get
        {
            if (!IsArc) return null;
            var arc = BulgeMath.GetArcParameters(From.Position, To.Position, From.Bulge);
            double midAngle = arc.StartAngle + arc.SweepAngle / 2;
            return new Point(
                arc.Center.X + arc.Radius * Math.Cos(midAngle),
                arc.Center.Y + arc.Radius * Math.Sin(midAngle));
        }
    }

    public ImpostSegment(PhysicalElement impost, int index, ContourVertex from, ContourVertex to, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceImpost = impost;
        SegmentIndex = index;
        From = from;
        To = to;
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