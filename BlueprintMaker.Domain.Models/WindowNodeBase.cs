using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models;

/// <summary>
/// Базовый класс для всех элементов окна. Содержит логику хранения 
/// локальной геометрии и правила вывода (Стратегии).
/// </summary>
public abstract class WindowNodeBase : IWindowNode
{
    public Guid Id { get; }
    public IWindowNode? Parent { get; private set; }

    private readonly List<IWindowNode> _children = new();
    public IReadOnlyList<IWindowNode> Children => _children;

    private Contour? _localContour;

    private IDerivationRule? _rule;

    protected WindowNodeBase(Guid id, Contour? localContour, IDerivationRule? rule)
    {
        Id = id;
        _localContour = localContour;
        _rule = rule;
    }

    /// <summary>
    /// Реализация Pull-модели. Холст вызывает этот метод, чтобы отрисовать элемент
    /// </summary>
    public Contour GetContour()
    {
        // Если есть правило — вычисляем форму от родителя
        if (_rule != null)
        {
            if (Parent == null)
                throw new InvalidOperationException($"Элемент {Id} имеет правило вывода, но не имеет родителя.");

            return _rule.Resolve(Parent);
        }

        // Если правила нет, возвращаем локальную (нарисованную) форму
        if (_localContour != null)
            return _localContour;

        throw new InvalidOperationException($"Элемент {Id} не имеет ни правила, ни локального контура.");
    }


    protected void AddChildInternal(IWindowNode child)
    {
        if (child.Parent != null)
            throw new InvalidOperationException("Элемент уже имеет родителя.");

        _children.Add(child);
        ((WindowNodeBase)child).Parent = this;
    }

    protected void RemoveChildInternal(IWindowNode child)
    {
        _children.Remove(child);
        ((WindowNodeBase)child).Parent = null;
    }

    /// <summary>
    /// При изменени пользователем обновляем контур и стираем правило
    /// </summary>
    protected void FreezeGeometry()
    {
        if (_rule != null)
        {
            _localContour = GetContour(); // Вычисляем и запоминаем
            _rule = null;                 // Больше не параметрический
        }
    }

    /// <summary>Прямое обновление локальной формы</summary>
    protected void UpdateLocalContour(Contour newContour)
    {
        if (_rule != null)
            FreezeGeometry();

        _localContour = newContour;
    }

    /// <summary>Смена стратегии (например, импост стоял по центру — стал на 30%).</summary>
    protected void SetRule(IDerivationRule newRule)
    {
        _rule = newRule;
        _localContour = null; // Возвращаемся к параметрической модели
    }
}
