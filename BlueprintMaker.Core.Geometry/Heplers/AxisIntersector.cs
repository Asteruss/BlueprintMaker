namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>Пересечение контура с вертикальной или горизонтальной линией.</summary>
internal static class AxisIntersector
{
    public static IReadOnlyList<Point> Intersect(
        IReadOnlyList<ContourVertex> vertices, bool isClosed, AxisOrientation orientation, double coordinate)
    {
        var results = new List<Point>();
        int n = vertices.Count;
        int limit = isClosed ? n : n - 1;

        for (int i = 0; i < limit; i++)
        {
            var current = vertices[i];
            var next = isClosed && i == n - 1 ? vertices[0] : vertices[i + 1];

            if (current.Position.DistanceTo(next.Position) < 1e-9) continue;   // нулевой сегмент

            var seg = new ContourSegment(current.Position, next.Position, current.Bulge);
            if (!seg.IsArc)
            {
                var p = IntersectStraight(seg.A, seg.B, orientation, coordinate);
                if (p.HasValue) results.Add(p.Value);
            }
            else
            {
                results.AddRange(IntersectArc(seg, orientation, coordinate));
            }
        }

        var unique = new List<Point>();
        foreach (var p in results)
        {
            bool duplicate = false;
            foreach (var f in unique)
            {
                if (p.DistanceTo(f) < 0.001) { duplicate = true; break; }
            }
            if (!duplicate) unique.Add(p);
        }
        return unique;
    }

    private static Point? IntersectStraight(Point from, Point to, AxisOrientation orientation, double coordinate)
    {
        double a = orientation == AxisOrientation.Vertical ? from.X : from.Y;
        double b = orientation == AxisOrientation.Vertical ? to.X : to.Y;

        if (Math.Abs(a - b) < 0.0001) return null;

        double t = (coordinate - a) / (b - a);
        if (t < 0 || t > 1) return null;

        return orientation == AxisOrientation.Vertical
            ? new Point(coordinate, from.Y + (to.Y - from.Y) * t)
            : new Point(from.X + (to.X - from.X) * t, coordinate);
    }

    private static IEnumerable<Point> IntersectArc(ContourSegment arc, AxisOrientation orientation, double coordinate)
    {
        double fixedDelta = orientation == AxisOrientation.Vertical
            ? coordinate - arc.Center.X
            : coordinate - arc.Center.Y;

        double underSqrt = arc.R * arc.R - fixedDelta * fixedDelta;
        if (underSqrt < 0) yield break;

        double offset = Math.Sqrt(underSqrt);

        for (int sign = -1; sign <= 1; sign += 2)
        {
            var candidate = orientation == AxisOrientation.Vertical
                ? new Point(coordinate, arc.Center.Y + sign * offset)
                : new Point(arc.Center.X + sign * offset, coordinate);

            double angle = Math.Atan2(candidate.Y - arc.Center.Y, candidate.X - arc.Center.X);

            if (ContourSegment.AngleInSweep(arc.StartAngle, arc.Sweep, angle))
                yield return candidate;
        }
    }
}