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

    [Fact]
    public void MoveNode_FreezesRuleAndUpdatesGeometry()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // Изначально импост стоит от 10 до 90 по Y
        Assert.Equal(10, impost.GetContour().BoundingBox.MinY, 2);
        Assert.Equal(90, impost.GetContour().BoundingBox.MaxY, 2);

        // Двигаем нижний узел (индекс 0) вверх до Y=30
        impost.MoveNode(0, new Point(50, 30));

        // Импост должен был заморозиться и изменить габариты
        var bounds = impost.GetContour().BoundingBox;
        Assert.Equal(30, bounds.MinY, 2); // Теперь идет от 30
        Assert.Equal(90, bounds.MaxY, 2);

        // Убедимся, что у нас 2 вершины (форма не сломалась).
        Assert.Equal(2, impost.GetContour().Vertices.Count);
    }

    [Fact]
    public void SplitAt_AddsNewNodeToContour()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // Изначально у импоста 2 вершины (прямая линия)
        Assert.Equal(2, impost.GetContour().Vertices.Count);

        // Разбиваем импост посередине (в точке 50, 50)
        impost.SplitAt(0, new Point(50, 50));

        // Теперь должно быть 3 вершины
        Assert.Equal(3, impost.GetContour().Vertices.Count);

        // Проверяем, что средняя точка встала правильно
        Assert.Equal(new Point(50, 50), impost.GetContour().Vertices[1].Position);
    }

    [Fact]
    public void SetSegmentArc_CreatesS_ShapedImpost()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // Делаем первую половину дугой (Bulge = 0.5)
        impost.SplitAt(0, new Point(50, 50));
        impost.SetSegmentArc(0, 0.5); // Дуга на первом сегменте

        // Делаем вторую половину дугой в обратную сторону (Bulge = -0.5)
        impost.SetSegmentArc(1, -0.5); // Дуга на втором сегменте

        var vertices = impost.GetContour().Vertices;
        Assert.True(vertices[0].IsArcStart);
        Assert.True(vertices[1].IsArcStart);

        // Проверяем, что дуги выгнуты в разные стороны (знаки разные)
        Assert.True(vertices[0].Bulge > 0);
        Assert.True(vertices[1].Bulge < 0);
    }
    [Fact]
    public void SplitAt_WithoutPosition_DividesByMiddle()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // Вставляем вертикальный импост (от 10,10 до 10,90)
        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // Разбиваем первый сегмент без указания точки
        impost.SplitAt(0);

        var vertices = impost.GetContour().Vertices;
        Assert.Equal(3, vertices.Count);

        // Средняя точка должна быть ровно посередине (50, 50)
        Assert.Equal(new Point(50, 50), vertices[1].Position);
    }

    [Fact]
    public void MoveNode_AlongImpostLine_ChangesSegmentLengthsButPreservesShape()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // Вставляем вертикальный импост (от 10 до 90 по Y)
        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // Разбиваем импост ровно посередине (в точке 50, 50)
        impost.SplitAt(0, new Point(50, 50));

        // Проверяем начальные длины сегментов (от 10 до 50 = 40, от 50 до 90 = 40)
        var verticesBefore = impost.GetContour().Vertices;
        double seg1LenBefore = verticesBefore[0].Position.DistanceTo(verticesBefore[1].Position);
        double seg2LenBefore = verticesBefore[1].Position.DistanceTo(verticesBefore[2].Position);
        Assert.Equal(40, seg1LenBefore, 2);
        Assert.Equal(40, seg2LenBefore, 2);

        // двигаем узел вдоль линии импоста вниз до Y=70
        impost.MoveNode(1, new Point(50, 70));

        var verticesAfter = impost.GetContour().Vertices;

        // 1. Общая форма импоста не изменилась (габариты остались 10..90)
        var bounds = impost.GetContour().BoundingBox;
        Assert.Equal(10, bounds.MinY, 2);
        Assert.Equal(90, bounds.MaxY, 2);

        // 2. Координата среднего узла обновилась
        Assert.Equal(new Point(50, 70), verticesAfter[1].Position);

        // 3. Длины сегментов изменились
        // Сегмент 1 (от 10 до 70) должен стать длиной 60
        double seg1LenAfter = verticesAfter[0].Position.DistanceTo(verticesAfter[1].Position);
        Assert.Equal(60, seg1LenAfter, 2);

        // Сегмент 2 (от 70 до 90) должен стать длиной 20
        double seg2LenAfter = verticesAfter[1].Position.DistanceTo(verticesAfter[2].Position);
        Assert.Equal(20, seg2LenAfter, 2);
    }
}