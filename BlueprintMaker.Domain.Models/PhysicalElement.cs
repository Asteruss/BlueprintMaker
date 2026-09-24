using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

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

    public void SetSegmentArcByControlPoint(int startVertexIndex, Point controlPoint)
    {
        var currentVertices = GetContour().Vertices.ToList();
        if (startVertexIndex < 0 || startVertexIndex >= currentVertices.Count - 1)
            throw new ArgumentOutOfRangeException(nameof(startVertexIndex));

        var from = currentVertices[startVertexIndex].Position;
        var to = currentVertices[startVertexIndex + 1].Position;

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
            // внешний доборник: просто крепится снаружи, дерево-родитель - сама стена
            var externalJamb = new Jamb(Guid.NewGuid(), this, this, startSegment, segmentCount);
            AddChildInternal(externalJamb);
            return externalJamb;
        }
        else
        {
            // внутренний доборник: занимает слот в targetSpace
            if (targetSpace.Children.Count > 0)
                throw new InvalidOperationException("Пространство уже занято. Сначала очистите его.");

            // Геометрия берется от стены (this), а дерево-родитель - targetSpace
            var internalJamb = new Jamb(Guid.NewGuid(), targetSpace, this, startSegment, segmentCount);
            targetSpace.AddChildInternal(internalJamb);

            // Создаем остаток пространства
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
