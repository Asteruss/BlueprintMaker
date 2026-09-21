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
    [Fact]
    public void ComplexTest_HexagonFrame_WithImpostsAndJambs()
    {
        // 1. Создаем стандартную прямоугольную раму 100x100
        var frame = CreateRectFrame(100, 100);

        // 2. Добавляем узлы (превращаем прямоугольник в 6-угольник)
        // Разбиваем верхнюю грань (индекс 0) и нижнюю (теперь индекс 3)
        frame.SplitAt(0); // Добавит узел на (50, 0)
        frame.SplitAt(3); // Добавит узел на (50, 100)

        // Теперь у нас 6 вершин: 
        // 0:(0,0), 1:(50,0), 2:(100,0), 3:(100,100), 4:(50,100), 5:(0,100)
        Assert.Equal(6, frame.GetContour().Vertices.Count);

        double h = 50 * Math.Sqrt(3); // Высота правильного шестиугольника (~86.6)
        double halfH = h / 2;        // Середина по Y (~43.3)

        // 3. Двигаем узлы, формируя правильный шестиугольник (Flat-Top)
        frame.MoveNode(0, new Point(25, 0));
        frame.MoveNode(1, new Point(75, 0));
        frame.MoveNode(2, new Point(100, halfH));
        frame.MoveNode(3, new Point(75, h));
        frame.MoveNode(4, new Point(25, h));
        frame.MoveNode(5, new Point(0, halfH));

        // 4. Материализуем раму с шириной бруса 10
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // 5. Вставляем первый вертикальный импост по центру
        var (space1, space2, impost1) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // Импост должен встать ровно по центру (X=50)
        Assert.Equal(50, impost1.GetContour().BoundingBox.MinX, 1);

        // 6. Вставляем второй (горизонтальный) импост в правую часть (space2)
        var (space2a, space2b, impost2) = space2.InsertImpost(AxisOrientation.Horizontal, 0.5);

        // Проверяем, что правое пространство разделилось на верхнее и нижнее
        Assert.True(space2a.GetContour().BoundingBox.MinY < halfH, "Верхнее правое прост. должно быть выше центра");
        Assert.True(space2b.GetContour().BoundingBox.MinY >= halfH - 0.1, "Нижнее правое прост. должно быть ниже центра");

        // 7. Добавляем внешний доборник на правую скошенную грань рамы (сегмент 2: от (100, halfH) до (75, h))
        var jamb = frame.AddJamb(2, targetSpace: null);

        // Доборник должен стать ребенком рамы
        Assert.Contains(jamb, frame.Children);
        Assert.Equal(2, jamb.GetContour().Vertices.Count);

        // Проверяем, что координаты доборника совпадают с координатами сегмента рамы
        Assert.Equal(new Point(100, halfH), jamb.GetContour().Vertices[0].Position);
        Assert.Equal(new Point(75, h), jamb.GetContour().Vertices[1].Position);

        // 8. Добавляем внутренний доборник в левое пространство (space1), прикрепив к импосту
        var innerJamb = impost1.AddJamb(0, targetSpace: space1);

        // space1 должен заняться (появится Доборник и Остаток)
        Assert.Equal(2, space1.Children.Count);
        Assert.Equal(innerJamb, space1.Children[0]);
    }
}