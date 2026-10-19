using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Application.Commands;

public sealed class SplitImpostCommand : IUndoableCommand
{
    private readonly Impost _impost;
    private readonly Point? _position;
    private int _createdNodeIndex = -1;

    public SplitImpostCommand(Impost impost, Point? position = null)
    {
        _impost = impost;
        _position = position;
    }

    public void Execute()
    {
        _impost.SplitAt(0, _position);
        _createdNodeIndex = 1;
    }

    public void Undo()
    {
        if (_createdNodeIndex != -1)
            _impost.RemoveNode(_createdNodeIndex);
        
    }
}