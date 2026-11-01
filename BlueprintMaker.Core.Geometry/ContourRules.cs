namespace BlueprintMaker.Core.Geometry;

public static class ContourRules
{
    /// <summary>
    /// Замкнутый контур допустим, если у него 3 и больше вершин, либо 2 вершины и хотя бы одна дуга:
    /// две дуги по разные стороны хорды (линза, круг) или дуга и отрезок (сегмент круга).
    /// </summary>
    public static bool IsValidClosed(IReadOnlyList<ContourVertex> v)
    {
        if (v.Count >= 3) return true;
        if (v.Count != 2) return false;

        if (v[0].Position.DistanceTo(v[1].Position) < GeometryConstants.Tolerance) return false;

        bool a0 = v[0].IsArcStart, a1 = v[1].IsArcStart;
        if (!a0 && !a1) return false;                                  // отрезок туда и обратно
        if (a0 && a1 && Math.Sign(v[0].Bulge) != Math.Sign(v[1].Bulge))
            return false;                                              // обе дуги на одной стороне хорды
        return true;
    }
}