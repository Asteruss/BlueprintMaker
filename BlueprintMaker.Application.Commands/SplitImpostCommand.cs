using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Application.Commands;

public sealed class SplitImpostCommand : IUndoableCommand
{
    private readonly Impost _impost;
    private readonly int _segmentIndex;
    private readonly Point? _position;
    private int _createdNodeIndex = -1;

    public SplitImpostCommand(Impost impost, int segmentIndex, Point? position = null)
    {
        _impost = impost;
        _segmentIndex = segmentIndex;
        _position = position;
    }

    public void Execute()
    {
        _impost.SplitAt(_segmentIndex, _position);
        _createdNodeIndex = _segmentIndex + 1;
    }

    public void Undo()
    {
        if (_createdNodeIndex != -1)
            _impost.RemoveNode(_createdNodeIndex);
        
    }
}