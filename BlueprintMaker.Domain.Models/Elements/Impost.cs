using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;
using System.Data;

namespace BlueprintMaker.Domain.Models.Elements;



public sealed class Impost : WindowNodeBase, IMaterializable
{
    public double Width { get; private set; }

    // Конструктор без ширины
    internal Impost(Guid id, IWindowNode parent, AxisOrientation orientation, double relativePosition)
        : base(id, localContour: null, rule: new ImpostPlacementRule(orientation, relativePosition))
    {
    }

    // Если не материализован (Width == 0), возвращаем дефолт для UI. Иначе реальную ширин
    public override double GetPhysicalWidth()
    {
        return Width == 0 ? GeometryConstants.DisplayWidth : Width;
    }

    public void Materialize(double width)
    {
        if (width <= 0) throw new ArgumentException("Ширина должна быть больше нуля.");
        Width = width;
    }

    public void UpdatePosition(double newRelativePosition)
    {
        // Пересоздаем правило с новой позицией
        var orientation = (Rule as ImpostPlacementRule)?.Orientation ?? AxisOrientation.Vertical;
        SetRule(new ImpostPlacementRule(orientation, newRelativePosition));
    }

    /// <summary>
    /// Двигает один узел
    /// </summary>
    public void MoveNode(int vertexIndex, Point newPosition)
    {
        var vertices = GetContour().Vertices.ToList();
        if (vertexIndex < 0 || vertexIndex >= vertices.Count)
            throw new ArgumentOutOfRangeException(nameof(vertexIndex));

        // Замораживаем (если еще не заморожен) и берем актуальный список вершин
        var currentVertices = GetContour().Vertices.ToList();
        currentVertices[vertexIndex] = new ContourVertex(newPosition, currentVertices[vertexIndex].Bulge);

        UpdateLocalContour(Contour.CreateOpen(currentVertices));
    }

    /// <summary>
    /// Превращает прямой сегмент в дугу с заданным Bulge
    /// </summary>
    public void SetSegmentArc(int startVertexIndex, double bulge)
    {
        var currentVertices = GetContour().Vertices.ToList();
        if (startVertexIndex < 0 || startVertexIndex >= currentVertices.Count - 1)
            throw new ArgumentOutOfRangeException(nameof(startVertexIndex));

        currentVertices[startVertexIndex] = new ContourVertex(currentVertices[startVertexIndex].Position, bulge);

        UpdateLocalContour(Contour.CreateOpen(currentVertices));
    }

    /// <summary>
    /// Разбивает сегмент на два, вставляя новую точку.
    /// </summary>
    public void SplitAt(int segmentIndex, Point? position = null)
    {
        var currentVertices = GetContour().Vertices.ToList();
        if (segmentIndex < 0 || segmentIndex >= currentVertices.Count - 1)
            throw new ArgumentOutOfRangeException(nameof(segmentIndex));

        var targetPos = position ?? _getSegmentMidpoint(currentVertices[segmentIndex], currentVertices[segmentIndex + 1]);

        // Вставляем новую вершину после segmentIndex
        currentVertices.Insert(segmentIndex + 1, new ContourVertex(targetPos, 0));

        UpdateLocalContour(Contour.CreateOpen(currentVertices));
    }

    /// <summary>
    /// Вычисляет точную середину сегмента. Если это дуга, считает через углы окружности.
    /// </summary>
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
}