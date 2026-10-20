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
    /// <param name="startSegment">Индекс сегмента, к которому крепится доборник</param>
    /// <param name="targetSpace">Целевое световое пространство (если внутренний). Null = внешний.</param>
    /// <param name="segmentCount">Количество сегментов</param>
    public Jamb AddJamb(int startSegment, LightSpace? targetSpace = null, int segmentCount = 1)
    {
        if (targetSpace == null)
        {
            // внешний: знак +1 (наружу)
            var externalJamb = new Jamb(Guid.NewGuid(), this, this, startSegment, segmentCount, 1);
            AddChildInternal(externalJamb);
            return externalJamb;
        }
        else
        {
            if (targetSpace.Children.Count > 0)
                throw new InvalidOperationException("Пространство уже занято.");

            // внутренний: знак -1 (внутрь)
            var internalJamb = new Jamb(Guid.NewGuid(), targetSpace, this, startSegment, segmentCount, -1);
            targetSpace.AddChildInternal(internalJamb);

            var remainingSpace = new LightSpace(Guid.NewGuid(), targetSpace);
            targetSpace.AddChildInternal(remainingSpace);

            return internalJamb;
        }
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
