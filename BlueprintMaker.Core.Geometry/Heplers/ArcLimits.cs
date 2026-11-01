namespace BlueprintMaker.Core.Geometry;

public static class ArcLimits
{
    /// <summary>Bulge = 1 — полуокружность: ширина дуги равна длине хорды.</summary>
    public const double MaxBulge = 1.0;

    public static double Clamp(double bulge) =>
        double.IsNaN(bulge) ? 0 : MathCompat.Clamp(bulge, -MaxBulge, MaxBulge);
}