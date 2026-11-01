using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueprintMaker.Core.Geometry;

/// <summary>Сторона, в которую смотрит узкое основание трапеции.</summary>
public enum ShapeSide { Top = 0, Right = 1, Bottom = 2, Left = 3 }

/// <summary>
/// Фабрика контуров окон. Все контуры замкнуты, идут по часовой стрелке на экране
/// (ось Y вниз), origin — левый верхний угол габарита
/// </summary>
public static class FrameShapeFactory
{
    public static Contour Rectangle(double width, double height, Point? origin = null)
    {
        Require(width, nameof(width));
        Require(height, nameof(height));

        return Build(new[]
        {
            new Point(0, 0), new Point(width, 0),
            new Point(width, height), new Point(0, height)
        }, origin);
    }

    /// <param name="baseWidth">Длина широкого основания</param>
    /// <param name="narrowWidth">Длина узкого основания (меньше baseWidth)</param>
    /// <param name="height">Расстояние между основаниями</param>
    /// <param name="narrowSide">Куда смотрит узкое основание</param>
    public static Contour Trapezoid(
        double baseWidth, double narrowWidth, double height,
        ShapeSide narrowSide = ShapeSide.Top, Point? origin = null)
    {
        Require(baseWidth, nameof(baseWidth));
        Require(narrowWidth, nameof(narrowWidth));
        Require(height, nameof(height));
        if (narrowWidth >= baseWidth)
            throw new ArgumentException("Узкое основание должно быть меньше широкого", nameof(narrowWidth));

        double inset = (baseWidth - narrowWidth) / 2.0;

        // Исходное положение: узкое основание сверху
        var pts = new List<Point>
        {
            new(inset, 0), new(inset + narrowWidth, 0),
            new(baseWidth, height), new(0, height)
        };

        // Поворот на четверти по часовой стрелке: (x, y) -> (-y, x)
        for (int turn = 0; turn < (int)narrowSide; turn++)
            for (int i = 0; i < pts.Count; i++)
                pts[i] = new Point(-pts[i].Y, pts[i].X);

        return Build(pts, origin);
    }

    /// <summary>Правильный многоугольник с длиной стороны sideLength, одна сторона горизонтальна снизу</summary>
    public static Contour RegularPolygon(int sides, double sideLength, Point? origin = null)
    {
        if (sides < 3) throw new ArgumentOutOfRangeException(nameof(sides), "Минимум 3 стороны.");
        Require(sideLength, nameof(sideLength));

        double radius = sideLength / (2.0 * Math.Sin(Math.PI / sides));
        double start = Math.PI / 2.0 - Math.PI / sides;   // нижняя сторона горизонтальна

        var pts = new List<Point>(sides);
        for (int i = 0; i < sides; i++)
        {
            double a = start + 2.0 * Math.PI * i / sides;
            pts.Add(new Point(radius * Math.Cos(a), radius * Math.Sin(a)));
        }
        return Build(pts, origin);
    }

    // ───────────────────────────── Овал / круг ─────────────────────────────

    public static IReadOnlyList<int> OvalBoardCounts { get; } = new[] { 2, 3, 4, 6 };

    private const double CircleTolerance = 1e-6;

    public static Contour Circle(double radius, int boards = 4, Point? origin = null) =>
        Oval(radius, radius, boards, origin);

    public static Contour Ellipse(double radiusX, double radiusY, int boards = 4, Point? origin = null) =>
        Oval(radiusX, radiusY, boards, origin);

    /// <summary>
    /// Овал из boards дуг (одна дуга — одна доска). Два радиуса: горизонтальный и вертикальный,
    /// габарит контура ровно 2·radiusX на 2·radiusY.
    /// Круг (радиусы равны): точный при 2, 3, 4 и 6 досках.
    /// Овал: четырёхцентровая схема (две дуги на торцах и две вдоль длинных сторон), стыки гладкие.
    /// При 6 досках длинные дуги делятся пополам. При 2 и 3 досках гладкий овал невозможен.
    /// </summary>
    public static Contour Oval(double radiusX, double radiusY, int boards, Point? origin = null)
    {
        Require(radiusX, nameof(radiusX));
        Require(radiusY, nameof(radiusY));
        if (!OvalBoardCounts.Contains(boards))
            throw new ArgumentOutOfRangeException(nameof(boards), "Количество досок: 2, 3, 4 или 6.");

        var (pts, bulges) = Math.Abs(radiusX - radiusY) < CircleTolerance
            ? CircleNodes(radiusX, boards)
            : FourCentreNodes(radiusX, radiusY, boards);

        var o = origin ?? new Point(0, 0);
        double dx = o.X + radiusX, dy = o.Y + radiusY;

        var result = new ContourVertex[boards];
        for (int i = 0; i < boards; i++)
            result[i] = new ContourVertex(
                new Point(Math.Round(pts[i].X + dx, 4), Math.Round(pts[i].Y + dy, 4)), bulges[i]);
        return Contour.CreateClosed(result);
    }

    /// <summary>
    /// Круг: n одинаковых дуг по 2π/n, bulge = tan(π / 2n). Центр в (0, 0), обход по часовой.
    /// Узлы: при 2 — слева и справа, при 3 и 6 — сверху, при 4 — по диагоналям.
    /// </summary>
    private static (Point[], double[]) CircleNodes(double radius, int n)
    {
        double t0 = n switch { 2 => 0.0, 4 => Math.PI / 4.0, _ => -Math.PI / 2.0 };
        double bulge = Math.Tan(Math.PI / (2.0 * n));

        var pts = new Point[n];
        var bulges = new double[n];
        for (int i = 0; i < n; i++)
        {
            double t = t0 + 2.0 * Math.PI * i / n;
            pts[i] = new Point(radius * Math.Cos(t), radius * Math.Sin(t));
            bulges[i] = bulge;
        }
        return (pts, bulges);
    }

    /// <summary>
    /// Четырёхцентровый овал. Для a ≥ b: на торцах дуги радиуса R (центры на большой оси),
    /// вдоль длинных сторон дуги радиуса r (центры на малой оси). Условие касания:
    /// (a − R)² + (r − b)² = (r − R)², поэтому r однозначно выводится из R, а стыки гладкие.
    /// Габарит точный: торцевая дуга доходит до x = a, длинная — до y = b.
    /// Если овал выше, чем шире, строим повёрнутым на 90°.
    /// </summary>
    private static (Point[], double[]) FourCentreNodes(double radiusX, double radiusY, int boards)
    {
        if (boards < 4)
            throw new ArgumentException(
                "При 2 и 3 досках возможен только круг. Выберите 4 или 6 досок либо сделайте радиусы равными.");

        bool tall = radiusY > radiusX;
        double a = tall ? radiusY : radiusX;   // большая полуось
        double b = tall ? radiusX : radiusY;   // малая полуось

        double R = BestEndRadius(a, b);
        double r = ConnectRadius(a, b, R);
        double phi = Math.Atan2(r - b, a - R);   // половина угла торцевой дуги

        // Узел стыка в первой четверти
        double px = (a - R) * r / (r - R);
        double py = R * (r - b) / (r - R);

        // Торцевая дуга: угол 2φ. Длинная дуга: угол π − 2φ (при 6 досках делится пополам)
        double endBulge = Math.Tan(phi / 2.0);
        double longBulge = Math.Tan((Math.PI - 2.0 * phi) / (boards == 6 ? 8.0 : 4.0));

        var pts = new List<Point>(boards);
        var bulges = new List<double>(boards);
        void Add(double x, double y, double bulge)
        {
            pts.Add(new Point(x, y));
            bulges.Add(bulge);
        }

        // Обход по часовой на экране (Y вниз). Bulge вершины относится к дуге до следующей вершины.
        Add(px, -py, endBulge);              // правая торцевая дуга → (px, py)
        Add(px, py, longBulge);              // нижняя длинная дуга
        if (boards == 6) Add(0, b, longBulge);
        Add(-px, py, endBulge);              // левая торцевая дуга
        Add(-px, -py, longBulge);            // верхняя длинная дуга
        if (boards == 6) Add(0, -b, longBulge);

        var ptsArr = pts.ToArray();
        if (tall)
            for (int i = 0; i < ptsArr.Length; i++)
                ptsArr[i] = new Point(-ptsArr[i].Y, ptsArr[i].X);   // поворот на 90°

        return (ptsArr, bulges.ToArray());
    }

    /// <summary>Радиус длинной дуги из условия касания с торцевой дугой радиуса R.</summary>
    private static double ConnectRadius(double a, double b, double R) =>
        ((a - R) * (a - R) + b * b - R * R) / (2.0 * (b - R));

    /// <summary>
    /// Радиус торцевой дуги R (0 &lt; R &lt; b), при котором максимальное радиальное отклонение
    /// от настоящего эллипса минимально. Грубый перебор, затем золотое сечение.
    /// </summary>
    private static double BestEndRadius(double a, double b)
    {
        const int steps = 48;
        double lo = 0.005 * b, hi = 0.98 * b;

        double best = lo, bestErr = double.MaxValue;
        for (int i = 0; i <= steps; i++)
        {
            double R = lo + (hi - lo) * i / steps;
            double e = MaxRelativeError(a, b, R);
            if (e < bestErr) { bestErr = e; best = R; }
        }

        double step = (hi - lo) / steps;
        double x1 = Math.Max(lo, best - step), x2 = Math.Min(hi, best + step);
        const double g = 0.6180339887498949;

        double c = x2 - g * (x2 - x1), d = x1 + g * (x2 - x1);
        double fc = MaxRelativeError(a, b, c), fd = MaxRelativeError(a, b, d);
        for (int i = 0; i < 40; i++)
        {
            if (fc < fd)
            {
                x2 = d; d = c; fd = fc;
                c = x2 - g * (x2 - x1); fc = MaxRelativeError(a, b, c);
            }
            else
            {
                x1 = c; c = d; fc = fd;
                d = x1 + g * (x2 - x1); fd = MaxRelativeError(a, b, d);
            }
        }
        return (x1 + x2) / 2.0;
    }

    /// <summary>
    /// Максимальное относительное отклонение дуг (первая четверть) от эллипса
    /// по длине луча из центра: |ρ_дуги / ρ_эллипса − 1|.
    /// </summary>
    private static double MaxRelativeError(double a, double b, double R)
    {
        double r = ConnectRadius(a, b, R);
        double phi = Math.Atan2(r - b, a - R);
        const int samples = 24;
        double worst = 0;

        for (int i = 0; i <= samples; i++)
        {
            // торцевая дуга: центр (a − R, 0), углы 0..φ
            double s1 = phi * i / samples;
            worst = Math.Max(worst, RayError(a, b, a - R + R * Math.Cos(s1), R * Math.Sin(s1)));

            // длинная дуга: центр (0, b − r), углы φ..π/2
            double s2 = phi + (Math.PI / 2.0 - phi) * i / samples;
            worst = Math.Max(worst, RayError(a, b, r * Math.Cos(s2), b - r + r * Math.Sin(s2)));
        }
        return worst;
    }

    private static double RayError(double a, double b, double x, double y)
    {
        double rho = Math.Sqrt(x * x + y * y);
        if (rho < 1e-12) return 0;
        double c = x / rho, s = y / rho;
        double ellipse = 1.0 / Math.Sqrt(c * c / (a * a) + s * s / (b * b));
        return Math.Abs(rho / ellipse - 1.0);
    }

    /// <summary>Сдвигает точки так, чтобы левый верхний угол габарита оказался в origin</summary>
    private static Contour Build(IReadOnlyList<Point> pts, Point? origin)
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        foreach (var p in pts)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
        }

        var o = origin ?? new Point(0, 0);
        var verts = new ContourVertex[pts.Count];
        for (int i = 0; i < pts.Count; i++)
        {
            verts[i] = new ContourVertex(new Point(
                Math.Round(pts[i].X - minX + o.X, 4),
                Math.Round(pts[i].Y - minY + o.Y, 4)));
        }
        return Contour.CreateClosed(verts);
    }

    private static void Require(double value, string name)
    {
        if (!(value > 0) || double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(name, "Значение должно быть больше нуля");
    }
}