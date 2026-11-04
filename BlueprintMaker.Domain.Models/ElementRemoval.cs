using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models;

public sealed class RemovalPlan
{
    public IWindowNode? Parent { get; init; }
    public IReadOnlyList<IWindowNode> Nodes { get; init; } = Array.Empty<IWindowNode>();
    public string? BlockReason { get; init; }

    public bool CanRemove => BlockReason == null && Parent != null && Nodes.Count > 0;
}

/// <summary>Планирует удаление элемента: что уйдёт вместе с ним и что мешает.</summary>
public static class ElementRemoval
{
    public static RemovalPlan Plan(IWindowNode node)
    {
        if (node is LightSpace space)
            return Blocked(SpaceHint(space));

        var parent = node.Parent;
        if (parent == null)
            return Blocked("Окно целиком удалить нельзя. Чтобы начать заново, создайте новое окно (Ctrl+N).");

        // Световые пространства, созданные вместе с элементом (половины импоста, остаток добора).
        // Вставка в пространство возможна только когда оно пусто, поэтому все его дети — одна группа.
        var companions = parent is LightSpace
            ? parent.Children.Where(c => c != node).ToList()
            : new List<IWindowNode>();

        var blockers = node.Children.Concat(companions)
            .Where(c => !IsEmptySpace(c))
            .ToList();

        if (blockers.Count > 0)
        {
            var names = new List<string>();
            foreach (var b in blockers) Collect(b, names);

            return Blocked($"Нельзя удалить «{Describe(node)}»: есть связанные элементы " +
                           $"({string.Join(", ", names.Distinct())}). Сначала удалите их.");
        }

        var nodes = new List<IWindowNode> { node };
        nodes.AddRange(companions);

        return new RemovalPlan { Parent = parent, Nodes = nodes };
    }

    public static string Describe(IWindowNode n) => n switch
    {
        Sash => "створка",          // раньше Frame: Sash — его наследник
        Frame => "рама",
        Impost => "импост",
        Jamb => "добор",
        LightSpace => "световое пространство",
        _ => n.GetType().Name
    };

    private static bool IsEmptySpace(IWindowNode n) => n is LightSpace && n.Children.Count == 0;

    private static void Collect(IWindowNode n, List<string> names)
    {
        if (n is LightSpace)
        {
            foreach (var c in n.Children) Collect(c, names);
            return;
        }
        names.Add(Describe(n));
    }

    private static string SpaceHint(LightSpace space)
    {
        var parent = space.Parent;
        if (parent is LightSpace)
        {
            if (parent.Children.Any(c => c is Impost))
                return "Это световое пространство создано импостом. Чтобы убрать его, удалите импост.";
            if (parent.Children.Any(c => c is Jamb))
                return "Это световое пространство осталось после добора. Чтобы убрать его, удалите добор.";
        }
        return "Световое пространство рамы или створки удалить нельзя: оно существует всегда.";
    }

    private static RemovalPlan Blocked(string reason) => new() { BlockReason = reason };
}