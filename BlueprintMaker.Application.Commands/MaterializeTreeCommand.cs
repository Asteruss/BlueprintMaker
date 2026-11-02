using BlueprintMaker.Domain.Models;

namespace BlueprintMaker.Application.Commands;

/// <summary>Материализует рамку и всё её содержимое одной командой истории.</summary>
public sealed class MaterializeTreeCommand : IUndoableCommand
{
    private readonly List<(PhysicalElement Element, double OldWidth)> _targets;
    private readonly double _width;
    private readonly List<MaterializeCommand> _done = new();

    private MaterializeTreeCommand(List<(PhysicalElement, double)> targets, double width)
    {
        _targets = targets;
        _width = width;
    }

    /// <summary>Количество элементов, которые будут изменены.</summary>
    public int Count => _targets.Count;

    /// <summary>
    /// Собирает элементы сверху вниз. overwrite = false: уже материализованные пропускаются.
    /// overwrite = true: берутся все, у кого ширина отличается от новой.
    /// </summary>
    public static MaterializeTreeCommand Create(IWindowNode root, double width, bool overwrite, out int skipped)
    {
        var targets = new List<(PhysicalElement, double)>();
        int skip = 0;

        void Visit(IWindowNode node)
        {
            if (node is PhysicalElement pe)
            {
                bool take = pe.Width <= 0 || (overwrite && Math.Abs(pe.Width - width) > 1e-9);
                if (take) targets.Add((pe, pe.Width));
                else skip++;
            }

            foreach (var child in node.Children)
                Visit(child);
        }

        Visit(root);
        skipped = skip;
        return new MaterializeTreeCommand(targets, width);
    }

    public void Execute()
    {
        _done.Clear();
        foreach (var (element, _) in _targets)
        {
            var command = new MaterializeCommand(element, _width);
            command.Execute();
            _done.Add(command);
        }
    }

    public void Undo()
    {
        // С конца: сначала дети, потом родители
        for (int i = _done.Count - 1; i >= 0; i--)
        {
            _done[i].Undo();

            // Если элемент был материализован раньше, возвращаем его прежнюю ширину
            var (element, oldWidth) = _targets[i];
            if (oldWidth > 0) element.Materialize(oldWidth);
        }
        _done.Clear();
    }
}