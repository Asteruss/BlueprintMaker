using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Application.Commands;

public sealed class InsertSashCommand : IUndoableCommand
{
    private readonly LightSpace _targetSpace;
    private readonly double _width;
    private Sash? _createdSash;

    public InsertSashCommand(LightSpace targetSpace, double width)
    {
        _targetSpace = targetSpace;
        _width = width;
    }

    public void Execute()
    {
        _createdSash = _targetSpace.InsertSash();
        _createdSash.Materialize(_width);
    }

    public void Undo()
    {
        if (_createdSash != null)
            _targetSpace.RemoveChildInternal(_createdSash);
    }
}