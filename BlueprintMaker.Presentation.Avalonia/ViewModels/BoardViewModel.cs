using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using DomainBoard = BlueprintMaker.Domain.Models.Materialisation.Board;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class BoardViewModel : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;
    public DomainBoard SourceBoard { get; }

    public BoardViewModel(DomainBoard board, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceBoard = board;
        Geometry = board.PhysicalContour?.ToAvaloniaGeometry() ?? new StreamGeometry();
        DisplayName = "Доска";
    }

    public IBrush FillColor => IsSelected ? Brushes.Blue : Brushes.Black;

    protected override void OnSelectedChanged()
    {
        this.RaisePropertyChanged(nameof(FillColor));
    }
    /// <summary>Ширина доски (от материализации).</summary>
    public double BoardWidth => SourceBoard?.Width ?? 0;

    /// <summary>
    /// Длина доски — максимальное расстояние между двумя смежными ContourVertex
    /// в PhysicalContour. Для прямоугольной доски — это длинная сторона (outer или inner).
    /// </summary>
    public double BoardLength =>
    SourceBoard?.PhysicalContour == null
        ? 0
        : ContourMath.LongestEdgeLength(SourceBoard.PhysicalContour);
}

