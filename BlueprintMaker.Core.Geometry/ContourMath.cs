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

        var resultList = result.ToList();
        EnsureClockwise(resultList);
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

        // Фильтрация дубликатов
        var finalResults = new List<Point>();
        foreach (var p in results)
        {
            bool isDuplicate = false;
            foreach (var f in finalResults)
            {
                if (p.DistanceTo(f) < 0.001) // Точки почти совпадают
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate) finalResults.Add(p);
        }
        return finalResults;
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

            // Проверяем, лежит ли точка в секторе дуги
            double angle = Math.Atan2(candidate.Y - arc.Center.Y, candidate.X - arc.Center.X);

            if (IsAngleWithinSweep(arc.StartAngle, arc.SweepAngle, angle))
                yield return candidate;
        }
    }

    private static bool IsAngleWithinSweep(double startAngle, double sweep, double angle)
    {
        double t = angle - startAngle;
        while (t <= -Math.PI) t += 2 * Math.PI;
        while (t > Math.PI) t -= 2 * Math.PI;

        return sweep >= 0 ? (t >= 0 && t <= sweep) : (t <= 0 && t >= sweep);
    }

    private static Hit? FindHit(IReadOnlyList<ContourVertex> vertices, bool isClosed, Point target)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            if (!isClosed && i == vertices.Count - 1) break;
            var curr = vertices[i];
            var next = vertices[(i + 1) % vertices.Count];

            if (curr.Position == target)
                return new Hit { SegmentIndex = i, T = 0.0, Position = target };

            var proj = SegmentProjection.Project(curr.Position, next.Position, curr.Bulge, target);
            if (proj.Distance < 0.5) // Допуск 0.5 мм
            {
                return new Hit { SegmentIndex = i, T = proj.Parameter, Position = target };
            }
        }
        return null; 
    }


    /// <summary>
    /// Разрезает замкнутый контур открытой линией (импостом) на две половины
    /// </summary>
    public static (Contour? first, Contour? second) SliceContour(Contour parent, Contour cutter)
    {
        if (cutter.Vertices.Count < 2) return (null, null);
        return SliceWithPolyline(parent.Vertices, parent.IsClosed, cutter.Vertices);
    }

    private static (Contour? first, Contour? second) SliceWithPolyline(
        IReadOnlyList<ContourVertex> vertices, bool isClosed, IReadOnlyList<ContourVertex> cutterVerts)
    {
        // Убираем нулевые отрезки (дубли подряд идущих точек)
        var cutter = new List<ContourVertex>();
        foreach (var v in cutterVerts)
        {
            if (cutter.Count == 0 || cutter[cutter.Count - 1].Position.DistanceTo(v.Position) > 0.001)
                cutter.Add(v);
        }
        if (cutter.Count < 2) return (null, null);

        var hitA = FindHit(vertices, isClosed, cutter[0].Position);
        var hitB = FindHit(vertices, isClosed, cutter[cutter.Count - 1].Position);
        if (hitA == null || hitB == null) return (null, null);

        // Упорядочиваем по ходу контура; если надо, разворачиваем ломаную
        if (hitA.Value.SegmentIndex > hitB.Value.SegmentIndex ||
           (hitA.Value.SegmentIndex == hitB.Value.SegmentIndex && hitA.Value.T > hitB.Value.T))
        {
            (hitA, hitB) = (hitB, hitA);
            cutter = ReversePolyline(cutter);
        }

        int m = cutter.Count - 1;          // индекс последней точки ломаной
        var start = cutter[0].Position;
        var end = cutter[m].Position;

        // Половина 1: start -> по родителю до end -> обратно по ломаной к start
        var path1 = new List<ContourVertex> { new ContourVertex(start, 0) };
        for (int i = hitA.Value.SegmentIndex + 1; i <= hitB.Value.SegmentIndex; i++)
            path1.Add(vertices[i % vertices.Count]);

        path1.Add(new ContourVertex(end, -cutter[m - 1].Bulge));
        for (int k = m - 1; k >= 1; k--)
            path1.Add(new ContourVertex(cutter[k].Position, -cutter[k - 1].Bulge));

        // Половина 2: end -> по родителю до start -> вперёд по ломаной к end
        var path2 = new List<ContourVertex> { new ContourVertex(end, 0) };
        for (int i = hitB.Value.SegmentIndex + 1; i < vertices.Count + hitA.Value.SegmentIndex + 1; i++)
            path2.Add(vertices[i % vertices.Count]);

        path2.Add(new ContourVertex(start, cutter[0].Bulge));
        for (int k = 1; k <= m - 1; k++)
            path2.Add(new ContourVertex(cutter[k].Position, cutter[k].Bulge));

        if (path1.Count < 3 || path2.Count < 3) return (null, null);

        return (Contour.CreateClosed(EnsureClockwise(path1)),
                Contour.CreateClosed(EnsureClockwise(path2)));
    }

    // Разворот ломаной: bulge отрезка меняет знак и переходит к новой начальной точке отрезка
    private static List<ContourVertex> ReversePolyline(List<ContourVertex> src)
    {
        int m = src.Count - 1;
        var res = new List<ContourVertex>(src.Count);
        for (int k = 0; k <= m; k++)
        {
            double bulge = k < m ? -src[m - k - 1].Bulge : 0;
            res.Add(new ContourVertex(src[m - k].Position, bulge));
        }
        return res;
    }

    /// <summary>
    /// Вычисляет знак площади (положительный = по часовой стрелке, отрицательный = против).
    /// </summary>
    public static double CalculateSignedArea(IReadOnlyList<ContourVertex> vertices)
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

    public static List<ContourVertex> EnsureClockwise(List<ContourVertex> vertices)
    {
        if (vertices.Count >= 3 && CalculateSignedArea(vertices) < 0)
        {
            vertices.Reverse();
        }
        return vertices;
    }
    public static List<ContourVertex> OffsetPolyline(IReadOnlyList<ContourVertex> vertices, double distance)
    {
        var result = new List<ContourVertex>();
        int n = vertices.Count;
        if (n == 0) return result;

        for (int i = 0; i < n; i++)
        {
            var curr = vertices[i];
            var prev = i > 0 ? vertices[i - 1] : curr;
            var next = i < n - 1 ? vertices[i + 1] : curr;

            double dx1 = curr.Position.X - prev.Position.X;
            double dy1 = curr.Position.Y - prev.Position.Y;
            double len1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);

            double dx2 = next.Position.X - curr.Position.X;
            double dy2 = next.Position.Y - curr.Position.Y;
            double len2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

            double nx = 0, ny = 0;
            // Левая нормаль на экране (наружу для обхода по часовой)
            if (len1 > 0.0001) { nx += dy1 / len1; ny += -dx1 / len1; }
            if (len2 > 0.0001) { nx += dy2 / len2; ny += -dx2 / len2; }

            double nlen = Math.Sqrt(nx * nx + ny * ny);
            if (nlen > 0.0001) { nx /= nlen; ny /= nlen; }

            var offset = new Point(nx * distance, ny * distance);
            result.Add(new ContourVertex(curr.Position + offset, curr.Bulge));
        }
        return result;
    }

    /// <summary>
    /// Создает замкнутую полосу (брус) вокруг открытого контура (центральной линии).
    /// Расширяется в обе стороны на halfWidth.
    /// </summary>
    public static IReadOnlyList<ContourVertex> OffsetBandCenter(IReadOnlyList<ContourVertex> vertices, double width)
    {
        double halfWidth = width / 2.0;
        var leftSide = new List<ContourVertex>();
        var rightSide = new List<ContourVertex>();

        for (int i = 0; i < vertices.Count; i++)
        {
            var curr = vertices[i];
            var prev = i > 0 ? vertices[i - 1] : curr;
            var next = i < vertices.Count - 1 ? vertices[i + 1] : curr;

            var dirPrev = new Point(curr.Position.X - prev.Position.X, curr.Position.Y - prev.Position.Y);
            var dirNext = new Point(next.Position.X - curr.Position.X, next.Position.Y - curr.Position.Y);
            var dirSum = new Point(dirPrev.X + dirNext.X, dirPrev.Y + dirNext.Y);
            double len = Math.Sqrt(dirSum.X * dirSum.X + dirSum.Y * dirSum.Y);

            Point normal;
            if (len > 0.001)
            {
                normal = new Point(-dirSum.Y / len, dirSum.X / len);
            }
            else
            {
                double dx = next.Position.X - curr.Position.X;
                double dy = next.Position.Y - curr.Position.Y;
                double l = Math.Sqrt(dx * dx + dy * dy);
                normal = l > 0.001 ? new Point(-dy / l, dx / l) : new Point(0, 0);
            }

            leftSide.Add(new ContourVertex(
                new Point(curr.Position.X - normal.X * halfWidth, curr.Position.Y - normal.Y * halfWidth),
                curr.Bulge));

            rightSide.Add(new ContourVertex(
                new Point(curr.Position.X + normal.X * halfWidth, curr.Position.Y + normal.Y * halfWidth),
                curr.Bulge));
        }

        var result = new List<ContourVertex>(leftSide);
        rightSide.Reverse();
        result.AddRange(rightSide);

        return result;
    }


    /// <summary>
    /// Отсекает часть многоугольника subject, которая выходит за пределы многоугольника clipWindow
    /// </summary>
    public static IReadOnlyList<ContourVertex> ClipPolygon(IReadOnlyList<ContourVertex> subject, IReadOnlyList<ContourVertex> clipWindow)
    {
        var outputList = new List<ContourVertex>(subject);

        // Итерируемся по ребрам окна отсечения
        for (int i = 0; i < clipWindow.Count; i++)
        {
            var clipEdgeStart = clipWindow[i].Position;
            var clipEdgeEnd = clipWindow[(i + 1) % clipWindow.Count].Position;

            var inputList = outputList;
            outputList = new List<ContourVertex>();

            if (inputList.Count == 0) break;

            // Нормаль ребра отсечения (внутрь многоугольника)
            double dx = clipEdgeEnd.X - clipEdgeStart.X;
            double dy = clipEdgeEnd.Y - clipEdgeStart.Y;
            // Предполагаем обход по часовой стрелке, поэтому нормаль (-dy, dx)
            double nx = -dy;
            double ny = dx;

            for (int j = 0; j < inputList.Count; j++)
            {
                var current = inputList[j];
                var prev = inputList[(j - 1 + inputList.Count) % inputList.Count];

                bool currentInside = IsInside(current.Position, clipEdgeStart, nx, ny);
                bool prevInside = IsInside(prev.Position, clipEdgeStart, nx, ny);

                if (currentInside)
                {
                    if (!prevInside)
                    {
                        // Заходим внутрь: добавляем точку пересечения
                        outputList.Add(new ContourVertex(Intersect(prev.Position, current.Position, clipEdgeStart, clipEdgeEnd), 0));
                    }
                    outputList.Add(current);
                }
                else if (prevInside)
                {
                    // Выходим наружу: добавляем точку пересечения
                    outputList.Add(new ContourVertex(Intersect(prev.Position, current.Position, clipEdgeStart, clipEdgeEnd), 0));
                }
            }
        }

        return outputList;
    }

    private static bool IsInside(Point test, Point edgeStart, double nx, double ny)
    {
        double px = test.X - edgeStart.X;
        double py = test.Y - edgeStart.Y;
        return (px * nx + py * ny) >= -0.5;
    }

    private static Point Intersect(Point p1, Point p2, Point p3, Point p4)
    {
        // Пересечение отрезков p1-p2 и p3-p4
        double d1x = p2.X - p1.X, d1y = p2.Y - p1.Y;
        double d2x = p4.X - p3.X, d2y = p4.Y - p3.Y;

        double denom = d1x * d2y - d1y * d2x;
        if (Math.Abs(denom) < 1e-9) return p2; // Параллельны

        double t = ((p3.X - p1.X) * d2y - (p3.Y - p1.Y) * d2x) / denom;
        return new Point(p1.X + d1x * t, p1.Y + d1y * t);
    }
}