namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>Смещение открытой ломаной (с дугами) и обрезка по родителю.</summary>
internal static class PolylineOffset
{
    /// <summary>
    /// Точное смещение открытой ломаной с дугами на d (d &gt; 0 — влево по ходу).
    /// Прямые сдвигаются параллельно, дуги — концентрически; вершины — пересечения смещённых кривых.
    /// Bulge вершины относится к сегменту от неё к следующей.
    /// </summary>
    public static List<ContourVertex> Offset(IReadOnlyList<ContourVertex> verts, double d)
    {
        var src = PolylineUtils.DedupOpen(verts, GeometryConstants.Tolerance);
        if (src.Count < 2) return src;

        int m = src.Count - 1;
        var segs = SegmentMath.BuildSegs(src, false);
        var pts = new Point[m + 1];

        pts[0] = SegmentMath.Shift(segs[0].A, segs[0].LeftNormal(segs[0].A), d);
        for (int k = 1; k < m; k++)
            pts[k] = SegmentMath.OffsetJoint(segs[k - 1], segs[k], d);
        pts[m] = SegmentMath.Shift(segs[m - 1].B, segs[m - 1].LeftNormal(segs[m - 1].B), d);

        var res = new List<ContourVertex>(m + 1);
        for (int i = 0; i < m; i++)
        {
            double bulge = segs[i].IsArc
                ? SegmentMath.SubBulge(segs[i], pts[i], pts[i + 1], GeometryConstants.MaxOffsetSweep)
                : 0;
            res.Add(new ContourVertex(pts[i], bulge));
        }
        res.Add(new ContourVertex(pts[m], 0));
        return res;
    }

    /// <summary>
    /// Обрезает продлённую ломаную по контуру родителя: берутся первое и последнее пересечения.
    /// Возвращает ломаную с правильными Bulge частичных дуг.
    /// </summary>
    public static List<ContourVertex>? TrimToParent(IReadOnlyList<ContourVertex> line, IReadOnlyList<ContourVertex> parent)
    {
        var ls = SegmentMath.BuildSegs(line, false);
        var ps = SegmentMath.BuildSegs(parent, true);
        if (ls.Length == 0 || ps.Length == 0) return null;

        var hits = new List<(int seg, double t, Point p)>();
        for (int i = 0; i < ls.Length; i++)
            for (int j = 0; j < ps.Length; j++)
                foreach (var (ta, _, p) in SegmentMath.IntersectSegs(ls[i], ps[j]))
                    hits.Add((i, ta, p));

        hits.Sort((x, y) => (x.seg + x.t).CompareTo(y.seg + y.t));

        // Пересечение ровно в вершине даёт две записи
        var uniq = new List<(int seg, double t, Point p)>();
        foreach (var h in hits)
            if (uniq.Count == 0 || uniq[uniq.Count - 1].p.DistanceTo(h.p) > GeometryConstants.Tolerance)
                uniq.Add(h);
        if (uniq.Count < 2) return null;

        var first = uniq[0];
        var last = uniq[uniq.Count - 1];

        var pts = new List<Point> { first.p };
        var sidx = new List<int> { first.seg };
        for (int k = first.seg + 1; k <= last.seg; k++)
        {
            pts.Add(ls[k].A);
            sidx.Add(k);
        }
        pts.Add(last.p);

        var res = new List<ContourVertex>();
        for (int q = 0; q < pts.Count - 1; q++)
            res.Add(new ContourVertex(pts[q],
                SegmentMath.SubBulge(ls[sidx[q]], pts[q], pts[q + 1], GeometryConstants.MaxOffsetSweep)));
        res.Add(new ContourVertex(pts[pts.Count - 1], 0));

        res = PolylineUtils.DedupOpen(res, GeometryConstants.Tolerance);
        return res.Count >= 2 ? res : null;
    }
}