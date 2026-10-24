using System;
using System.Collections.Generic;

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

    public static Contour Circle(double radius, Point? origin = null)
        => Ellipse(radius, radius, origin);

    /// <param name="radiusX">Горизонтальный радиус</param>
    /// <param name="radiusY">Вертикальный радиус</param>
    /// <param name="segmentsPerQuarter">Дуг на четверть; 0 — подобрать по вытянутости</param>
    public static Contour Ellipse(double radiusX, double radiusY, Point? origin = null, int segmentsPerQuarter = 0)
    {
        Require(radiusX, nameof(radiusX));
        Require(radiusY, nameof(radiusY));

        bool circle = Math.Abs(radiusX - radiusY) < 1e-9;
        int perQuarter = circle ? 1
            : segmentsPerQuarter > 0 ? segmentsPerQuarter
            : AutoSegments(radiusX, radiusY);

        int n = perQuarter * 4;
        var o = origin ?? new Point(0, 0);
        double cx = o.X + radiusX, cy = o.Y + radiusY;

        Point P(double t) => new(cx + radiusX * Math.Cos(t), cy + radiusY * Math.Sin(t));

        var verts = new ContourVertex[n];
        for (int i = 0; i < n; i++)
        {
            double t0 = 2.0 * Math.PI * i / n;
            double t1 = 2.0 * Math.PI * (i + 1) / n;
            var p0 = P(t0);
            var pm = P((t0 + t1) / 2.0);
            var p1 = P(t1);
            verts[i] = new ContourVertex(p0, ArcBulge(p0, pm, p1));
        }
        return Contour.CreateClosed(verts);
    }

    private static int AutoSegments(double rx, double ry)
    {
        double ratio = Math.Max(rx, ry) / Math.Min(rx, ry);
        return Math.Clamp((int)Math.Ceiling(3.0 * Math.Sqrt(ratio)), 4, 16);
    }

    /// <summary>
    /// Bulge дуги, проходящей через p0, pm, p1 (pm лежит между ними на дуге)
    /// Угол дуги θ = 2(π − ∠p0·pm·p1), bulge = tan(θ/4); знак — по направлению обхода в координатах контура
    /// </summary>
    private static double ArcBulge(Point p0, Point pm, Point p1)
    {
        double ux = p0.X - pm.X, uy = p0.Y - pm.Y;
        double vx = p1.X - pm.X, vy = p1.Y - pm.Y;

        double angleAtMid = Math.Atan2(Math.Abs(ux * vy - uy * vx), ux * vx + uy * vy);
        double theta = 2.0 * (Math.PI - angleAtMid);

        double cross = (pm.X - p0.X) * (p1.Y - pm.Y) - (pm.Y - p0.Y) * (p1.X - pm.X);
        if (Math.Abs(cross) < 1e-12) return 0;

        return Math.Sign(cross) * Math.Tan(theta / 4.0);
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