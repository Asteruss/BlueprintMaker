using BlueprintMaker.Domain.Models;

namespace BlueprintMaker.Application.Commands;

/// <summary>
/// Превращает сегмент в дугу (или обратно в прямую).
/// </summary>
public sealed class SetSegmentArcCommand : IUndoableCommand
{
    private readonly PhysicalElement _element;
    private readonly int _segmentIndex;
    private readonly double _newBulge;
    private double _oldBulge;

    public SetSegmentArcCommand(PhysicalElement element, int segmentIndex, double bulge)
    {
        _element = element;
        _segmentIndex = segmentIndex;
        _newBulge = bulge;
    }

    public void Execute()
    {
        // Запоминаем старую кривизну (0 = прямая)
        _oldBulge = _element.GetContour().Vertices[_segmentIndex].Bulge;
        _element.SetSegmentArc(_segmentIndex, _newBulge);
    }

    public void Undo()
    {
        // Возвращаем старую кривизну
        _element.SetSegmentArc(_segmentIndex, _oldBulge);
    }
}