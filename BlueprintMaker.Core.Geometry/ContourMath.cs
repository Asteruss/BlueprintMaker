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
}