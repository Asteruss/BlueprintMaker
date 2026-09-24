using BlueprintMaker.Domain.Models;
using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Application.Commands;

public sealed class MoveArcControlPointCommand : IUndoableCommand
{
    private readonly PhysicalElement _element;
    private readonly int _segmentIndex;
    private readonly Point _newControlPos;
    private double _oldBulge;

    public MoveArcControlPointCommand(PhysicalElement element, int segmentIndex, Point newControlPos)
    {
        _element = element;
        _segmentIndex = segmentIndex;
        _newControlPos = newControlPos;
    }

    public void Execute()
    {
        _oldBulge = _element.GetContour().Vertices[_segmentIndex].Bulge;
        _element.SetSegmentArcByControlPoint(_segmentIndex, _newControlPos);
    }

    public void Undo()
    {
        _element.SetSegmentArc(_segmentIndex, _oldBulge);
    }
}