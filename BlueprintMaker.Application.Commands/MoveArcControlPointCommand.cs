using BlueprintMaker.Domain.Models;
using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Application.Commands;

/// <summary>
/// Меняет Bulge сегмента через контрольную точку дуги.
/// oldBulge передаётся в конструкторе (запоминается в начале drag в NodeViewModel.BeginDrag),
/// чтобы Undo корректно вернул сегмент к начальной кривизне.
/// </summary>
public sealed class MoveArcControlPointCommand : IUndoableCommand
{
    private readonly PhysicalElement _element;
    private readonly int _segmentIndex;
    private readonly double _oldBulge;
    private readonly Point _newControlPos;

    public MoveArcControlPointCommand(PhysicalElement element, int segmentIndex, double oldBulge, Point newControlPos)
    {
        _element = element;
        _segmentIndex = segmentIndex;
        _oldBulge = oldBulge;
        _newControlPos = newControlPos;
    }

    public void Execute()
    {
        _element.SetSegmentArcByControlPoint(_segmentIndex, _newControlPos);
    }

    public void Undo()
    {
        _element.SetSegmentArc(_segmentIndex, _oldBulge);
    }
}