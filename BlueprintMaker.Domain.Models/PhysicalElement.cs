using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models;

public abstract class PhysicalElement : WindowNodeBase, IMaterializable
{
    protected PhysicalElement(Guid id, Contour? localContour, IDerivationRule? rule)
        : base(id, localContour, rule)
    {
    }
    public abstract void Materialize(double width);

    public void MoveNode(int vertexIndex, Point newPosition)
    {
        var currentVertices = GetContour().Vertices.ToList();
        if (vertexIndex < 0 || vertexIndex >= currentVertices.Count)
            throw new ArgumentOutOfRangeException(nameof(vertexIndex));

        currentVertices[vertexIndex] = new ContourVertex(newPosition, currentVertices[vertexIndex].Bulge);
        UpdateLocalContour(RebuildContour(currentVertices));
    }

    public void SetSegmentArc(int startVertexIndex, double bulge)
    {
        var currentVertices = GetContour().Vertices.ToList();
        if (startVertexIndex < 0 || startVertexIndex >= currentVertices.Count - 1)
            throw new ArgumentOutOfRangeException(nameof(startVertexIndex));

        currentVertices[startVertexIndex] = new ContourVertex(currentVertices[startVertexIndex].Position, bulge);
        UpdateLocalContour(RebuildContour(currentVertices));
    }

    public void SplitAt(int segmentIndex, Point? position = null)
    {
        var currentVertices = GetContour().Vertices.ToList();
        if (segmentIndex < 0 || segmentIndex >= currentVertices.Count - 1)
            throw new ArgumentOutOfRangeException(nameof(segmentIndex));

        var targetPos = position ?? _getSegmentMidpoint(currentVertices[segmentIndex], currentVertices[segmentIndex + 1]);
        currentVertices.Insert(segmentIndex + 1, new ContourVertex(targetPos, 0));

        UpdateLocalContour(RebuildContour(currentVertices));
    }

    private Point _getSegmentMidpoint(ContourVertex from, ContourVertex to)
    {
        if (!from.IsArcStart)
            return from.Position.Midpoint(to.Position);

        var arc = BulgeMath.GetArcParameters(from.Position, to.Position, from.Bulge);
        double midAngle = arc.StartAngle + arc.SweepAngle / 2;
        return new Point(
            arc.Center.X + arc.Radius * Math.Cos(midAngle),
            arc.Center.Y + arc.Radius * Math.Sin(midAngle));
    }

    // Вспомогательный метод, чтобы понимать, замкнут контур (Рама) или открыт (Импост)
    protected abstract Contour RebuildContour(List<ContourVertex> vertices);
}
