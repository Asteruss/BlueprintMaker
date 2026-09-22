using BlueprintMaker.Domain.Models;
using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Application.Commands;

public sealed class MoveNodeCommand : IUndoableCommand
{
    private readonly PhysicalElement _element;
    private readonly int _vertexIndex;
    private readonly Point _newPosition;
    private Point _oldPosition;

    public MoveNodeCommand(PhysicalElement element, int vertexIndex, Point newPosition)
    {
        _element = element;
        _vertexIndex = vertexIndex;
        _newPosition = newPosition;
    }

    public void Execute()
    {
        _oldPosition = _element.GetContour().Vertices[_vertexIndex].Position;
        _element.MoveNode(_vertexIndex, _newPosition);
    }

    public void Undo()
    {
        _element.MoveNode(_vertexIndex, _oldPosition);
    }
}