using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Materialisation;

public sealed class Board
{
    public double Width { get; }
    public double Length { get; internal set; }
    public Contour PhysicalContour { get; internal set; }

    public Joint StartJoint { get; internal set; }
    public Joint EndJoint { get; internal set; }

    public JointEndType StartJointType { get; internal set; } = JointEndType.Miter;
    public JointEndType EndJointType { get; internal set; } = JointEndType.Miter;

    // Исходные точки рамы (внешние и внутренние углы)
    public Point StartOuter { get; set; }
    public Point EndOuter { get; set; }
    public Point StartInner { get; set; }
    public Point EndInner { get; set; }

    // Bulge кромок доски от начала к концу (0 — прямая)
    public double OuterBulge { get; set; }
    public double InnerBulge { get; set; }
    // Колбек, который вызывает FrameAssembly для пересчета геометрии

    public bool IsArc => System.Math.Abs(OuterBulge) > 1e-4 || System.Math.Abs(InnerBulge) > 1e-4;
    internal Action<Board>? RecalculateGeometryCallback { get; set; }

    internal Board(double width)
    {
        Width = width;
    }

    // Вызывается из Joint, когда меняется стык
    internal void ApplyJointTypes(JointEndType startType, JointEndType endType)
    {
        StartJointType = startType;
        EndJointType = endType;
        RecalculateGeometryCallback?.Invoke(this);
    }
}