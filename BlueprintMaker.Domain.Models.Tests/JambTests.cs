using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Tests;

public class JambTests
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
    public void Jamb_AttachedToSingleSegment_Has2Vertices()
    {
        var frame = CreateRectFrame(100, 100);

        // Крепим к сегменту 0 (нижняя грань: от 0,0 до 100,0)
        var jamb = frame.AttachJamb(0, 1);

        // Доборник должен быть открытой линией с 2 вершинами
        Assert.False(jamb.GetContour().IsClosed);
        Assert.Equal(2, jamb.GetContour().Vertices.Count);

        // Габариты должны совпадать с нижней гранью
        Assert.Equal(100, jamb.GetContour().BoundingBox.Width, 2);
        Assert.Equal(0, jamb.GetContour().BoundingBox.MinY, 2);
    }

    [Fact]
    public void Jamb_AttachedToMultipleSegments_Has3Vertices()
    {
        var frame = CreateRectFrame(100, 100);

        // Крепим к сегментам 0 и 1 (нижняя и правая грани: 0,0 -> 100,0 -> 100,100)
        var jamb = frame.AttachJamb(0, 2);

        Assert.False(jamb.GetContour().IsClosed);
        Assert.Equal(3, jamb.GetContour().Vertices.Count);

        // Проверяем среднюю точку (она должна быть 100,0)
        Assert.Equal(new Point(100, 0), jamb.GetContour().Vertices[1].Position);
    }

    [Fact]
    public void Jamb_AttachedToFullContour_BecomesClosed()
    {
        var frame = CreateRectFrame(100, 100);

        // Крепим ко всем 4 сегментам рамы (весь периметр)
        var jamb = frame.AttachJamb(0, 4);

        // Доборник должен стать замкнутым контуром
        Assert.True(jamb.GetContour().IsClosed);
        Assert.Equal(4, jamb.GetContour().Vertices.Count);
    }
}
