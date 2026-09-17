namespace BlueprintMaker.Core.Geometry;

public readonly struct Rect
{
    public double MinX { get; }
    public double MinY { get; }
    public double MaxX { get; }
    public double MaxY { get; }

    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public Point Center => new((MinX + MaxX) / 2, (MinY + MaxY) / 2);

    private Rect(double minX, double minY, double maxX, double maxY)
    {
        MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
    }

    public static Rect FromPoint(Point p) => new(p.X, p.Y, p.X, p.Y);

    public Rect Union(Point p) => new(
        Math.Min(MinX, p.X),
        Math.Min(MinY, p.Y),
        Math.Max(MaxX, p.X),
        Math.Max(MaxY, p.Y));
}
