namespace BlueprintMaker.Core.Geometry;

public readonly struct Point : IEquatable<Point>
{
    public double X { get; }
    public double Y { get; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static readonly Point Zero = new(0, 0);

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator *(Point a, double scalar) => new(a.X * scalar, a.Y * scalar);
    public static Point operator /(Point a, double scalar) => new(a.X / scalar, a.Y / scalar);

    public double DistanceTo(Point other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public Point Midpoint(Point other) => new((X + other.X) / 2, (Y + other.Y) / 2);

    public bool Equals(Point other) =>
        Math.Abs(X - other.X) < 0.0001 && Math.Abs(Y - other.Y) < 0.0001;

    public override bool Equals(object? obj) => obj is Point p && Equals(p);
    public override int GetHashCode() => HashCode.Combine(Math.Round(X, 4), Math.Round(Y, 4));
    public static bool operator ==(Point a, Point b) => a.Equals(b);
    public static bool operator !=(Point a, Point b) => !a.Equals(b);

    public override string ToString() => $"({X:F2}, {Y:F2})";
}