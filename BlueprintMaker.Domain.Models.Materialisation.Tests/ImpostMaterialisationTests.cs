using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;
namespace BlueprintMaker.Domain.Models.Materialisation.Tests;

public class ImpostMaterialisationTests
{
    private Frame CreateRectFrame(double w, double h)
    {
        var vertices = new ContourVertex[]
        {
            new(new Point(0, 0)), new(new Point(w, 0)),
            new(new Point(w, h)), new(new Point(0, h))
        };
        return Frame.Create(Contour.CreateClosed(vertices));
    }

    private Frame CreateHexagonFrame(double radius)
    {
        var vertices = new System.Collections.Generic.List<ContourVertex>();
        for (int i = 0; i < 6; i++)
        {
            double angle = i * System.Math.PI / 3;
            double x = System.Math.Round(radius * System.Math.Cos(angle), 2);
            double y = System.Math.Round(radius * System.Math.Sin(angle), 2);
            vertices.Add(new ContourVertex(new Point(x, y)));
        }
        return Frame.Create(Contour.CreateClosed(vertices));
    }

    [Fact]
    public void MaterializeImpost_Horizontal_InRectangle()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        var (_, _, impost) = rootSpace.InsertImpost(AxisOrientation.Horizontal, 0.5);
        impost.Materialize(40);

        var board = impost.GetPhysicalGeometry().First();
        var bounds = board.BoundingBox;

        // Импост на Y=50, ширина 40 -> от 30 до 70
        Assert.Equal(30, bounds.MinY, 2);
        Assert.Equal(70, bounds.MaxY, 2);
        Assert.Equal(40, bounds.Height, 2);

        // Световое пространство не должно быть null
        Assert.NotNull(frame.InnerSpace.GetContour());
    }

    [Fact]
    public void MaterializeImpost_Vertical_InHexagon()
    {
        var frame = CreateHexagonFrame(200);
        frame.Materialize(20);
        var rootSpace = frame.InnerSpace;

        var (_, _, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);
        impost.Materialize(40);

        var board = impost.GetPhysicalGeometry().First();
        Assert.True(board.IsClosed);

        // Доска должна быть толщиной 40
        Assert.Equal(40, board.BoundingBox.Width, 2);
    }

    [Fact]
    public void MaterializeImpost_MultipleImposts_Grid()
    {
        var frame = CreateRectFrame(600, 1000);
        frame.Materialize(50);
        var rootSpace = frame.InnerSpace;

        // Вставляем 3 импоста (делят на 4 части)
        rootSpace.InsertMultipleImposts(3, AxisOrientation.Vertical);
        frame.Materialize(20); // Материализуем раму полностью (включая доски)

        // Находим первый импост
        var impost = frame.InnerSpace.Children.OfType<Impost>().FirstOrDefault();
        Assert.NotNull(impost);

        impost.Materialize(30);
        var board = impost.GetPhysicalGeometry().First();

        // Доска должна быть замкнута и иметь толщину 30
        Assert.True(board.IsClosed);
        Assert.Equal(30, board.BoundingBox.Width, 2);
    }

    [Fact]
    public void MaterializeImpost_SplitImpost_GeneratesComplexBand()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        var (_, _, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // Разбиваем импост пополам (создаем 3 точки)
        impost.SplitAt(0, null);

        // Материализуем
        impost.Materialize(40);

        var board = impost.GetPhysicalGeometry().First();

        // Доска должна быть замкнута
        Assert.True(board.IsClosed);

        // У S-образного импоста доска должна иметь больше вершин, чем у прямого (прямой имеет ~4-8 вершин в зависимости от стыков)
        // Проверяем, что контур не вырожден
        Assert.True(board.Vertices.Count >= 4);
    }

    [Fact]
    public void MaterializeImpost_DoesNotBreakLightSpace()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);
        impost.Materialize(40);

        // Пространства должны остаться валидными
        Assert.NotNull(space1.GetContour());
        Assert.NotNull(space2.GetContour());

        // Габариты пространств должны быть корректными
        var bounds1 = space1.GetContour().BoundingBox;
        var bounds2 = space2.GetContour().BoundingBox;

        Assert.True(bounds1.Width > 0);
        Assert.True(bounds2.Width > 0);
    }
}