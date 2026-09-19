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

        // Фиксируем направление 
        var p1 = intersections[0];
        var p2 = intersections[1];

        var start = p1;
        var end = p2;

        if (Orientation == AxisOrientation.Vertical)
        {
            // Вертикальный: start всегда снизу (Y меньше), end сверху
            if (p1.Y > p2.Y) { start = p2; end = p1; }
        }
        else
        {
            // Горизонтальный: start всегда слева (X меньше), end справа
            if (p1.X > p2.X) { start = p2; end = p1; }
        }

        // Создаем открытый контур (отрезок) между двумя найденными точками
        return Contour.CreateOpen(new[]
        {
            new ContourVertex(start),
            new ContourVertex(end)
        });
    }
}