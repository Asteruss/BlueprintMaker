using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Tests;

public class JambPlacementTests
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
    public void AddJamb_External_AttachesToWallOutside()
    {
        var frame = CreateRectFrame(100, 100);

        // UI передает: сегмент 0, без пространства (null)
        var jamb = frame.AddJamb(0, targetSpace: null);

        // У рамы теперь 2 ребенка: её InnerSpace и внешний доборник
        Assert.Equal(2, frame.Children.Count);

        // Проверяем, что доборник реально лежит в детях рамы
        Assert.Contains(jamb, frame.Children);

        // У самого доборника нет детей (он еще не материализован и не имеет своих вложенных пространств)
        Assert.Empty(jamb.Children);
    }

    [Fact]
    public void AddJamb_Internal_OccupiesGivenSpace()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // Вставляем импост, чтобы получить 2 пространства
        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // UI передает: сегмент 0 ИМПОСТА, и целевое пространство space1
        var jamb = impost.AddJamb(0, targetSpace: space1);

        // Доборник должен стать ребенком space1, а не импоста
        Assert.Equal(2, space1.Children.Count); // Доборник + Остаток
        Assert.Equal(jamb, space1.Children[0]);

        // Space2 остался пустым
        Assert.Empty(space2.Children);

        // У самого импоста не должно появиться детей (он не родитель доборника)
        Assert.Empty(impost.Children);
    }

    [Fact]
    public void AddJamb_Internal_ToOccupiedSpace_Throws()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // Занимаем rootSpace первым доборником
        frame.AddJamb(0, targetSpace: rootSpace);

        // Пытаемся всунуть туда же второй
        Assert.Throws<InvalidOperationException>(() =>
            frame.AddJamb(1, targetSpace: rootSpace));
    }

    [Fact]
    public void AddJamb_MultipleJambsInSequence_OccupiesCorrectSpaces()
    {
        var frame = CreateRectFrame(100, 100);
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;

        // 1. Вставляем вертикальный импост по центру (делит на лево/право)
        var (space1, space2, impost) = rootSpace.InsertImpost(AxisOrientation.Vertical, 0.5);

        // 2. Добавляем ПЕРВЫЙ доборник в space1, прикрепив к импосту
        var jamb1 = impost.AddJamb(0, targetSpace: space1);

        // В space1 теперь 2 ребенка: jamb1 и Остаток1
        Assert.Equal(2, space1.Children.Count);
        var remainingSpace1 = space1.Children[1] as LightSpace;
        Assert.NotNull(remainingSpace1);

        // 3. Добавляем второй доборник, прикрепив его к первому доборнику, 
        // и помещаем в Остаток1
        var jamb2 = jamb1.AddJamb(0, targetSpace: remainingSpace1);

        // В Остатке1 теперь 2 ребенка: jamb2 и Остаток2
        Assert.Equal(2, remainingSpace1.Children.Count);
        Assert.Equal(jamb2, remainingSpace1.Children[0]);

        var remainingSpace2 = remainingSpace1.Children[1] as LightSpace;
        Assert.NotNull(remainingSpace2);

        // 4. Проверяем финальный остаток
        // Он должен быть пустым (готов к заполнению стеклом или декором)
        Assert.Empty(remainingSpace2.Children);

        // И убеждаемся, что правое пространство (space2) вообще не тронули
        Assert.Empty(space2.Children);
    }
}