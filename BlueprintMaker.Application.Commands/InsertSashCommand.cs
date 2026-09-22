using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Application.Commands;

public sealed class InsertSashCommand : IUndoableCommand
{
    private readonly LightSpace _targetSpace;
    private Sash? _createdSash;

    public InsertSashCommand(LightSpace targetSpace)
    {
        _targetSpace = targetSpace;
    }

    public void Execute()
    {
        _createdSash = _targetSpace.InsertSash();
    }

    public void Undo()
    {
        if (_createdSash != null)
            _targetSpace.RemoveChildInternal(_createdSash);
    }
}