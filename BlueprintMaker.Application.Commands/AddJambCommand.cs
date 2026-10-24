using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Application.Commands;

public sealed class AddJambCommand : IUndoableCommand
{
    private readonly PhysicalElement _wall;
    private readonly int _segment;
    private readonly LightSpace? _targetSpace;
    private readonly double _width;

    private Jamb? _jamb;
    private LightSpace? _remainder;
    private MaterializeCommand? _materialize;

    public AddJambCommand(PhysicalElement wall, int segment, LightSpace? targetSpace, double width)
    {
        _wall = wall;
        _segment = segment;
        _targetSpace = targetSpace;
        _width = width;
    }

    public void Execute()
    {
        var result = _wall.AddJamb(_segment, _targetSpace);
        _jamb = result.Jamb;
        _remainder = result.Remainder;

        _materialize = new MaterializeCommand(_jamb, _width);
        _materialize.Execute();
    }

    public void Undo()
    {
        if (_jamb == null) return;

        _materialize?.Undo();
        _materialize = null;

        if (_targetSpace != null)
        {
            if (_remainder != null) _targetSpace.RemoveChildInternal(_remainder);
            _targetSpace.RemoveChildInternal(_jamb);
        }
        else
        {
            _wall.RemoveChildInternal(_jamb);
        }

        _jamb = null;
        _remainder = null;
    }
}