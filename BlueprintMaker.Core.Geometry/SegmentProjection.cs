namespace BlueprintMaker.Core.Geometry;

/// <summary>
/// Помогает определить, попал ли клик мышки в конкретный сегмент (прямую или дугу).
/// </summary>
public static class SegmentProjection
{
    public static double DistanceToSegment(Point from, Point to, double bulge, Point test)
    {
        if (Math.Abs(bulge) < 0.0001)
            return DistanceToLine(from, to, test);

        var arc = BulgeMath.GetArcParameters(from, to, bulge);
        double testAngle = Math.Atan2(test.Y - arc.Center.Y, test.X - arc.Center.X);

        // Упрощенный расчет: ищем угол дуги, ближайший к точке клика.
        // Для хит-теста этого достаточно.
        double relativeAngle = testAngle - arc.StartAngle;
        while (relativeAngle < 0) relativeAngle += 2 * Math.PI;
        while (relativeAngle > 2 * Math.PI) relativeAngle -= 2 * Math.PI;

        bool withinSweep = arc.SweepAngle > 0
            ? relativeAngle >= 0 && relativeAngle <= arc.SweepAngle
            : relativeAngle <= 0 && relativeAngle >= arc.SweepAngle;

        if (withinSweep)
        {
            var closest = new Point(
                arc.Center.X + arc.Radius * Math.Cos(testAngle),
                arc.Center.Y + arc.Radius * Math.Sin(testAngle));
            return closest.DistanceTo(test);
        }

        return Math.Min(from.DistanceTo(test), to.DistanceTo(test));
    }

    private static double DistanceToLine(Point from, Point to, Point test)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double lengthSq = dx * dx + dy * dy;

        if (lengthSq < 0.0001) return from.DistanceTo(test);

        double t = ((test.X - from.X) * dx + (test.Y - from.Y) * dy) / lengthSq;
        t = Math.Clamp(t, 0, 1);

        var closest = new Point(from.X + dx * t, from.Y + dy * t);
        return closest.DistanceTo(test);
    }
}