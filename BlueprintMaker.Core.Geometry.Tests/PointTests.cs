namespace BlueprintMaker.Core.Geometry.Tests;

public class PointTests
{
    [Fact]
    public void DistanceTo_345Triangle_Returns5()
    {
        var a = new Point(0, 0);
        var b = new Point(3, 4);

        Assert.Equal(5, a.DistanceTo(b), 5);
    }

    [Fact]
    public void Midpoint_ReturnsAverage()
    {
        var a = new Point(0, 0);
        var b = new Point(10, 20);

        var mid = a.Midpoint(b);

        Assert.Equal(new Point(5, 10), mid);
    }

    [Fact]
    public void Equals_WithinTolerance_True()
    {
        var a = new Point(1.0, 2.0);
        var b = new Point(1.00005, 2.0); // Разница меньше 0.0001

        Assert.True(a == b);
    }

    [Fact]
    public void Equals_OutsideTolerance_False()
    {
        var a = new Point(1.0, 2.0);
        var b = new Point(1.01, 2.0); // Разница больше 0.0001

        Assert.False(a == b);
    }
}