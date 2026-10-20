namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>Поиск точки на родительском контуре и обход его границы.</summary>
internal static class BoundaryWalker
{
    /// <summary>
    /// Находит сегмент родителя, на котором лежит точка (допуск 0.5 мм).
    /// Попадание в вершину всегда относится к началу следующего сегмента.
    /// </summary>
    public static Hit? FindHit(ContourSegment[] segs, Point target)
    {
        int n = segs.Length;
        double best = double.MaxValue;
        int bi = -1;
        double bt = 0;

        for (int i = 0; i < n; i++)
        {
            double d = segs[i].DistanceTo(target, out double t);
            if (d < best - 1e-9) { best = d; bi = i; bt = t; }
        }
        if (bi < 0 || best > 0.5) return null;

        if (segs[bi].B.DistanceTo(target) < GeometryConstants.Tolerance * 10)
        {
            bi = (bi + 1) % n;
            bt = 0;
        }
        return new Hit { SegmentIndex = bi, T = bt, Position = target };
    }

    /// <summary>
    /// Путь по родителю вперёд от точки S (hit from) до точки E (hit to).
    /// Вершины S...(без E), у каждой Bulge правильной части дуги.
    /// </summary>
    public static List<ContourVertex> WalkParent(ContourSegment[] segs, Hit from, Point S, Hit to, Point E)
    {
        int n = segs.Length;
        double posF = from.SegmentIndex + from.T;
        double posT = to.SegmentIndex + to.T;
        int fs = from.SegmentIndex;
        int ts = to.SegmentIndex + (posT < posF ? n : 0);

        var res = new List<ContourVertex>();

        if (ts == fs)
        {
            res.Add(new ContourVertex(S, SubBulge(segs[fs], S, E)));
            return res;
        }

        var first = segs[fs];
        res.Add(new ContourVertex(S, SubBulge(first, S, first.B)));

        for (int j = fs + 1; j < ts; j++)
        {
            var sg = segs[j % n];
            res.Add(new ContourVertex(sg.A, sg.IsArc ? sg.Bulge : 0));
        }

        var last = segs[ts % n];
        res.Add(new ContourVertex(last.A, SubBulge(last, last.A, E)));
        return res;
    }

    private static double SubBulge(ContourSegment s, Point from, Point to) =>
        SegmentMath.SubBulge(s, from, to, Math.Abs(s.Sweep) + 0.05);

    private static double PathLength(List<ContourVertex> pts, Point end)
    {
        double len = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[i].Position;
            var b = i + 1 < pts.Count ? pts[i + 1].Position : end;
            double chord = a.DistanceTo(b);
            double bg = pts[i].Bulge;
            if (Math.Abs(bg) > 1e-4)
            {
                double th = 4 * Math.Atan(Math.Abs(bg));
                len += chord * th / (2 * Math.Sin(th / 2));
            }
            else len += chord;
        }
        return len;
    }

    /// <summary>Кратчайший путь по границе родителя от pFrom до pTo (pTo не включается).</summary>
    public static List<ContourVertex> BoundaryPath(ContourSegment[] segs, Hit hFrom, Point pFrom, Hit hTo, Point pTo)
    {
        var fwd = WalkParent(segs, hFrom, pFrom, hTo, pTo);
        var bwdRaw = WalkParent(segs, hTo, pTo, hFrom, pFrom);
        if (PathLength(fwd, pTo) <= PathLength(bwdRaw, pFrom)) return fwd;

        var tmp = new List<ContourVertex>(bwdRaw) { new ContourVertex(pFrom, 0) };
        var rev = PolylineUtils.ReverseOpen(tmp);   // от pFrom к pTo
        rev.RemoveAt(rev.Count - 1);                // pTo не включаем
        return rev;
    }
}