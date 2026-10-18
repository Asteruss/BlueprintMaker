using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Materialisation;

public sealed class FrameAssembly
{
    private readonly List<Board> _boards = new();
    private readonly List<Joint> _joints = new();

    public IReadOnlyList<Board> Boards => _boards;
    public IReadOnlyList<Joint> Joints => _joints;
    public double Width { get; private set; }

    public void Rebuild(Contour outerContour, Contour innerContour, double width)
    {
        _boards.Clear();
        _joints.Clear();
        Width = width;

        int n = outerContour.Vertices.Count;
        if (n < 3 || innerContour.Vertices.Count != n) return;

        for (int i = 0; i < n; i++)
        {
            var b = new Board(width)
            {
                StartOuter = outerContour.Vertices[i].Position,
                EndOuter = outerContour.Vertices[(i + 1) % n].Position,
                StartInner = innerContour.Vertices[i].Position,
                EndInner = innerContour.Vertices[(i + 1) % n].Position,

                // Привязываем колбек пересчета к этому классу
                RecalculateGeometryCallback = CalculateBoardGeometry
            };

            _boards.Add(b);
        }

        for (int i = 0; i < n; i++)
        {
            var joint = new Joint(_boards[i], _boards[(i + 1) % n]);
            _joints.Add(joint);
            _boards[i].EndJoint = joint;
            _boards[(i + 1) % n].StartJoint = joint;
        }

        foreach (var b in _boards) CalculateBoardGeometry(b);
    }

    public void ChangeJoint(int jointIndex, JointEndType newTypeForA)
    {
        if (jointIndex >= 0 && jointIndex < _joints.Count)
        {
            _joints[jointIndex].SetTypeA(newTypeForA);
        }
    }

    private void CalculateBoardGeometry(Board b)
    {
        var dx = b.EndOuter.X - b.StartOuter.X;
        var dy = b.EndOuter.Y - b.StartOuter.Y;
        if (dx * dx + dy * dy < 0.0001) { b.PhysicalContour = null; return; }

        // Сосед на начале — предыдущая доска, на конце — следующая
        var prev = b.StartJoint?.BoardA;
        var next = b.EndJoint?.BoardB;

        // Начало: идём от начала доски
        var (startOut, startIn) = ResolveEnd(b, prev, b.StartJointType, atStart: true);
        // Конец
        var (endOut, endIn) = ResolveEnd(b, next, b.EndJointType, atStart: false);

        b.Length = System.Math.Max(startOut.DistanceTo(endOut), startIn.DistanceTo(endIn));

        b.PhysicalContour = Contour.CreateClosed(new List<ContourVertex>
    {
        new(startOut),
        new(endOut),
        new(endIn),
        new(startIn),
    });
    }

    /// Возвращает (внешняя, внутренняя) точки торца доски у одного стыка.
    private static (Point Outer, Point Inner) ResolveEnd(Board b, Board? neighbor, JointEndType type, bool atStart)
    {
        var outer = atStart ? b.StartOuter : b.EndOuter;
        var inner = atStart ? b.StartInner : b.EndInner;

        if (type == JointEndType.Miter || neighbor == null)
            return (outer, inner);

        if (type == JointEndType.Mortise)
        {
            // Сквозная доска: внешняя точка на месте,
            // внутренняя — пересечение моей внутренней кромки с внешней кромкой соседа
            var p = Intersect(b.StartInner, b.EndInner, neighbor.StartOuter, neighbor.EndOuter);
            return (outer, p ?? inner);
        }

        // Tenon: доска упирается. Внутренняя точка на месте,
        // внешняя — пересечение моей внешней кромки с внутренней кромкой соседа
        var q = Intersect(b.StartOuter, b.EndOuter, neighbor.StartInner, neighbor.EndInner);
        return (q ?? outer, inner);
    }

    private static Point? Intersect(Point a1, Point a2, Point b1, Point b2)
    {
        double d1x = a2.X - a1.X, d1y = a2.Y - a1.Y;
        double d2x = b2.X - b1.X, d2y = b2.Y - b1.Y;

        double denom = d1x * d2y - d1y * d2x;
        if (System.Math.Abs(denom) < 1e-9) return null;

        double t = ((b1.X - a1.X) * d2y - (b1.Y - a1.Y) * d2x) / denom;
        return new Point(a1.X + d1x * t, a1.Y + d1y * t);
    }
}