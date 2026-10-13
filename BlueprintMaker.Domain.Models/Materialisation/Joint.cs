namespace BlueprintMaker.Domain.Models.Materialisation;

public sealed class Joint
{
    public Board BoardA { get; } // Доска, которая заканчивается в этом стыке
    public Board BoardB { get; } // Доска, которая начинается в этом стыке

    public JointEndType TypeA { get; private set; } = JointEndType.Miter; // Край доски A
    public JointEndType TypeB { get; private set; } = JointEndType.Miter; // Край доски B

    internal Joint(Board boardA, Board boardB)
    {
        BoardA = boardA;
        BoardB = boardB;
    }

    public void SetTypeA(JointEndType type)
    {
        TypeA = type;
        EnforcePairAndApply();
    }

    public void SetTypeB(JointEndType type)
    {
        TypeB = type;
        EnforcePairAndApply();
    }

    private void EnforcePairAndApply()
    {
        // Синхронизируем вторую сторону
        if (TypeA == JointEndType.Tenon) TypeB = JointEndType.Mortise;
        else if (TypeA == JointEndType.Mortise) TypeB = JointEndType.Tenon;
        else if (TypeA == JointEndType.Miter) TypeB = JointEndType.Miter;

        // Уведомляем доски о смене их краев
        BoardA.ApplyJointTypes(
            BoardA.StartJoint == this ? TypeA : BoardA.StartJointType,
            BoardA.EndJoint == this ? TypeA : BoardA.EndJointType);

        BoardB.ApplyJointTypes(
            BoardB.StartJoint == this ? TypeB : BoardB.StartJointType,
            BoardB.EndJoint == this ? TypeB : BoardB.EndJointType);
    }
}