namespace BlueprintMaker.Core.Geometry.Tests;

public class ContourTests
{
    private static ContourVertex V(double x, double y, double bulge = 0) =>
        new(new Point(x, y), bulge);

    [Fact]
    public void CreateClosed_LessThan3Vertices_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Contour.CreateClosed(new[] { V(0, 0), V(1, 1) }));
    }

    [Fact]
    public void CreateOpen_LessThan2Vertices_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Contour.CreateOpen(new[] { V(0, 0) }));
    }

    [Fact]
    public void BoundingBox_ClosedRectangle_CalculatedCorrectly()
    {
        var contour = Contour.CreateClosed(new[]
        {
            V(0, 0), V(10, 0), V(10, 5), V(0, 5)
        });

        Assert.Equal(0, contour.BoundingBox.MinX);
        Assert.Equal(10, contour.BoundingBox.MaxX);
        Assert.Equal(5, contour.BoundingBox.MaxY);
    }

    [Fact]
    public void BoundingBox_OpenPolyline_CalculatedCorrectly()
    {
        var contour = Contour.CreateOpen(new[]
        {
            V(0, 0), V(10, 10), V(20, 5)
        });

        Assert.Equal(0, contour.BoundingBox.MinX);
        Assert.Equal(20, contour.BoundingBox.MaxX);
        Assert.Equal(10, contour.BoundingBox.MaxY);
    }
}