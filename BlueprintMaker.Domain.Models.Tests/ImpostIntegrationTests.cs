using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Tests;

public class ImpostIntegrationTests
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
    public void InsertImpost_SplitSpacesHaveCorrectBounds()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical);

        // space1 леваяполовина: от X=10 до X=50
        var space1Bounds = space1.GetContour().BoundingBox;
        Assert.Equal(10, space1Bounds.MinX, 2);
        Assert.Equal(50, space1Bounds.MaxX, 2);
        Assert.Equal(40, space1Bounds.Width, 2);

        // space2 правая половина: от X=50 до X=90
        var space2Bounds = space2.GetContour().BoundingBox;
        Assert.Equal(50, space2Bounds.MinX, 2);
        Assert.Equal(90, space2Bounds.MaxX, 2);
        Assert.Equal(40, space2Bounds.Width, 2);
    }

    [Fact]
    public void InsertImpost_OffCenterSplit_CalculatesCorrectBounds()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // Вставляем импост на 25% от левого края (RelativePosition = 0.25)
        // Внутреннее пространство от 10 до 90. 25% от него: 10 + 80*0.25 = 30.
        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.25);

        var impostBounds = impost.GetContour().BoundingBox;
        Assert.Equal(30, impostBounds.MinX, 2); // Импост на X=30

        // space1 - левая часть: от 10 до 30 (ширина 20)
        Assert.Equal(20, space1.GetContour().BoundingBox.Width, 2);
        Assert.Equal(10, space1.GetContour().BoundingBox.MinX, 2);

        // space2 - правая часть: от 30 до 90 (ширина 60)
        // space2 - правая часть: от 30 до 90 (ширина 60)
        Assert.Equal(60, space2.GetContour().BoundingBox.Width, 2);
        Assert.Equal(30, space2.GetContour().BoundingBox.MinX, 2);
    }


    [Fact]
    public void MovingImpost_UpdatesSplitSpacesGeometry()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // 1. Вставляем на 25% (X=30). Левая (space1) = 20, Правая (space2) = 60.
        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.25);

        // Проверяем начальное состояние
        Assert.Equal(20, space1.GetContour().BoundingBox.Width, 2);
        Assert.Equal(60, space2.GetContour().BoundingBox.Width, 2);

        // 2. двигаем на 75% (X=70). 
        // Благодаря Pull-модели пространства должны пересчитаться автоматически!
        impost.UpdatePosition(0.75);

        // Проверяем, что габариты изменились. Левая стала 60, Правая стала 20.
        Assert.Equal(60, space1.GetContour().BoundingBox.Width, 2);
        Assert.Equal(10, space1.GetContour().BoundingBox.MinX, 2); // От 10 до 70

        Assert.Equal(20, space2.GetContour().BoundingBox.Width, 2);
        Assert.Equal(70, space2.GetContour().BoundingBox.MinX, 2); // От 70 до 90
    }
}