namespace BlueprintMaker.Core.Geometry;

/// <summary>
/// Вершина контура. Хранит точку и кривизну (Bulge) сегмента, идущего от него к следующей вершине.
/// Bulge = 0 -> прямая линия. Bulge != 0 -> дуга.
/// </summary>
public readonly struct ContourVertex
{
    public Point Position { get; }
    public double Bulge { get; }

    public ContourVertex(Point position, double bulge = 0)
    {
        Position = position;
        Bulge = bulge;
    }

    public bool IsArcStart => Math.Abs(Bulge) > 0.0001;
}