using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Application.Commands;

/// <summary>
/// Вставляет N импостов в LightSpace, деля его на N+1 равных частей
/// Undo: удаляет все созданные children и восстанавливает прежние
/// </summary>
public sealed class InsertMultipleImpostsCommand : IUndoableCommand
{
    private readonly LightSpace _targetSpace;
    private readonly int _count;
    private readonly AxisOrientation _orientation;
    private List<IWindowNode> _oldChildren = new();

    public InsertMultipleImpostsCommand(LightSpace targetSpace, int count, AxisOrientation orientation)
    {
        _targetSpace = targetSpace;
        _count = count;
        _orientation = orientation;
    }

    public void Execute()
    {
        _oldChildren = _targetSpace.Children.ToList();
        _targetSpace.InsertMultipleImposts(_count, _orientation);
    }

    public void Undo()
    {
        // Удаляем все созданные children
        foreach (var child in _targetSpace.Children.ToList())
            _targetSpace.RemoveChildInternal(child);

        // Восстанавливаем старые children
        foreach (var child in _oldChildren)
            _targetSpace.AddChildInternal(child);
    }
}