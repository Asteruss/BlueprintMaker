namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>Разрез замкнутого контура ломаной</summary>
internal static class ContourSlicer
{
    /// <summary>Убирает дубли и ориентирует ломаную по ходу контура родителя</summary>
    public static List<ContourVertex>? OrderCutter(
        IReadOnlyList<ContourVertex> parentVerts, IReadOnlyList<ContourVertex> cutterVerts)
    {
        var c = PolylineUtils.DedupOpen(cutterVerts);
        if (c.Count < 2) return null;

        var segs = SegmentMath.BuildSegs(parentVerts, true);
        if (segs.Length < 2) return null;

        var hA = BoundaryWalker.FindHit(segs, c[0].Position);
        var hB = BoundaryWalker.FindHit(segs, c[c.Count - 1].Position);
        if (!hA.HasValue || !hB.HasValue) return null;

        if (hA.Value.SegmentIndex + hA.Value.T > hB.Value.SegmentIndex + hB.Value.T)
            c = PolylineUtils.ReverseOpen(c);
        return c;
    }

    /// <summary>
    /// Режет родителя ломаной. Возвращает (forward, backward):
    /// forward — часть, идущая по родителю вперёд от начала ломаной к её концу.
    /// </summary>
    public static (Contour? forward, Contour? backward) Slice(
        IReadOnlyList<ContourVertex> parentVerts, IReadOnlyList<ContourVertex> cutterVerts)
    {
        var cutter = PolylineUtils.DedupOpen(cutterVerts);
        if (cutter.Count < 2) return (null, null);

        var segs = SegmentMath.BuildSegs(parentVerts, true);
        if (segs.Length < 2) return (null, null);

        int m = cutter.Count - 1;
        var S = cutter[0].Position;
        var E = cutter[m].Position;

        var hA = BoundaryWalker.FindHit(segs, S);
        var hB = BoundaryWalker.FindHit(segs, E);
        if (!hA.HasValue || !hB.HasValue) return (null, null);

        double posA = hA.Value.SegmentIndex + hA.Value.T;
        double posB = hB.Value.SegmentIndex + hB.Value.T;
        if (Math.Abs(posA - posB) < 1e-9) return (null, null);

        // forward: по родителю от S до E, затем назад по ломаной
        var p1 = BoundaryWalker.WalkParent(segs, hA.Value, S, hB.Value, E);
        p1.Add(new ContourVertex(E, -cutter[m - 1].Bulge));
        for (int k = m - 1; k >= 1; k--)
            p1.Add(new ContourVertex(cutter[k].Position, -cutter[k - 1].Bulge));

        // backward: по родителю от E до S, затем вперёд по ломаной
        var p2 = BoundaryWalker.WalkParent(segs, hB.Value, E, hA.Value, S);
        p2.Add(new ContourVertex(S, cutter[0].Bulge));
        for (int k = 1; k <= m - 1; k++)
            p2.Add(new ContourVertex(cutter[k].Position, cutter[k].Bulge));

        return (PolylineUtils.MakeContour(p1), PolylineUtils.MakeContour(p2));
    }
}