using BlueprintMaker.Domain.Models;
using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Application.Commands;

/// <summary>
/// Перемещает узел элемента на новую позицию.
/// oldPosition передаётся в конструкторе (запоминается в начале drag в NodeViewModel.BeginDrag),
/// чтобы Undo корректно вернул элемент к начальной позиции.
/// </summary>
public sealed class MoveNodeCommand : IUndoableCommand
{
    private readonly PhysicalElement _element;
    private readonly int _vertexIndex;
    private readonly Point _oldPosition;
    private readonly Point _newPosition;

    public MoveNodeCommand(PhysicalElement element, int vertexIndex, Point oldPosition, Point newPosition)
    {
        _element = element;
        _vertexIndex = vertexIndex;
        _oldPosition = oldPosition;
        _newPosition = newPosition;
    }

    public void Execute()
    {
        _element.MoveNode(_vertexIndex, _newPosition);
    }

    public void Undo()
    {
        _element.MoveNode(_vertexIndex, _oldPosition);
    }
}