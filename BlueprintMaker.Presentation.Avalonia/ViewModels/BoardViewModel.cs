using Avalonia.Media;
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
}

