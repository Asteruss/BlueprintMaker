using BlueprintMaker.Core.Geometry.Heplers;

namespace BlueprintMaker.Core.Geometry;

/// <summary>
/// Помогает определить, попал ли клик мышки в конкретный сегмент (прямую или дугу).
/// </summary>
public static class SegmentProjection
{
    /// <summary>
    /// Проецирует точку на сегмент (прямой или дуговой) и возвращает полный результат.
    /// </summary>
    public static SegmentProjectionResult Project(Point from, Point to, double bulge, Point test)
    {
        if (Math.Abs(bulge) < 0.0001)
            return ProjectOntoLine(from, to, test);

        var arc = BulgeMath.GetArcParameters(from, to, bulge);
        return ProjectOntoArc(arc, from, to, test);
    }

    private static SegmentProjectionResult ProjectOntoLine(Point from, Point to, Point test)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double lengthSq = dx * dx + dy * dy;

        double t = lengthSq < 0.0001
            ? 0
            : ((test.X - from.X) * dx + (test.Y - from.Y) * dy) / lengthSq;

        t = MathCompat.Clamp(t, 0, 1);

        var closest = new Point(from.X + dx * t, from.Y + dy * t);
        return new SegmentProjectionResult(closest, t, closest.DistanceTo(test));
    }

    private static SegmentProjectionResult ProjectOntoArc(ArcParameters arc, Point from, Point to, Point test)
    {
        double testAngle = Math.Atan2(test.Y - arc.Center.Y, test.X - arc.Center.X);
        double relativeAngle = NormalizeAngle(testAngle - arc.StartAngle);
        double sweep = arc.SweepAngle;

        bool withinSweep = sweep >= 0
            ? (relativeAngle >= 0 && relativeAngle <= sweep)
            : (relativeAngle <= 0 && relativeAngle >= sweep);

        if (withinSweep)
        {
            var closest = new Point(
                arc.Center.X + arc.Radius * Math.Cos(testAngle),
                arc.Center.Y + arc.Radius * Math.Sin(testAngle));

            double t = sweep == 0 ? 0 : relativeAngle / sweep;
            return new SegmentProjectionResult(closest, t, closest.DistanceTo(test));
        }

        // Точка проекции вне углового диапазона дуги — ближайшая точка одна из концевых.
        double distToStart = from.DistanceTo(test);
        double distToEnd = to.DistanceTo(test);

        return distToStart <= distToEnd
            ? new SegmentProjectionResult(from, 0, distToStart)
            : new SegmentProjectionResult(to, 1, distToEnd);
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle <= -Math.PI) angle += 2 * Math.PI;
        while (angle > Math.PI) angle -= 2 * Math.PI;
        return angle;
    }

    // Для обратной совместимости
    public static double DistanceToSegment(Point from, Point to, double bulge, Point test)
    {
        return Project(from, to, bulge, test).Distance;
    }
}