using BlueprintMaker.Application;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class FrameViewModel : ReactiveObject
{
    private readonly Frame _frame;
    private readonly DocumentHistory _history;

    public ObservableCollection<WireframeLine> Lines { get; } = new();
    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<BoardViewModel> Boards { get; } = new();

    // Доступ к сборке досок для управления стыками
    public FrameAssembly Assembly => _frame.Assembly;

    [Reactive] private bool _isMaterialized;

    public FrameViewModel(Frame frame, DocumentHistory history)
    {
        _frame = frame;
        _history = history;
        Refresh();
    }

    public void Refresh()
    {
        Lines.Clear();
        Nodes.Clear();
        Boards.Clear();

        var contour = _frame.GetContour();
        if (contour?.Vertices == null) return;

        // Генерируем скелет (узлы и линии)
        for (int i = 0; i < contour.Vertices.Count; i++)
        {
            var from = contour.Vertices[i];
            var to = contour.Vertices[(i + 1) % contour.Vertices.Count];
            Lines.Add(new WireframeLine(from, to));
            Nodes.Add(new NodeViewModel(_frame, i, from.Position, _history));
        }

        // Генерируем доски, если рама материализована
        if (_frame.Width > 0)
        {
            IsMaterialized = true;
            foreach (var board in _frame.Assembly.Boards)
            {
                Boards.Add(new BoardViewModel(board));
            }
        }
        else
        {
            IsMaterialized = false;
        }
    }
}