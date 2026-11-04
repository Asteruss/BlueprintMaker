using BlueprintMaker.Domain.Models;

namespace BlueprintMaker.Application.Commands;

public sealed class DeleteElementCommand : IUndoableCommand
{
    private readonly RemovalPlan _plan;
    private readonly List<(IWindowNode Node, int Index)> _removed = new();

    public DeleteElementCommand(RemovalPlan plan)
    {
        if (!plan.CanRemove)
            throw new InvalidOperationException(plan.BlockReason ?? "Удаление невозможно.");
        _plan = plan;
    }

    public void Execute()
    {
        var parent = (WindowNodeBase)_plan.Parent!;

        // Индексы запоминаются до удаления, иначе они сдвинутся
        _removed.Clear();
        foreach (var node in _plan.Nodes)
            _removed.Add((node, parent.Children.ToList().IndexOf(node)));

        foreach (var (node, _) in _removed)
            parent.RemoveChildInternal(node);
    }

    public void Undo()
    {
        var parent = (WindowNodeBase)_plan.Parent!;

        // По возрастанию индекса: каждая вставка встаёт на исходное место
        foreach (var (node, index) in _removed.OrderBy(r => r.Index))
            parent.InsertChildInternal(index, node);

        _removed.Clear();
    }
}