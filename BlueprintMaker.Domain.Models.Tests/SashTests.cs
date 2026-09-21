using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Tests;

public class SashTests
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
    public void InsertSash_CopiesParentGeometryAndCreatesInnerSpace()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10); // Внутреннее пространство 80x80 (от 10,10 до 90,90)
        var rootSpace = frame.InnerSpace;

        // Вставляем створку
        var sash = rootSpace.InsertSash();

        // Створка должна стать ребенком пространства
        Assert.Single(rootSpace.Children);
        Assert.Equal(sash, rootSpace.Children[0]);

        // Габариты створки должны совпадать с габаритами пространства (80x80)
        var sashBounds = sash.GetContour().BoundingBox;
        Assert.Equal(10, sashBounds.MinX, 2);
        Assert.Equal(10, sashBounds.MinY, 2);
        Assert.Equal(80, sashBounds.Width, 2);
        Assert.Equal(80, sashBounds.Height, 2);

        // Внутри створки должно появиться новое световое пространство
        Assert.NotNull(sash.InnerSpace);
        Assert.Empty(sash.InnerSpace.Children); // Оно пустое
    }

    [Fact]
    public void InsertSash_InnerSpaceGeometryUpdatesWithMaterialization()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // Вставляем створку и сразу материализуем её с шириной бруса 5
        var sash = rootSpace.InsertSash();
        sash.Materialize(5);

        // Внутреннее пространство створки должно стать 70x70 
        // (отступ по 5 от габаритов створки 80x80)
        var innerBounds = sash.InnerSpace.GetContour().BoundingBox;
        Assert.Equal(15, innerBounds.MinX, 2);
        Assert.Equal(15, innerBounds.MinY, 2);
        Assert.Equal(70, innerBounds.Width, 2);
        Assert.Equal(70, innerBounds.Height, 2);
    }
}
