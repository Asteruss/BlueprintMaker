using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models;

public abstract class PhysicalElement : WindowNodeBase, IMaterializable
{
    public double Width { get; private set; }

    protected PhysicalElement(Guid id, Contour? localContour, IDerivationRule? rule)
        : base(id, localContour, rule)
    {
    }

    public void Materialize(double width)
    {
        if (width <= 0) throw new ArgumentException("Ширина должна быть больше нуля.");
        Width = width;
        OnMaterialized();
    }
    public void Dematerialize()
    {
        Width = 0;
    }
    public virtual IReadOnlyList<Contour> GetPhysicalGeometry()
    {
        return new List<Contour> { GetContour() };
    }

    protected virtual void OnMaterialized() { }

    public override double GetPhysicalWidth() => Width == 0 ? GeometryConstants.DisplayWidth : Width;


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
        var currentContour = GetContour();
        var currentVertices = currentContour.Vertices.ToList();

        int maxSegmentIndex = currentVertices.Count - (currentContour.IsClosed ? 0 : 1);
        if (startVertexIndex < 0 || startVertexIndex >= maxSegmentIndex)
            throw new ArgumentOutOfRangeException(nameof(startVertexIndex));

        currentVertices[startVertexIndex] = new ContourVertex(currentVertices[startVertexIndex].Position, bulge);
        UpdateLocalContour(RebuildContour(currentVertices));
    }

    public void SetSegmentArcByControlPoint(int startVertexIndex, Point controlPoint)
    {
        var currentContour = GetContour();
        var currentVertices = currentContour.Vertices.ToList();

        int maxSegmentIndex = currentVertices.Count - (currentContour.IsClosed ? 0 : 1);
        if (startVertexIndex < 0 || startVertexIndex >= maxSegmentIndex)
            throw new ArgumentOutOfRangeException(nameof(startVertexIndex));

        var from = currentVertices[startVertexIndex].Position;

        var to = (currentContour.IsClosed && startVertexIndex == currentVertices.Count - 1)
            ? currentVertices[0].Position
            : currentVertices[startVertexIndex + 1].Position;

        // Вычисляем Bulge по 3 точкам
        double bulge = BulgeMath.BulgeFromThreePoints(from, controlPoint, to);
        SetSegmentArc(startVertexIndex, bulge);
    }

    public void SplitAt(int segmentIndex, Point? position = null)
    {
        var currentVertices = GetContour().Vertices.ToList();

        int maxSegmentIndex = currentVertices.Count - (GetContour().IsClosed ? 0 : 1);

        if (segmentIndex < 0 || segmentIndex >= maxSegmentIndex)
            throw new ArgumentOutOfRangeException(nameof(segmentIndex));

        var targetPos = position ?? _getSegmentMidpoint(currentVertices[segmentIndex], currentVertices[(segmentIndex + 1) % currentVertices.Count]);

        currentVertices.Insert(segmentIndex + 1, new ContourVertex(targetPos, 0));
        UpdateLocalContour(RebuildContour(currentVertices));
    }

    public void RemoveNode(int vertexIndex)
    {
        var currentVertices = GetContour().Vertices.ToList();
        if (vertexIndex < 0 || vertexIndex >= currentVertices.Count)
            throw new ArgumentOutOfRangeException(nameof(vertexIndex));

        currentVertices.RemoveAt(vertexIndex);
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

    /// <summary>
    /// Добавляет доборник к сегменту стены.
    /// </summary>
    public JambResult AddJamb(int segmentIndex, LightSpace? targetSpace = null) =>
        targetSpace != null ? targetSpace.InsertJamb(segmentIndex) : AddExternalJamb(segmentIndex);

    public JambResult AddExternalJamb(int segmentIndex)
    {
        var contour = GetContour();
        if (!contour.IsClosed)
            throw new InvalidOperationException("Внешний добор возможен только у замкнутого контура.");
        if (segmentIndex < 0 || segmentIndex >= contour.Vertices.Count)
            throw new ArgumentOutOfRangeException(nameof(segmentIndex));
        if (Children.OfType<Jamb>().Any(j => j.Side == JambSide.Outer && j.SegmentIndex == segmentIndex))
            throw new InvalidOperationException("На этом сегменте уже есть внешний добор.");

        var jamb = new Jamb(Guid.NewGuid(), segmentIndex, JambSide.Outer);
        AddChildInternal(jamb);
        return new JambResult(jamb, null);
    }

    protected List<ContourVertex> EnsureClockwise(List<ContourVertex> vertices)
    {
        // Проверяем только если точек 3 и больше (иначе площадь не посчитать)
        if (vertices.Count >= 3)
        {
            if (ContourMath.CalculateSignedArea(vertices) < 0)
            {
                vertices.Reverse();
            }
        }
        return vertices;
    }
    // Вспомогательный метод, чтобы понимать, замкнут контур (Рама) или открыт (Импост)
    protected abstract Contour RebuildContour(List<ContourVertex> vertices);
}
