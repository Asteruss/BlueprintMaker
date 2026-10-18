using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using DomainBoard = BlueprintMaker.Domain.Models.Materialisation.Board;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class BoardViewModel : ReactiveObject
{
    [Reactive] private StreamGeometry _geometry;

    public BoardViewModel(DomainBoard board)
    {
        _geometry = board.PhysicalContour?.ToAvaloniaGeometry() ?? new StreamGeometry();
    }
}