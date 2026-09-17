using BlueprintMaker.Core.Geometry.Heplers;

namespace BlueprintMaker.Core.Geometry;

/// <summary>
/// Конвертация между визуальной точкой (куда тянул пользователь) и компактным значением Bulge
/// </summary>
public static class BulgeMath
{
    /// <summary>
    /// Вычисляет Bulge по трем точкам: начало сегмента, контрольная точка (мышь), конец сегмента
    /// </summary>
    public static double BulgeFromThreePoints(Point start, Point control, Point end)
    {
        // Определяем, с какой стороны от прямой (start-end) лежит контрольная точка
        double cross = (end.X - start.X) * (control.Y - start.Y) - (end.Y - start.Y) * (control.X - start.X);

        // Вычисляем угол между векторами (control-start) и (end-control)
        // Упрощенный расчет bulge через тангенс четверти угла
        double d1 = start.DistanceTo(control);
        double d2 = control.DistanceTo(end);
        double d3 = start.DistanceTo(end);

        if (d1 < 0.0001 || d2 < 0.0001 || d3 < 0.0001) return 0;

        // Формула угла по трем сторонам (теорема косинусов) -> тангенс половинного угла
        // Чтобы не усложнять код сложной тригонометрией, используем геометрический подход:
        // Считаем высоту треугольника (h) и основание (d3)
        double s = (d1 + d2 + d3) / 2; // полупериметр
        double area = Math.Sqrt(Math.Max(0, s * (s - d1) * (s - d2) * (s - d3)));
        double h = d3 > 0.0001 ? 2 * area / d3 : 0;

        // tan(θ/4) = h / (d3/2) = 2h / d3
        double bulge = (2 * h) / d3;

        // Если точка справа от вектора (по часовой), bulge отрицательный, иначе положительный
        return cross > 0 ? bulge : -bulge;
    }

    /// <summary>
    /// Восстанавливает параметры дуги по началу, концу и сохраненному Bulge
    /// </summary>
    public static ArcParameters GetArcParameters(Point start, Point end, double bulge)
    {
        if (Math.Abs(bulge) < 0.0001)
            throw new InvalidOperationException("Сегмент не является дугой (Bulge = 0).");

        double chord = start.DistanceTo(end);
        if (chord < 0.0001)
            throw new InvalidOperationException("Вырожденный сегмент: точки совпадают.");

        // theta = 4 * atan(bulge)
        double theta = 4 * Math.Atan(bulge);
        double radius = chord / (2 * Math.Sin(theta / 2));

        Point mid = start.Midpoint(end);
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;

        // Нормаль к хорде
        double nx = -dy / chord;
        double ny = dx / chord;

        double apothem = radius * Math.Cos(theta / 2);
        double sign = bulge > 0 ? 1 : -1;

        var center = new Point(
            mid.X + nx * apothem * sign,
            mid.Y + ny * apothem * sign);

        double startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
        double sweepAngle = theta;

        return new ArcParameters(center, radius, startAngle, sweepAngle);
    }
}