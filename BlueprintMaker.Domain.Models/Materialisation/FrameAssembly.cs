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
        int n = outerContour.Vertices.Count;
        if (!ContourRules.IsValidClosed(outerContour.Vertices) || innerContour.Vertices.Count != n) return;

        Width = width;

        // Количество досок не изменилось — обновляем геометрию, типы стыков сохраняются
        if (_boards.Count == n)
        {
            for (int i = 0; i < n; i++)
                ApplySource(_boards[i], outerContour, innerContour, i, n);

            foreach (var b in _boards)
                CalculateBoardGeometry(b);
            return;
        }

        _boards.Clear();
        _joints.Clear();

        for (int i = 0; i < n; i++)
        {
            var b = new Board(width) { RecalculateGeometryCallback = CalculateBoardGeometry };
            ApplySource(b, outerContour, innerContour, i, n);
            _boards.Add(b);
        }

        for (int i = 0; i < n; i++)
        {
            var joint = new Joint(_boards[i], _boards[(i + 1) % n]);
            _joints.Add(joint);
            _boards[i].EndJoint = joint;
            _boards[(i + 1) % n].StartJoint = joint;
        }

        foreach (var b in _boards)
            CalculateBoardGeometry(b);
    }

    public void ChangeJoint(int jointIndex, JointEndType newTypeForA)
    {
        if (jointIndex >= 0 && jointIndex < _joints.Count)
            _joints[jointIndex].SetTypeA(newTypeForA);
    }

    private static void ApplySource(Board b, Contour outer, Contour inner, int i, int n)
    {
        b.StartOuter = outer.Vertices[i].Position;
        b.EndOuter = outer.Vertices[(i + 1) % n].Position;
        b.StartInner = inner.Vertices[i].Position;
        b.EndInner = inner.Vertices[(i + 1) % n].Position;
        b.OuterBulge = outer.Vertices[i].Bulge;
        b.InnerBulge = inner.Vertices[i].Bulge;
    }

    private void CalculateBoardGeometry(Board b)
    {
        if (b.StartOuter.DistanceTo(b.EndOuter) < 0.01)
        {
            b.PhysicalContour = null;
            return;
        }

        var prev = b.StartJoint?.BoardA;
        var next = b.EndJoint?.BoardB;

        var (startOut, startIn) = ResolveEnd(b, prev, b.StartJointType, atStart: true);
        var (endOut, endIn) = ResolveEnd(b, next, b.EndJointType, atStart: false);

        // Дуги кромок между новыми концами (укорочение/удлинение стыками учтено)
        double outerBulge = ContourMath.SubBulge(b.StartOuter, b.EndOuter, b.OuterBulge, startOut, endOut);
        double innerBulge = ContourMath.SubBulge(b.StartInner, b.EndInner, b.InnerBulge, startIn, endIn);

        b.Length = System.Math.Max(
            EdgeLength(startOut, endOut, outerBulge),
            EdgeLength(startIn, endIn, innerBulge));

        // Обход: внешняя кромка вперёд, торец, внутренняя кромка назад (знак Bulge меняется), торец
        b.PhysicalContour = Contour.CreateClosed(new List<ContourVertex>
        {
            new(startOut, outerBulge),
            new(endOut, 0),
            new(endIn, -innerBulge),
            new(startIn, 0),
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
            var p = ContourMath.IntersectCurves(
                b.StartInner, b.EndInner, b.InnerBulge,
                neighbor.StartOuter, neighbor.EndOuter, neighbor.OuterBulge,
                inner);
            return (outer, p ?? inner);
        }

        // Tenon: доска упирается. Внутренняя точка на месте,
        // внешняя — пересечение моей внешней кромки с внутренней кромкой соседа
        var q = ContourMath.IntersectCurves(
            b.StartOuter, b.EndOuter, b.OuterBulge,
            neighbor.StartInner, neighbor.EndInner, neighbor.InnerBulge,
            outer);
        return (q ?? outer, inner);
    }

    /// Длина кромки: хорда или длина дуги
    private static double EdgeLength(Point a, Point b, double bulge)
    {
        double chord = a.DistanceTo(b);
        if (System.Math.Abs(bulge) < 1e-4) return chord;

        double theta = 4 * System.Math.Atan(System.Math.Abs(bulge));
        return chord * theta / (2 * System.Math.Sin(theta / 2));
    }
}