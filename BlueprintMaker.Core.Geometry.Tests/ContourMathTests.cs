namespace BlueprintMaker.Core.Geometry.Tests;

public class ContourMathTests
{
    private static ContourVertex V(double x, double y, double bulge = 0) =>
        new(new Point(x, y), bulge);

    [Fact]
    public void Inset_RectangleShrinksByWidth()
    {
        // Квадрат 100x100
        var vertices = new[] { V(0, 0), V(100, 0), V(100, 100), V(0, 100) };
        double width = 10;

        var insetVertices = ContourMath.Inset(vertices, width);

        // Проверяем, что новый квадрат стал 80x80 и сместился на 10 по осям
        Assert.Equal(4, insetVertices.Count);

        // Нижняя левая точка должна стать (10, 10) или близко к ней
        // (Точное положение зависит от тригонометрии усредненной нормали, но для прямоугольника это точно 10,10)
        Assert.Equal(10, insetVertices[0].Position.X, 1);
        Assert.Equal(10, insetVertices[0].Position.Y, 1);

        // Проверяем, что габариты уменьшились
        var bounds = ContourMath.CalculateBounds(insetVertices, true);
        Assert.Equal(80, bounds.Width, 1);
        Assert.Equal(80, bounds.Height, 1);
    }

    [Fact]
    public void CalculateBounds_TopSemicircle_IncludesArcPeak()
    {
        // Прямоугольник 10x5 с полукруглой аркой сверху между (10,5) и (0,5)
        // Хорда длиной 10, полукруг -> радиус 5, вершина на высоте 5(база) + 5(радиус) = 10
        var vertices = new[]
        {
            V(0, 0),
            V(10, 0),
            V(10, 5, 1.0), // Bulge = 1.0 означает полукруг
            V(0, 5)
        };

        var bounds = ContourMath.CalculateBounds(vertices, true);

        Assert.Equal(10, bounds.MaxY, 1); // Высота должна быть 10
    }

    [Fact]
    public void CalculateBounds_ArchWindow_IncludesArcPeak()
    {
        // Окно: прямоугольное основание 100x50, а верхняя грань — полукруг.
        // Хорда от (100,50) до (0,50). Длина хорды 100 -> радиус полукруга 50.
        // Значит, вершина дуги должна быть на высоте 50 (база) + 50 (радиус) = 100.
        var vertices = new[]
        {
            V(0, 0),
            V(100, 0),
            V(100, 50, 1.0), // Bulge = 1.0 означает ровно полукруг
            V(0, 50)
        };

        var bounds = ContourMath.CalculateBounds(vertices, isClosed: true);

        Assert.Equal(0, bounds.MinX);
        Assert.Equal(100, bounds.MaxX);
        Assert.Equal(0, bounds.MinY);
        Assert.Equal(100, bounds.MaxY); // Проверяем, что дуга подняла габариты до 100
    }

    [Fact]
    public void Inset_Trapezoid_ShrinksBoundsAndPreservesVerticesCount()
    {
        // Трапеция: нижнее основание 100, верхнее основание 60, высота 100.
        var vertices = new[]
        {
            V(0, 0),
            V(100, 0),
            V(80, 100),
            V(20, 100)
        };
        double width = 10;

        var insetVertices = ContourMath.Inset(vertices, width);
        var bounds = ContourMath.CalculateBounds(insetVertices, isClosed: true);

        // Точек должно остаться столько же (4 угла трапеции)
        Assert.Equal(4, insetVertices.Count);

        // Габариты должны гарантированно уменьшиться
        Assert.True(bounds.Width < 100, "Ширина трапеции после отступа должна стать меньше 100");
        Assert.True(bounds.Height < 100, "Высота трапеции после отступа должна стать меньше 100");

        // Нижняя левая точка должна сместиться вправо (внутрь) больше чем на 10, 
        // так как угол трапеции острый (меньше 90 градусов), и пересечение линий уходит дальше.
        var bottomLeft = insetVertices.First();
        Assert.True(bottomLeft.Position.X > 10, "Острый угол должен срезаться сильнее внутрь");
    }

    [Fact]
    public void Inset_ArchWindow_PreservesArcBulgeAndShrinks()
    {
        // То же арочное окно (прямоугольник + полукруг)
        var vertices = new[]
        {
            V(0, 0),
            V(100, 0),
            V(100, 50, 1.0), // Полукруг
            V(0, 50)
        };
        double width = 10;

        var insetVertices = ContourMath.Inset(vertices, width);

        // Проверяем, что кривизна (Bulge) сохранилась на нужной вершине
        // В исходном массиве Bulge был на индексе 2. После Inset порядок вершин не меняется.
        Assert.Equal(1.0, insetVertices[2].Bulge, 2);

        // Габариты должны уменьшиться (100 - 2*10 = 80 ширина)
        var bounds = ContourMath.CalculateBounds(insetVertices, isClosed: true);
        Assert.Equal(80, bounds.Width, 1);
    }
}