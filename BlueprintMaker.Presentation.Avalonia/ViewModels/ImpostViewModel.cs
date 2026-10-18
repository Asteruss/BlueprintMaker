using Avalonia.Input;
using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Rules;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class ImpostViewModel : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;
    public Impost SourceImpost { get; }

    public ImpostViewModel(Impost impost, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceImpost = impost;
        DisplayName = "Импост";
        _geometry = impost.GetContour()?.ToAvaloniaGeometry() ?? new StreamGeometry();
    }

    public IBrush StrokeColor => IsSelected ? Brushes.Blue : Brushes.Black;
    public double StrokeThickness => GeometryConstants.DisplayWidth;

    // Курсор для перетаскивания
    public Cursor DragCursor => SourceImpost.Rule is ImpostPlacementRule r
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