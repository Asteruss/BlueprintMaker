namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>Очистка и разворот ломаных и замкнутых контуров.</summary>
internal static class PolylineUtils
{
    /// <summary>Убирает подряд идущие совпадающие точки, оставляя более позднюю вершину.</summary>
    public static List<ContourVertex> DedupOpen(IReadOnlyList<ContourVertex> verts, double tol = 0.001)
    {
        var res = new List<ContourVertex>(verts.Count);
        foreach (var v in verts)
        {
            if (res.Count > 0 && res[res.Count - 1].Position.DistanceTo(v.Position) <= tol)
                res[res.Count - 1] = v;
            else
                res.Add(v);
        }
        return res;
    }

    /// <summary>Чистит замкнутый контур от нулевых сегментов.</summary>
    public static List<ContourVertex> CleanClosed(IReadOnlyList<ContourVertex> v)
    {
        var res = new List<ContourVertex>(v.Count);
        foreach (var p in v)
        {
            if (res.Count > 0 && res[res.Count - 1].Position.DistanceTo(p.Position) < GeometryConstants.Tolerance)
                res[res.Count - 1] = p;
            else
                res.Add(p);
        }
        while (res.Count > 1 && res[res.Count - 1].Position.DistanceTo(res[0].Position) < GeometryConstants.Tolerance)
            res.RemoveAt(res.Count - 1);
        return res;
    }

    /// <summary>Чистит список вершин и создаёт замкнутый контур. null, если контур вырожден.</summary>
    public static Contour? MakeContour(List<ContourVertex> pts)
    {
        var c = CleanClosed(pts);
        if (!ContourRules.IsValidClosed(c)) return null;
        ContourMath.EnsureClockwise(c);
        return Contour.CreateClosed(c);
    }

    /// <summary>Разворот ломаной: Bulge сегмента меняет знак и переходит к новой начальной вершине сегмента.</summary>
    public static List<ContourVertex> ReverseOpen(List<ContourVertex> src)
    {
        int m = src.Count - 1;
        var res = new List<ContourVertex>(src.Count);
        for (int k = 0; k <= m; k++)
        {
            double bulge = k < m ? -src[m - k - 1].Bulge : 0;
            res.Add(new ContourVertex(src[m - k].Position, bulge));
        }
        return res;
    }

    /// <summary>Разворот замкнутого контура на месте с переносом Bulge на новые сегменты.</summary>
    public static void ReverseClosed(List<ContourVertex> v)
    {
        int n = v.Count;
        var old = v.ToArray();
        v.Clear();
        for (int i = 0; i < n; i++)
        {
            int segIdx = ((n - 2 - i) % n + n) % n;
            v.Add(new ContourVertex(old[n - 1 - i].Position, -old[segIdx].Bulge));
        }
    }
}