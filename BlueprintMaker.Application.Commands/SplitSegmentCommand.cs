using BlueprintMaker.Domain.Models;
using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Application.Commands;

public sealed class SplitSegmentCommand : IUndoableCommand
{
    private readonly PhysicalElement _element;
    private readonly int _segmentIndex;
    private readonly Point? _position;
    private int _createdNodeIndex = -1;

    public SplitSegmentCommand(PhysicalElement element, int segmentIndex, Point? position = null)
    {
        _element = element;
        _segmentIndex = segmentIndex;
        _position = position;
    }

    public void Execute()
    {
        _createdNodeIndex = _segmentIndex + 1;
        _element.SplitAt(_segmentIndex, _position);
    }

    public void Undo()
    {
        if (_createdNodeIndex != -1)
            _element.RemoveNode(_createdNodeIndex);
    }
}