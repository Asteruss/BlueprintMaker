namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>
/// Результат проекции точки на сегмент: ближайшая точка, параметр вдоль сегмента и дистанция
/// </summary>
public readonly struct SegmentProjectionResult
{
    public Point ClosestPoint { get; }
    public double Parameter { get; }   // 0..1 вдоль сегмента
    public double Distance { get; }

    public SegmentProjectionResult(Point closestPoint, double parameter, double distance)
    {
        ClosestPoint = closestPoint;
        Parameter = parameter;
        Distance = distance;
    }
}