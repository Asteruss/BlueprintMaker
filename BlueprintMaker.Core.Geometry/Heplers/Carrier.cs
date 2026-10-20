namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>Бесконечная кривая-носитель: прямая или окружность</summary>
internal readonly struct Carrier
{
    public readonly bool IsArc;
    public readonly double X, Y;    // точка прямой или центр окружности
    public readonly double Dx, Dy;  // направление прямой
    public readonly double R;

    public Carrier(bool isArc, double x, double y, double dx, double dy, double r)
    {
        IsArc = isArc; X = x; Y = y; Dx = dx; Dy = dy; R = r;
    }
}

internal static class CarrierMath
{
    /// <summary>Носитель сегмента, смещённого влево на d (d < 0 — вправо).</summary>
    public static Carrier? Make(ContourSegment s, double d)
    {
        if (!s.IsArc)
        {
            var t = s.Tangent(s.A);
            return new Carrier(false, s.A.X - t.Y * d, s.A.Y + t.X * d, t.X, t.Y, 0);
        }

        double r = s.R - d * s.Sign;
        if (r < 1e-6) return null;
        return new Carrier(true, s.Center.X, s.Center.Y, 0, 0, r);
    }

    public static List<Point> Intersect(Carrier a, Carrier b)
    {
        var res = new List<Point>(2);

        if (!a.IsArc && !b.IsArc)
        {
            double den = a.Dx * b.Dy - a.Dy * b.Dx;
            if (Math.Abs(den) < 1e-12) return res;
            double t = ((b.X - a.X) * b.Dy - (b.Y - a.Y) * b.Dx) / den;
            res.Add(new Point(a.X + a.Dx * t, a.Y + a.Dy * t));
            return res;
        }

        if (a.IsArc && b.IsArc)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double dd = Math.Sqrt(dx * dx + dy * dy);
            if (dd < 1e-9) return res;
            if (dd > a.R + b.R + 1e-7 || dd < Math.Abs(a.R - b.R) - 1e-7) return res;

            double l = (a.R * a.R - b.R * b.R + dd * dd) / (2 * dd);
            double h2 = a.R * a.R - l * l;
            if (h2 < 0) h2 = 0;
            double h = Math.Sqrt(h2);
            double mx = a.X + dx * l / dd, my = a.Y + dy * l / dd;

            res.Add(new Point(mx - dy * h / dd, my + dx * h / dd));
            if (h > 1e-9) res.Add(new Point(mx + dy * h / dd, my - dx * h / dd));
            return res;
        }

        var line = a.IsArc ? b : a;
        var circ = a.IsArc ? a : b;

        double fx = line.X - circ.X, fy = line.Y - circ.Y;
        double bb = fx * line.Dx + fy * line.Dy;
        double cc = fx * fx + fy * fy - circ.R * circ.R;
        double disc = bb * bb - cc;
        double tol = 1e-9 * Math.Max(1.0, circ.R * circ.R);
        if (disc < -tol) return res;
        if (disc < 0) disc = 0;
        double sq = Math.Sqrt(disc);

        res.Add(new Point(line.X + line.Dx * (-bb - sq), line.Y + line.Dy * (-bb - sq)));
        if (sq > 1e-9)
            res.Add(new Point(line.X + line.Dx * (-bb + sq), line.Y + line.Dy * (-bb + sq)));
        return res;
    }
}