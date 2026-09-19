using BlueprintMaker.Core.Geometry.Heplers;

namespace BlueprintMaker.Core.Geometry;

/// <summary>
/// Операции над готовым контуром: отступы, пересечения, габариты
/// </summary>
public static class ContourMath
{
    public static Rect CalculateBounds(IReadOnlyList<ContourVertex> vertices, bool isClosed)
    {
        if (vertices.Count == 0) return default;

        var bounds = Rect.FromPoint(vertices[0].Position);

        for (int i = 0; i < vertices.Count; i++)
        {
            bounds = bounds.Union(vertices[i].Position);

            // В замкнутом контуре последний сегмент соединяет последнюю вершину с первой
            bool isLast = i == vertices.Count - 1;
            if (isLast && !isClosed) continue;

            var next = isLast ? vertices[0] : vertices[i + 1];

            if (vertices[i].IsArcStart)
            {
                var arc = BulgeMath.GetArcParameters(vertices[i].Position, next.Position, vertices[i].Bulge);
                bounds = bounds.Union(new Point(arc.Center.X + arc.Radius, arc.Center.Y));
                bounds = bounds.Union(new Point(arc.Center.X - arc.Radius, arc.Center.Y));
                bounds = bounds.Union(new Point(arc.Center.X, arc.Center.Y + arc.Radius));
                bounds = bounds.Union(new Point(arc.Center.X, arc.Center.Y - arc.Radius));
            }
        }
        return bounds;
    }

    /// <summary>
    /// Отступ внутрь на заданную ширину. Рама -> световое пространство
    /// </summary>
    public static IReadOnlyList<ContourVertex> Inset(IReadOnlyList<ContourVertex> vertices, double width)
    {
        int n = vertices.Count;
        if (n < 3) throw new ArgumentException("Inset требует замкнутый контур.");

        double orientationSign = Math.Sign(SignedArea(vertices));
        if (orientationSign == 0) orientationSign = 1;

        var newPositions = new Point[n];

        for (int i = 0; i < n; i++)
        {
            var prev = vertices[(i - 1 + n) % n];
            var curr = vertices[i];
            var next = vertices[(i + 1) % n];

            // Сдвигаем входящую и исходящую линии внутрь
            var offsetIncoming = OffsetLine(prev.Position, curr.Position, width, orientationSign);
            var offsetOutgoing = OffsetLine(curr.Position, next.Position, width, orientationSign);

            // Точка пересечения сдвинутых линий - это точный новый угол
            var intersection = IntersectLines(offsetIncoming, offsetOutgoing);

            if (intersection.HasValue)
            {
                newPositions[i] = intersection.Value;
            }
            else
            {
                // Если линии параллельны (не пересекаются), просто сдвигаем по нормали
                var normal = InwardNormal(curr.Position, next.Position, orientationSign);
                newPositions[i] = curr.Position + normal * width;
            }
        }

        var result = new ContourVertex[n];
        for (int i = 0; i < n; i++)
            result[i] = new ContourVertex(newPositions[i], vertices[i].Bulge);

        return result;
    }

    private static double SignedArea(IReadOnlyList<ContourVertex> vertices)
    {
        double sum = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            var a = vertices[i].Position;
            var b = vertices[(i + 1) % vertices.Count].Position;
            sum += a.X * b.Y - b.X * a.Y;
        }
        return sum / 2;
    }

    // Вспомогательная структура для отрезка
    private readonly record struct OffsetLineStruct(Point A, Point B);

    // Сдвиг прямой линии на ширину width внутрь
    private static OffsetLineStruct OffsetLine(Point from, Point to, double width, double orientationSign)
    {
        var offset = InwardNormal(from, to, orientationSign) * width;
        return new OffsetLineStruct(from + offset, to + offset);
    }

    // Нахождение точки пересечения двух бесконечных прямых
    private static Point? IntersectLines(OffsetLineStruct a, OffsetLineStruct b)
    {
        double x1 = a.A.X, y1 = a.A.Y, x2 = a.B.X, y2 = a.B.Y;
        double x3 = b.A.X, y3 = b.A.Y, x4 = b.B.X, y4 = b.B.Y;

        double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
        if (Math.Abs(denom) < 0.0001) return null; // Параллельны

        double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;

        return new Point(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
    }

    private static Point InwardNormal(Point from, Point to, double orientationSign)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 0.0001) return Point.Zero;
        return new Point(-dy / len * orientationSign, dx / len * orientationSign);
    }

    /// <summary>
    /// Находит точки пересечения контура с осевой линией (вертикальной или горизонтальной).
    /// </summary>
    public static IReadOnlyList<Point> IntersectWithLine(IReadOnlyList<ContourVertex> vertices, bool isClosed, AxisOrientation orientation, double coordinate)
    {
        var results = new List<Point>();
        int n = vertices.Count;
        int limit = isClosed ? n : n - 1;

        for (int i = 0; i < limit; i++)
        {
            var current = vertices[i];
            var next = isClosed && i == n - 1 ? vertices[0] : vertices[i + 1];

            if (Math.Abs(current.Bulge) < 0.0001)
            {
                // Прямая линия
                var p = IntersectStraightSegment(current.Position, next.Position, orientation, coordinate);
                if (p.HasValue) results.Add(p.Value);
            }
            else
            {
                // Дуга
                var arc = BulgeMath.GetArcParameters(current.Position, next.Position, current.Bulge);
                foreach (var p in IntersectArc(arc, orientation, coordinate))
                    results.Add(p);
            }
        }

        return results;
    }

    private static Point? IntersectStraightSegment(Point from, Point to, AxisOrientation orientation, double coordinate)
    {
        double a = orientation == AxisOrientation.Vertical ? from.X : from.Y;
        double b = orientation == AxisOrientation.Vertical ? to.X : to.Y;

        if (Math.Abs(a - b) < 0.0001) return null; // Параллельно оси

        double t = (coordinate - a) / (b - a);
        if (t < 0 || t > 1) return null; // За пределами отрезка

        return orientation == AxisOrientation.Vertical
            ? new Point(coordinate, from.Y + (to.Y - from.Y) * t)
            : new Point(from.X + (to.X - from.X) * t, coordinate);
    }

    private static IEnumerable<Point> IntersectArc(ArcParameters arc, AxisOrientation orientation, double coordinate)
    {
        // Уравнение окружности: (x - cx)^2 + (y - cy)^2 = r^2
        double fixedDelta = orientation == AxisOrientation.Vertical
            ? coordinate - arc.Center.X
            : coordinate - arc.Center.Y;

        double underSqrt = arc.Radius * arc.Radius - fixedDelta * fixedDelta;
        if (underSqrt < 0) yield break; // Линия не пересекает круг

        double offset = Math.Sqrt(underSqrt);

        for (int sign = -1; sign <= 1; sign += 2)
        {
            var candidate = orientation == AxisOrientation.Vertical
                ? new Point(coordinate, arc.Center.Y + sign * offset)
                : new Point(arc.Center.X + sign * offset, coordinate);

            // Проверяем, лежит ли точка в секторе дуги (упрощенная проверка по углу)
            double angle = Math.Atan2(candidate.Y - arc.Center.Y, candidate.X - arc.Center.X);
            double startAngle = arc.StartAngle;
            double endAngle = arc.StartAngle + arc.SweepAngle;

            bool withinSweep = arc.SweepAngle > 0
                ? (angle >= startAngle && angle <= endAngle)
                : (angle <= startAngle && angle >= endAngle);

            if (withinSweep) yield return candidate;
        }
    }
}