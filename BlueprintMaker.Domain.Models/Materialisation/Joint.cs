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
        TypeB = type switch
        {
            JointEndType.Tenon => JointEndType.Mortise,
            JointEndType.Mortise => JointEndType.Tenon,
            _ => JointEndType.Miter
        };
        ApplyToBoards();
    }

    public void SetTypeB(JointEndType type)
    {
        TypeB = type;
        TypeA = type switch
        {
            JointEndType.Tenon => JointEndType.Mortise,
            JointEndType.Mortise => JointEndType.Tenon,
            _ => JointEndType.Miter
        };
        ApplyToBoards();
    }

    private void ApplyToBoards()
    {
        BoardA.ApplyJointTypes(
            BoardA.StartJoint == this ? TypeA : BoardA.StartJointType,
            BoardA.EndJoint == this ? TypeA : BoardA.EndJointType);

        BoardB.ApplyJointTypes(
            BoardB.StartJoint == this ? TypeB : BoardB.StartJointType,
            BoardB.EndJoint == this ? TypeB : BoardB.EndJointType);
    }
}