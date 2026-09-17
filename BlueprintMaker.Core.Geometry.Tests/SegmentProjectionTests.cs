namespace BlueprintMaker.Core.Geometry.Tests;

public class SegmentProjectionTests
{
    [Fact]
    public void DistanceToSegment_PointOnLine_ReturnsZero()
    {
        var from = new Point(0, 0);
        var to = new Point(10, 0);
        var test = new Point(5, 0);
        double bulge = 0;

        var dist = SegmentProjection.DistanceToSegment(from, to, bulge, test);

        Assert.Equal(0, dist, 5);
    }

    [Fact]
    public void DistanceToSegment_PointAboveLine_Returns5()
    {
        var from = new Point(0, 0);
        var to = new Point(10, 0);
        var test = new Point(5, 5); // Точка на 5 единиц выше линии
        double bulge = 0;

        var dist = SegmentProjection.DistanceToSegment(from, to, bulge, test);

        Assert.Equal(5, dist, 5);
    }

    [Fact]
    public void DistanceToSegment_PointOutsideSegment_ReturnsDistanceToNearestEnd()
    {
        var from = new Point(0, 0);
        var to = new Point(10, 0);
        var test = new Point(15, 0); // Точка за пределами отрезка
        double bulge = 0;

        var dist = SegmentProjection.DistanceToSegment(from, to, bulge, test);

        // Должно вернуть расстояние до точки End (10,0), то есть 5
        Assert.Equal(5, dist, 5);
    }
}