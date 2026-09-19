namespace BlueprintMaker.Core.Geometry.Tests;

public class SegmentProjectionTests
{
    [Fact]
    public void Project_PointOnStraightLine_ReturnsZeroDistance()
    {
        var from = new Point(0, 0);
        var to = new Point(10, 0);
        var test = new Point(5, 0);
        double bulge = 0;

        var result = SegmentProjection.Project(from, to, bulge, test);

        Assert.Equal(0, result.Distance, 5);
        Assert.Equal(0.5, result.Parameter, 5);
        Assert.Equal(new Point(5, 0), result.ClosestPoint);
    }

    [Fact]
    public void Project_PointAboveStraightLine_ReturnsCorrectDistanceAndParam()
    {
        var from = new Point(0, 0);
        var to = new Point(10, 0);
        var test = new Point(5, 5);
        double bulge = 0;

        var result = SegmentProjection.Project(from, to, bulge, test);

        Assert.Equal(5, result.Distance, 5);
        Assert.Equal(0.5, result.Parameter, 5);
    }

    [Fact]
    public void Project_PointOutsideSegment_ReturnsNearestEnd()
    {
        var from = new Point(0, 0);
        var to = new Point(10, 0);
        var test = new Point(15, 0);
        double bulge = 0;

        var result = SegmentProjection.Project(from, to, bulge, test);

        Assert.Equal(5, result.Distance, 5);
        Assert.Equal(1, result.Parameter, 5); // Должен вернуть конец отрезка
        Assert.Equal(to, result.ClosestPoint);
    }

    [Fact]
    public void Project_PointOnArc_ReturnsZeroDistance()
    {
        // Полукруг радиусом 5, центр (0,0). От (5,0) до (-5,0), Bulge = 1
        var from = new Point(5, 0);
        var to = new Point(-5, 0);
        double bulge = 1.0;

        // Точка ровно на вершине дуги: (0, 5)
        var test = new Point(0, 5);

        var result = SegmentProjection.Project(from, to, bulge, test);

        Assert.Equal(0, result.Distance, 2);
        Assert.True(result.Parameter > 0.4 && result.Parameter < 0.6); // Где-то посередине дуги
    }

    [Fact]
    public void Project_PointOutsideArcSweep_ReturnsNearestEnd()
    {
        // Полукруг радиусом 5, центр (0,0). От (5,0) до (-5,0), Bulge = 1
        var from = new Point(5, 0);
        var to = new Point(-5, 0);
        double bulge = 1.0;

        // Точка снизу (вне дуги, дуга выгнута вверх)
        var test = new Point(0, -5);

        var result = SegmentProjection.Project(from, to, bulge, test);

        // Должна вернуть ближайший конец (либо from, либо to)
        Assert.True(result.Parameter == 0 || result.Parameter == 1);

        // Расстояние до концов дуги: sqrt(5^2 + 5^2) = sqrt(50) = 7.071
        Assert.Equal(Math.Sqrt(50), result.Distance, 2);
    }
}