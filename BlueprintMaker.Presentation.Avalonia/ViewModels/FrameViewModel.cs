using BlueprintMaker.Application;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class FrameViewModel : SelectableElement
{
    private readonly Frame _frame;
    private readonly DocumentHistory _history;
    private readonly ReactiveCommand<SelectableElement, Unit> _selectCmd;

    public ObservableCollection<WireframeLine> Lines { get; } = new();
    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<BoardViewModel> Boards { get; } = new();
    public ObservableCollection<SelectableElement> Children { get; } = new(); // Для дерева!
    public FrameAssembly Assembly => _frame.Assembly;

    [Reactive] private bool _isMaterialized;

    public FrameViewModel(Frame frame, DocumentHistory history, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        _frame = frame;
        _history = history;
        _selectCmd = selectCmd;
        DisplayName = "Рама";
        Refresh();
    }

    public void Refresh()
    {
        Lines.Clear();
        Nodes.Clear();
        Boards.Clear();
        Children.Clear();

        var contour = _frame.GetContour();
        if (contour?.Vertices == null) return;

        for (int i = 0; i < contour.Vertices.Count; i++)
        {
            var from = contour.Vertices[i];
            var to = contour.Vertices[(i + 1) % contour.Vertices.Count];
            var line = new WireframeLine(from, to, _selectCmd);
            Lines.Add(line);
            Children.Add(line);
        }

        if (_frame.Width > 0)
        {
            IsMaterialized = true;
            foreach (var board in _frame.Assembly.Boards)
            {
                var bvm = new BoardViewModel(board, _selectCmd);
                Boards.Add(bvm);
                Children.Add(bvm);
            }
        }
        else
        {
            IsMaterialized = false;
        }
    }
}