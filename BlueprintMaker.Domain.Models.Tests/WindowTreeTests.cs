using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Tests;

public class WindowTreeTests
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
    public void InsertImpost_CreatesValidTreeStructure()
    {
        var frame = CreateRectFrame(100, 100);
        var lightSpace = frame.InnerSpace;

        // Вставляем 1 вертикальный импост
        var (space1, space2, impost) = lightSpace.InsertImpost(AxisOrientation.Vertical);

        // LightSpace теперь содержит РОВНО 3 детей: Импост и 2 новых пространства
        Assert.Equal(3, lightSpace.Children.Count);

        Assert.Equal(impost, lightSpace.Children[0]);
        Assert.Equal(space1, lightSpace.Children[1]);
        Assert.Equal(space2, lightSpace.Children[2]);

        // У новых пространств нет своих детей (они пустые листы)
        Assert.Empty(space1.Children);
        Assert.Empty(space2.Children);
    }

    [Fact]
    public void InsertImpost_NestedImposts_CreatesGridStructure()
    {
        var frame = CreateRectFrame(100, 100);
        var rootSpace = frame.InnerSpace;

        // 1. Вертикальный импост (делит rootSpace на лево/право)
        var (space1, space2, vImpost) = rootSpace.InsertImpost(AxisOrientation.Vertical);

        // 2. Горизонтальный импост внутри левого пространства (space1)
        var (space1a, space1b, hImpost) = space1.InsertImpost(AxisOrientation.Horizontal);

        // Проверяем, что space1 теперь содержит 3 детей (hImpost, space1, space2)
        Assert.Equal(3, space1.Children.Count);
        Assert.Equal(hImpost, space1.Children[0]);
        Assert.Equal(space1a, space1.Children[1]);
        Assert.Equal(space1b, space1.Children[2]);

        // space2 остается нетронутым (пустым листом)
        Assert.Empty(space2.Children);
    }
    [Fact]
    public void Materialize_UpdatesLightSpaceGeometry()
    {
        var frame = CreateRectFrame(100, 100);
        var rootSpace = frame.InnerSpace;

        // До материализации (ширина 1) световое пространство должно быть 98x98
        var boundsBefore = rootSpace.GetContour().BoundingBox;
        Assert.Equal(98, boundsBefore.Width, 2);
        Assert.Equal(98, boundsBefore.Height, 2);

        // Материализуем раму с шириной 10
        frame.Materialize(10);

        // После материализации световое пространство должно стать 80x80
        var boundsAfter = rootSpace.GetContour().BoundingBox;
        Assert.Equal(80, boundsAfter.Width, 2);
        Assert.Equal(80, boundsAfter.Height, 2);
    }

    [Fact]
    public void InsertImpost_ImpostGeometryMatchesLightSpace()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10); // Световое пространство стало 80x80, отступы по 10

        var rootSpace = frame.InnerSpace;
        var (_, _, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical);

        var impostBounds = impost.GetContour().BoundingBox;

        // Импост должен стоять по центру (x=50) и идти от 10 до 90 по Y
        Assert.Equal(50, impostBounds.MinX, 2);
        Assert.Equal(10, impostBounds.MinY, 2);
        Assert.Equal(90, impostBounds.MaxY, 2);
    }
}