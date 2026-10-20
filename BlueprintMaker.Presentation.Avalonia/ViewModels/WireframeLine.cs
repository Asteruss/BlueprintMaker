using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class WireframeLine : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;
    public PhysicalElement SourceElement { get; }
    public int SegmentIndex { get; }
    public ContourVertex From { get; }
    public ContourVertex To { get; }

    /// <summary>Кривизна сегмента</summary>
    public double Bulge => From.Bulge;

    /// <summary>True, если сегмент — дуга.</summary>
    public bool IsArc => Math.Abs(From.Bulge) > GeometryConstants.Tolerance;
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
    public WireframeLine(PhysicalElement element, int index, ContourVertex from, ContourVertex to, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceElement = element;
        SegmentIndex = index;
        From = from;
        To = to;
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