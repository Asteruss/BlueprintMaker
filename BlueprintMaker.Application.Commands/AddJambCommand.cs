using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
namespace BlueprintMaker.Application.Commands;

public sealed class AddJambCommand : IUndoableCommand
{
    private readonly PhysicalElement _wall;
    private readonly int _startSegment;
    private readonly LightSpace? _targetSpace;

    private Jamb? _createdJamb;
    private LightSpace? _remainingSpace;

    public AddJambCommand(PhysicalElement wall, int startSegment, LightSpace? targetSpace = null)
    {
        _wall = wall;
        _startSegment = startSegment;
        _targetSpace = targetSpace;
    }

    public void Execute()
    {
        _createdJamb = _wall.AddJamb(_startSegment, _targetSpace);

        // Если доборник внутренний, он породит остаток. Запомним его для отката
        if (_targetSpace != null && _targetSpace.Children.Count > 0)
        {
            _remainingSpace = _targetSpace.Children[1] as LightSpace;
        }
    }

    public void Undo()
    {
        if (_createdJamb == null) return;

        // Если был внутренним, сносим и добор, и остаток
        if (_targetSpace != null)
        {
            if (_remainingSpace != null) _targetSpace.RemoveChildInternal(_remainingSpace);
            _targetSpace.RemoveChildInternal(_createdJamb);
        }
        else
        {
            // Если внешний — просто удаляем снаружи
            _wall.RemoveChildInternal(_createdJamb);
        }

        _createdJamb = null;
        _remainingSpace = null;
    }
}