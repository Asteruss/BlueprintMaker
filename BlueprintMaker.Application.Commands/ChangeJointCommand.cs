using BlueprintMaker.Domain.Models.Materialisation;

namespace BlueprintMaker.Application.Commands;

/// <summary>
/// Меняет тип стыка (Miter/Tenon/Mortise) для одного Joint
/// forA = true — меняется TypeA (доска, заканчивающая стык)
/// forA = false — меняется TypeB (доска, начинающая стык)
/// Undo восстанавливает прежние TypeA/TypeB через SetTypeA/SetTypeB
/// </summary>
public sealed class ChangeJointCommand : IUndoableCommand
{
    private readonly Joint _joint;
    private readonly JointEndType _newType;
    private readonly bool _forA;
    private JointEndType _oldTypeA;
    private JointEndType _oldTypeB;

    public ChangeJointCommand(Joint joint, JointEndType newType, bool forA)
    {
        _joint = joint;
        _newType = newType;
        _forA = forA;
    }

    public void Execute()
    {
        _oldTypeA = _joint.TypeA;
        _oldTypeB = _joint.TypeB;
        if (_forA)
            _joint.SetTypeA(_newType);
        else
            _joint.SetTypeB(_newType);
    }

    public void Undo()
    {
        if (_forA)
            _joint.SetTypeA(_oldTypeA);
        else
            _joint.SetTypeB(_oldTypeB);
    }
}