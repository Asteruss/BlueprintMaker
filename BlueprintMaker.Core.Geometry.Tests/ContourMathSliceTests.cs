namespace BlueprintMaker.Core.Geometry.Tests;

public class ContourMathSliceTests
{
    private static ContourVertex V(double x, double y, double bulge = 0) => new(new Point(x, y), bulge);

    [Fact]
    public void SliceContour_VerticalCut_ProducesTwoRectangles()
    {
        // Прямоугольник 100x100
        var parentVertices = new[] { V(0, 0), V(100, 0), V(100, 100), V(0, 100) };
        var parent = Contour.CreateClosed(parentVertices);

        // Вертикальный разрез по центру
        var cutter = Contour.CreateOpen(new[] { V(50, 0), V(50, 100) });

        var (first, second) = ContourMath.SliceContour(parent, cutter);

        // Проверяем габариты половинок
        Assert.Equal(50, first.BoundingBox.Width, 2);
        Assert.Equal(50, second.BoundingBox.Width, 2);

        // Проверяем, что у половинок по 4 вершины (2 угла рамы + 2 точки реза)
        Assert.Equal(4, first.Vertices.Count);
        Assert.Equal(4, second.Vertices.Count);
    }

    [Fact]
    public void SliceContour_HorizontalCut_ProducesTwoCorrectHalves()
    {
        // Прямоугольник 100x100
        var parentVertices = new[] { V(0, 0), V(100, 0), V(100, 100), V(0, 100) };
        var parent = Contour.CreateClosed(parentVertices);

        // Горизонтальный разрез по центру
        var cutter = Contour.CreateOpen(new[] { V(0, 50), V(100, 50) });

        var (first, second) = ContourMath.SliceContour(parent, cutter);

        // Нижняя половина должна иметь высоту 50, верхняя тоже 50
        Assert.Equal(50, first.BoundingBox.Height, 2);
        Assert.Equal(50, second.BoundingBox.Height, 2);
    }
}