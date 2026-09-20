
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Tests;

public class FrameInteractionTests
{
    private Frame CreateRectFrame(double w, double h)
    {
        var vertices = new ContourVertex[]
        {
            new(new Point(0, 0)),
            new(new Point(w, 0)),
            new(new Point(w, h)),
            new(new Point(0, h))
        };
        return Frame.Create(Contour.CreateClosed(vertices));
    }

    [Fact]
    public void Frame_SplitAt_AddsNewNode()
    {
        var frame = CreateRectFrame(100, 100);

        // Изначально у рамы 4 угла
        Assert.Equal(4, frame.GetContour().Vertices.Count);

        // Разбиваем нижнюю грань (сегмент 0) без указания точки (по середине)
        frame.SplitAt(0);

        // Должно стать 5 вершин
        Assert.Equal(5, frame.GetContour().Vertices.Count);

        // Проверяем, что новая точка встала посередине нижней грани (50, 0)
        Assert.Equal(new Point(50, 0), frame.GetContour().Vertices[1].Position);
    }

    [Fact]
    public void Frame_MoveNode_ChangesBounds()
    {
        var frame = CreateRectFrame(100, 100);

        // Двигаем первый угол (0,0) в новую точку (20, -20)
        frame.MoveNode(0, new Point(20, -20));

        var bounds = frame.GetContour().BoundingBox;

        // Проверяем, что габариты рамы изменились по Y (стали от -20 до 100)
        Assert.Equal(-20, bounds.MinY, 2);
        Assert.Equal(100, bounds.MaxY, 2);

        // MinX останется 0, так как левый нижний угол (0, 100) никуда не делся
        Assert.Equal(0, bounds.MinX, 2);
    }

    [Fact]
    public void Frame_SetSegmentArc_CreatesArcAndUpdatesBounds()
    {
        var frame = CreateRectFrame(100, 100);

        // Разбиваем нижнюю грань, чтобы сделать из сегмента арку
        // Сегмент 0 идет от (0,0) до (100,0). Сделаем дугу с Bulge = 1.0 (полукруг вверх)
        frame.SetSegmentArc(0, 1.0);

        var vertices = frame.GetContour().Vertices;

        // Проверяем, что первый сегмент стал дугой
        Assert.True(vertices[0].IsArcStart);
        Assert.Equal(1.0, vertices[0].Bulge, 2);

        // Габариты по Y должны были вырасти, так как дуга выгнулась вверх за пределы (0,0)
        var bounds = frame.GetContour().BoundingBox;
        Assert.True(bounds.MinY < 0, "Дуга должна выгнуться наружу (вниз по Y, так как контур по часовой)");
        // По правилу SignedArea контур по часовой, значит внутренняя нормаль смотрит вверх.
        // Bulge = 1.0 выгибает дугу влево от направления обхода, то есть вверх (наружу).
        // Так что MaxY должен стать больше 100? Нет, дуга на нижней грани.
        // Направление: (0,0) -> (100,0). Лево от вектора (0,0)->(100,0) это -Y.
        // Bulge = 1.0 выгибает влево, значит вниз (MinY < 0).
        Assert.True(bounds.MinY < 0, "Нижняя грань должна выгнуться наружу (MinY < 0)");
    }
}
