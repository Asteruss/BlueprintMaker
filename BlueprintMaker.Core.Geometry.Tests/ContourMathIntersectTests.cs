namespace BlueprintMaker.Core.Geometry.Tests;

public class ContourMathIntersectTests
{
    private static ContourVertex V(double x, double y, double bulge = 0) => new(new Point(x, y), bulge);

    [Fact]
    public void IntersectWithLine_VerticalThroughRectangle_Returns2Points()
    {
        var vertices = new[] { V(0, 0), V(100, 0), V(100, 100), V(0, 100) };
        var contour = Contour.CreateClosed(vertices);

        var intersections = ContourMath.IntersectWithLine(contour.Vertices, true, AxisOrientation.Vertical, 50);

        Assert.Equal(2, intersections.Count);
        Assert.Contains(new Point(50, 0), intersections);
        Assert.Contains(new Point(50, 100), intersections);
    }

    [Fact]
    public void IntersectWithLine_VerticalThroughCircle_Returns2Points()
    {
        // Круг радиус 50, центр (0,0). 4 дуги (Bulge = 0.4142)
        var vertices = new[]
        {
            V(50, 0, 0.4142),
            V(0, 50, 0.4142),
            V(-50, 0, 0.4142),
            V(0, -50, 0.4142)
        };
        var contour = Contour.CreateClosed(vertices);

        var intersections = ContourMath.IntersectWithLine(contour.Vertices, true, AxisOrientation.Vertical, 0);

        Assert.Equal(2, intersections.Count);
        Assert.Contains(new Point(0, 50), intersections);
        Assert.Contains(new Point(0, -50), intersections);
    }

    [Fact]
    public void IntersectWithLine_OutsideBounds_Returns0Points()
    {
        var vertices = new[] { V(0, 0), V(100, 0), V(100, 100), V(0, 100) };
        var contour = Contour.CreateClosed(vertices);

        var intersections = ContourMath.IntersectWithLine(contour.Vertices, true, AxisOrientation.Vertical, 150);

        Assert.Empty(intersections);
    }
}