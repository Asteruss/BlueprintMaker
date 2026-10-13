using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Materialisation;

public sealed class Board
{
    public double Width { get; }
    public double Length { get; private set; }
    public Contour PhysicalContour { get; private set; }

    public Point StartCenter { get; }
    public Point EndCenter { get; }

    public Joint StartJoint { get; internal set; }
    public Joint EndJoint { get; internal set; }

    public JointEndType StartJointType { get; private set; } = JointEndType.Miter;
    public JointEndType EndJointType { get; private set; } = JointEndType.Miter;

    internal Board(Point startCenter, Point endCenter, double width)
    {
        StartCenter = startCenter;
        EndCenter = endCenter;
        Width = width;
        CalculateGeometry();
    }

    internal void ApplyJointTypes(JointEndType startType, JointEndType endType)
    {
        StartJointType = startType;
        EndJointType = endType;
        CalculateGeometry();
    }

    private void CalculateGeometry()
    {
        // Если Шип - доска отступает на ширину бруса, освобождая место для Паза
        double startOffset = StartJointType == JointEndType.Tenon ? Width : 0;
        double endOffset = EndJointType == JointEndType.Tenon ? Width : 0;

        var dir = new Point(EndCenter.X - StartCenter.X, EndCenter.Y - StartCenter.Y);
        double lenSq = dir.X * dir.X + dir.Y * dir.Y;
        if (lenSq < 0.0001) { PhysicalContour = null; return; }

        double len = System.Math.Sqrt(lenSq);
        var unitDir = new Point(dir.X / len, dir.Y / len);
        var normal = new Point(-dir.Y / len, dir.X / len);

        // Смещаем точки начала и конца для Шипов
        var actualStart = new Point(StartCenter.X + unitDir.X * startOffset, StartCenter.Y + unitDir.Y * startOffset);
        var actualEnd = new Point(EndCenter.X - unitDir.X * endOffset, EndCenter.Y - unitDir.Y * endOffset);

        Length = actualStart.DistanceTo(actualEnd);
        double halfW = Width / 2.0;

        // Строим прямоугольный контур
        var p1 = new Point(actualStart.X + normal.X * halfW, actualStart.Y + normal.Y * halfW);
        var p2 = new Point(actualEnd.X + normal.X * halfW, actualEnd.Y + normal.Y * halfW);
        var p3 = new Point(actualEnd.X - normal.X * halfW, actualEnd.Y - normal.Y * halfW);
        var p4 = new Point(actualStart.X - normal.X * halfW, actualStart.Y - normal.Y * halfW);

        PhysicalContour = Contour.CreateClosed(new List<ContourVertex> { new(p1), new(p2), new(p3), new(p4) });
    }
}