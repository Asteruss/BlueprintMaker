namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>
/// Сегмент контура: прямая или дуга. Параметры дуги считаются из Bulge самостоятельно
/// (Bulge > 0 — против часовой, Bulge <; 0 — по часовой)
/// </summary>
internal readonly struct ContourSegment
{
    public readonly Point A;
    public readonly Point B;
    public readonly Point Center;
    public readonly double R;
    public readonly double StartAngle;
    public readonly double Sweep;   // знаковый угол дуги
    public readonly double Bulge;
    public readonly bool IsArc;

    public ContourSegment(Point a, Point b, double bulge)
    {
        A = a;
        B = b;

        double dx = b.X - a.X, dy = b.Y - a.Y;
        double chord = Math.Sqrt(dx * dx + dy * dy);

        if (Math.Abs(bulge) > 0.0001 && chord > 1e-9)
        {
            double theta = 4.0 * Math.Atan(bulge);
            double half = theta / 2.0;
            double r = chord / (2.0 * Math.Sin(Math.Abs(half)));
            double off = (chord / 2.0) / Math.Tan(half);   // со знаком: + центр слева от хорды

            var center = new Point(
                (a.X + b.X) / 2.0 + (-dy / chord) * off,
                (a.Y + b.Y) / 2.0 + (dx / chord) * off);

            IsArc = true;
            Bulge = bulge;
            Sweep = theta;
            R = r;
            Center = center;
            StartAngle = Math.Atan2(a.Y - center.Y, a.X - center.X);
        }
        else
        {
            IsArc = false;
            Bulge = 0;
            Sweep = 0;
            R = 0;
            Center = a;
            StartAngle = 0;
        }
    }

    /// <summary>+1: дуга идёт против часовой, -1: по часовой.</summary>
    public double Sign => Sweep >= 0 ? 1.0 : -1.0;

    /// <summary>Единичная касательная в точке p (по ходу сегмента).</summary>
    public Point Tangent(Point p)
    {
        if (!IsArc)
        {
            double dx = B.X - A.X, dy = B.Y - A.Y;
            double l = Math.Sqrt(dx * dx + dy * dy);
            return l < 1e-12 ? new Point(1, 0) : new Point(dx / l, dy / l);
        }
        double ux = (p.X - Center.X) / R, uy = (p.Y - Center.Y) / R;
        return new Point(-uy * Sign, ux * Sign);
    }

    /// <summary>Единичная нормаль слева по ходу сегмента.</summary>
    public Point LeftNormal(Point p)
    {
        var t = Tangent(p);
        return new Point(-t.Y, t.X);
    }

    /// <summary>Параметр точки вдоль сегмента: 0 — начало, 1 — конец (может выходить за [0,1]).</summary>
    public double Param(Point p)
    {
        if (!IsArc)
        {
            double dx = B.X - A.X, dy = B.Y - A.Y;
            double l2 = dx * dx + dy * dy;
            if (l2 < 1e-18) return 0;
            return ((p.X - A.X) * dx + (p.Y - A.Y) * dy) / l2;
        }

        double abs = Math.Abs(Sweep);
        double ang = Math.Atan2(p.Y - Center.Y, p.X - Center.X);
        double d = (ang - StartAngle) * Sign;
        d %= GeometryConstants.TwoPi;
        if (d < 0) d += GeometryConstants.TwoPi;
        if (d > abs + (GeometryConstants.TwoPi - abs) / 2) d -= GeometryConstants.TwoPi;   // точка «до начала» дуги
        return d / abs;
    }

    /// <summary>Расстояние от точки до сегмента (не до бесконечной кривой). t — зажатый параметр.</summary>
    public double DistanceTo(Point p, out double t)
    {
        double raw = Param(p);
        t = Math.Min(1, Math.Max(0, raw));

        if (!IsArc)
        {
            var q = new Point(A.X + (B.X - A.X) * t, A.Y + (B.Y - A.Y) * t);
            return p.DistanceTo(q);
        }

        if (raw >= 0 && raw <= 1)
            return Math.Abs(p.DistanceTo(Center) - R);

        double da = p.DistanceTo(A), db = p.DistanceTo(B);
        t = da <= db ? 0 : 1;
        return Math.Min(da, db);
    }

    /// <summary>Лежит ли угол в секторе дуги (работает для любых sweep, включая больше π).</summary>
    public static bool AngleInSweep(double startAngle, double sweep, double angle)
    {
        const double tol = 1e-7;
        double t = sweep >= 0 ? angle - startAngle : startAngle - angle;
        t %= GeometryConstants.TwoPi;
        if (t < 0) t += GeometryConstants.TwoPi;
        return t <= Math.Abs(sweep) + tol || t >= GeometryConstants.TwoPi - tol;
    }
}