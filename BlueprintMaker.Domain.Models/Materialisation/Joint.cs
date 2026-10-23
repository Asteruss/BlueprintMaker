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

    public void SetTypeA(JointEndType type) => Set(forA: true, type, new HashSet<Joint>());

    public void SetTypeB(JointEndType type) => Set(forA: false, type, new HashSet<Joint>());

    private static JointEndType Opposite(JointEndType type) => type switch
    {
        JointEndType.Tenon => JointEndType.Mortise,
        JointEndType.Mortise => JointEndType.Tenon,
        _ => JointEndType.Miter
    };

    private void Set(bool forA, JointEndType type, HashSet<Joint> visited)
    {
        if (!visited.Add(this)) return;

        TypeA = forA ? type : Opposite(type);
        TypeB = forA ? Opposite(type) : type;
        ApplyToBoards();

        PropagateArc(BoardA, TypeA, visited);
        PropagateArc(BoardB, TypeB, visited);
    }

    private void PropagateArc(Board board, JointEndType type, HashSet<Joint> visited)
    {
        if (!board.IsArc) return;

        var other = board.StartJoint == this ? board.EndJoint : board.StartJoint;
        if (other == null || other == this) return;

        other.Set(forA: other.BoardA == board, type, visited);
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