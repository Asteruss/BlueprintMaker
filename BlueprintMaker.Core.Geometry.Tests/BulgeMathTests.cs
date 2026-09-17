namespace BlueprintMaker.Core.Geometry.Tests;

public class BulgeMathTests
{
    [Fact]
    public void BulgeFromThreePoints_Collinear_ReturnsZero()
    {
        var start = new Point(0, 0);
        var control = new Point(5, 0);
        var end = new Point(10, 0);

        var bulge = BulgeMath.BulgeFromThreePoints(start, control, end);

        Assert.Equal(0, bulge, 5);
    }

    [Fact]
    public void BulgeFromThreePoints_Semicircle_Returns1()
    {
        // Прямая от 0,0 до 10,0. Контрольная точка в 5,5 (ровно полукруг)
        var start = new Point(0, 0);
        var control = new Point(5, 5);
        var end = new Point(10, 0);

        var bulge = BulgeMath.BulgeFromThreePoints(start, control, end);

        Assert.Equal(1, bulge, 2); // tan(45) = 1, допускаем погрешность вычислений
    }

    [Fact]
    public void BulgeFromThreePoints_ControlBelow_ReturnsNegative()
    {
        var start = new Point(0, 0);
        var control = new Point(5, -5);
        var end = new Point(10, 0);

        var bulge = BulgeMath.BulgeFromThreePoints(start, control, end);

        Assert.True(bulge < 0, "Bulge должен быть отрицательным для дуги вниз");
    }

    [Fact]
    public void GetArcParameters_Semicircle_RadiusMatchesHalfChord()
    {
        var start = new Point(-1, 0);
        var end = new Point(1, 0);
        double bulge = 1.0; // Полукруг

        var arc = BulgeMath.GetArcParameters(start, end, bulge);

        Assert.Equal(1, arc.Radius, 2);
        Assert.Equal(new Point(0, 0), arc.Center);
    }
}