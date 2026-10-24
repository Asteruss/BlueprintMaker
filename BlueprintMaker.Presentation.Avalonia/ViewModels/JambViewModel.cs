using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Media;
using BlueprintMaker.Domain.Models.Elements;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class JambViewModel : SelectableElement
{
    public Jamb SourceJamb { get; }

    public ObservableCollection<BoardViewModel> Boards { get; } = new();
    public bool IsMaterialized => SourceJamb.Width > 0;
    public IBrush StrokeColor => IsSelected ? Brushes.Blue : Brushes.Black;
    public IBrush FillColor => Brushes.Gray;


    /// <summary>Контур для немaтериализованного добора (для материализованного null).</summary>
    public Geometry? Outline { get; private set; }

    public JambViewModel(Jamb jamb, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceJamb = jamb;
        DisplayName = jamb.Side == JambSide.Inner ? "Добор (внутр.)" : "Добор (внеш.)";
        UpdateState();
    }

    public void UpdateState()
    {
        Boards.Clear();
        Outline = null;

        if (SourceJamb.Width > 0)
        {
            var board = SourceJamb.Board;
            if (board != null)
                Boards.Add(new BoardViewModel(board, SelectCommand));
        }
        else
        {
            Outline = SourceJamb.GetContour().ToAvaloniaGeometry();
        }
    }

    protected override void OnSelectedChanged() { }
}