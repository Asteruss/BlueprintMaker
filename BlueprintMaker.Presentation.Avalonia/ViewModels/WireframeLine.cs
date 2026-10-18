using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class WireframeLine : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;

    public WireframeLine(ContourVertex from, ContourVertex to, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        var contour = Contour.CreateOpen(new[] { from, to });
        Geometry = GeometryExtensions.ToAvaloniaGeometry(contour);
        DisplayName = "Линия";
    }

    public IBrush StrokeColor => IsSelected ? Brushes.Blue : Brushes.Black;

    protected override void OnSelectedChanged()
    {
        // Уведомляем UI, что цвет изменился
        this.RaisePropertyChanged(nameof(StrokeColor));
    }
}
