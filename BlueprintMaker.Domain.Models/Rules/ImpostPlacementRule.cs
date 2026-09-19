using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>
/// Стратегия: вычисляет отрезок импоста путем пересечения осевой линии с родителем.
/// </summary>
public sealed class ImpostPlacementRule : IDerivationRule
{
    public AxisOrientation Orientation { get; }
    public double RelativePosition { get; }

    public ImpostPlacementRule(AxisOrientation orientation, double relativePosition = 0.5)
    {
        Orientation = orientation;
        RelativePosition = Math.Clamp(relativePosition, 0, 1);
    }

    public Contour Resolve(IWindowNode parent)
    {
        var parentContour = parent.GetContour();
        var bounds = parentContour.BoundingBox;

        // Вычисляем координату осевой линии
        double coordinate = Orientation == AxisOrientation.Vertical
            ? bounds.MinX + RelativePosition * bounds.Width
            : bounds.MinY + RelativePosition * bounds.Height;

        // Ищем пересечения
        var intersections = ContourMath.IntersectWithLine(
            parentContour.Vertices, parentContour.IsClosed, Orientation, coordinate);

        if (intersections.Count != 2)
            throw new InvalidOperationException("Не удалось найти ровно 2 точки пересечения для импоста.");

        // Создаем открытый контур (отрезок) между двумя найденными точками
        return Contour.CreateOpen(new[]
        {
            new ContourVertex(intersections[0]),
            new ContourVertex(intersections[1])
        });
    }
}