using BlueprintMaker.Application;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using DynamicData;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class FrameViewModel : SelectableElement
{
    private readonly Frame _frame;
    private readonly DocumentHistory _history;
    private readonly ReactiveCommand<SelectableElement, Unit> _selectCmd;

    // Коллекции для холста
    public ObservableCollection<WireframeLine> Lines { get; } = new();
    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<BoardViewModel> Boards { get; } = new();
    public ObservableCollection<LightSpaceViewModel> Spaces { get; } = new();
    public ObservableCollection<ImpostViewModel> Imposts { get; } = new();

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
        Spaces.Clear();
        Imposts.Clear();
        Children.Clear();

        // Запускаем рекурсивное построение
        BuildTree(_frame, this);
    }

    private void BuildTree(IWindowNode node, SelectableElement parentVM)
    {
        if (node is Frame frame)
        {
            // Линии рамы
            var contour = frame.GetContour();
            for (int i = 0; i < contour.Vertices.Count; i++)
            {
                var from = contour.Vertices[i];
                var to = contour.Vertices[(i + 1) % contour.Vertices.Count];

                // Передаем frame и i
                var line = new WireframeLine(frame, i, from, to, _selectCmd);
                parentVM.Children.Add(line);
                Lines.Add(line);
            }
            for (int i = 0; i < contour.Vertices.Count; i++)
            {
                var p = contour.Vertices[i].Position;
                var nvm = new NodeViewModel(frame, i, p, _selectCmd, NodeType.FrameCorner);
                parentVM.Children.Add(nvm);
                Nodes.Add(nvm);
            }
            // Доски рамы
            if (frame.Width > 0)
            {
                IsMaterialized = true;
                foreach (var board in frame.Assembly.Boards)
                {
                    var bvm = new BoardViewModel(board, _selectCmd);
                    parentVM.Children.Add(bvm);
                    Boards.Add(bvm);
                }
            }
        }
        else if (node is LightSpace ls)
        {
            var lsVM = new LightSpaceViewModel(ls, _selectCmd);
            parentVM.Children.Add(lsVM);
            Spaces.Add(lsVM);
            parentVM = lsVM; // Дети будут вложены в это пространство в дереве
        }
        else if (node is Impost impost)
        {
            var impVM = new ImpostViewModel(impost, _selectCmd);
            parentVM.Children.Add(impVM);
            Imposts.Add(impVM);

            var impContour = impost.GetContour();
            for (int i = 0; i < impContour.Vertices.Count; i++)
            {
                var p = impContour.Vertices[i].Position;
                var nodeType = (i == 0 || i == impContour.Vertices.Count - 1) ? NodeType.ImpostEnd : NodeType.ImpostSplit;
                var nvm = new NodeViewModel(impost, i, p, _selectCmd, nodeType);
                parentVM.Children.Add(nvm);
                Nodes.Add(nvm);
            }

            parentVM = impVM;
        }

        // Рекурсивно идем в детей
        foreach (var child in node.Children)
        {
            BuildTree(child, parentVM);
        }
    }
}