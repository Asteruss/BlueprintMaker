namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>Операции над сегментами: построение, смещение стыков, пересечения.</summary>
internal static class SegmentMath
{
    public static ContourSegment[] BuildSegs(IReadOnlyList<ContourVertex> v, bool closed)
    {
        int n = v.Count;
        int count = closed ? n : n - 1;
        if (count < 0) count = 0;

        var segs = new ContourSegment[count];
        for (int i = 0; i < count; i++)
            segs[i] = new ContourSegment(v[i].Position, v[(i + 1) % n].Position, v[i].Bulge);
        return segs;
    }

    public static Point Shift(Point p, Point dir, double k) =>
        new Point(p.X + dir.X * k, p.Y + dir.Y * k);

    /// <summary>
    /// Bulge дуги сегмента s между точками from и to (та же окружность, то же направление)
    /// Для прямых — 0
    /// </summary>
    public static double SubBulge(ContourSegment s, Point from, Point to, double maxSweep)
    {
        if (!s.IsArc) return 0;
        if (from.DistanceTo(to) < GeometryConstants.Tolerance) return 0;

        double a1 = Math.Atan2(from.Y - s.Center.Y, from.X - s.Center.X);
        double a2 = Math.Atan2(to.Y - s.Center.Y, to.X - s.Center.X);
        double d = (a2 - a1) * s.Sign;
        d %= GeometryConstants.TwoPi;
        if (d < 0) d += GeometryConstants.TwoPi;
        if (d > maxSweep) d = 0;   // «вывернутая» дуга — считаем вырожденной
        return Math.Tan(d * s.Sign / 4.0);
    }

    /// <summary>Пересечения двух ограниченных сегментов: (параметр на a, параметр на b, точка).</summary>
    public static List<(double ta, double tb, Point p)> IntersectSegs(ContourSegment a, ContourSegment b)
    {
        var res = new List<(double, double, Point)>();
        var ca = CarrierMath.Make(a, 0);
        var cb = CarrierMath.Make(b, 0);
        if (!ca.HasValue || !cb.HasValue) return res;

        const double tol = 1e-9;
        foreach (var p in CarrierMath.Intersect(ca.Value, cb.Value))
        {
            double ta = a.Param(p), tb = b.Param(p);
            if (ta < -tol || ta > 1 + tol || tb < -tol || tb > 1 + tol) continue;
            res.Add((Math.Min(1, Math.Max(0, ta)), Math.Min(1, Math.Max(0, tb)), p));
        }
        return res;
    }

    /// <summary>
    /// Новая вершина стыка двух сегментов, смещённых влево на d:
    /// пересечение смещённых кривых (прямая/дуга), для гладкого стыка — сдвиг по нормали
    /// </summary>
    public static Point OffsetJoint(ContourSegment s0, ContourSegment s1, double d)
    {
        var v = s1.A;
        var t0 = s0.Tangent(s0.B);
        var t1 = s1.Tangent(s1.A);
        var n0 = new Point(-t0.Y, t0.X);
        var n1 = new Point(-t1.Y, t1.X);

        double cross = t0.X * t1.Y - t0.Y * t1.X;
        double dot = t0.X * t1.X + t0.Y * t1.Y;

        // Гладкий стык: точное пересечение неустойчиво, а ответ — сдвиг по нормали
        if (Math.Abs(cross) < 1e-4 && dot > 0)
            return Shift(v, n1, d);

        double ax = n0.X + n1.X, ay = n0.Y + n1.Y;
        double al = Math.Sqrt(ax * ax + ay * ay);
        var approx = al > 1e-9
            ? new Point(v.X + ax / al * d, v.Y + ay / al * d)
            : Shift(v, n1, d);

        var c0 = CarrierMath.Make(s0, d);
        var c1 = CarrierMath.Make(s1, d);
        if (c0.HasValue && c1.HasValue)
        {
            Point? best = null;
            double bd = double.MaxValue;
            foreach (var p in CarrierMath.Intersect(c0.Value, c1.Value))
            {
                double dist = p.DistanceTo(approx);
                if (dist < bd) { bd = dist; best = p; }
            }
            if (best.HasValue) return best.Value;
        }
        return Shift(v, n1, d);
    }
}