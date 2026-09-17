namespace BlueprintMaker.Core.Geometry.Heplers;

public readonly struct ArcParameters
{
    public Point Center { get; }
    public double Radius { get; }
    public double StartAngle { get; }
    public double SweepAngle { get; }

    public ArcParameters(Point center, double radius, double startAngle, double sweepAngle)
    {
        Center = center;
        Radius = radius;
        StartAngle = startAngle;
        SweepAngle = sweepAngle;
    }
}