using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Materialisation.Tests;

public class FrameMaterialisationTests
{
    private double ToMm(double val) => Math.Floor(val);

    private Frame CreateAndMaterialize(Contour contour, double width)
    {
        var frame = Frame.Create(contour);
        frame.Materialize(width);
        return frame;
    }

    private void AssertMiter(Frame frame, List<double> expectedLengths)
    {
        var boards = frame.Assembly.Boards;
        Assert.Equal(expectedLengths.Count, boards.Count);
        for (int i = 0; i < boards.Count; i++)
        {
            Assert.Equal(ToMm(expectedLengths[i]), ToMm(boards[i].Length));
        }
    }

    private void AssertTenonMortise(Frame frame, List<double> expectedLengths, double width)
    {
        var assembly = frame.Assembly;

        // Устанавливаем все стыки: Доска A - Шип, Доска B - Паз
        for (int i = 0; i < assembly.Joints.Count; i++)
        {
            assembly.ChangeJoint(i, JointEndType.Tenon);
        }

        // При такой рассадке каждая доска имеет StartJoint (Паз, полная длина) и EndJoint (Шип, укорочена на ширину).
        // Значит, итоговая длина = ожидаемая длина - ширина.
        var boards = assembly.Boards;
        for (int i = 0; i < boards.Count; i++)
        {
            Assert.Equal(ToMm(expectedLengths[i] - width), ToMm(boards[i].Length));
        }
    }

    private Contour CreateRect(double w, double h) => Contour.CreateClosed(new[]
    {
        new ContourVertex(new Point(0, 0)), new ContourVertex(new Point(w, 0)),
        new ContourVertex(new Point(w, h)), new ContourVertex(new Point(0, h))
    });

    // 2.1 Материализация прямоугольной проволоки
    [Fact]
    public void Test_Rectangle()
    {
        var contour = CreateRect(100, 50);
        var frame = CreateAndMaterialize(contour, 20);
        var expected = new List<double> { 100, 50, 100, 50 };
        AssertMiter(frame, expected);
        AssertTenonMortise(frame, expected, 20);
    }

    // 2.2 Материализация 4-угольной (не прямоугольной) проволоки
    [Fact]
    public void Test_Quadrilateral()
    {
        // Трапеция: 100x100, верх 60
        var contour = Contour.CreateClosed(new[]
        {
            new ContourVertex(new Point(0, 0)),
            new ContourVertex(new Point(100, 0)),
            new ContourVertex(new Point(80, 100)),
            new ContourVertex(new Point(20, 100))
        });
        var frame = CreateAndMaterialize(contour, 20);

        // Вычисляем точные длины сторон
        double s0 = 100;
        double s1 = new Point(100, 0).DistanceTo(new Point(80, 100)); // ~102.19
        double s2 = 60;
        double s3 = new Point(20, 100).DistanceTo(new Point(0, 0));   // ~102.19
        var expected = new List<double> { s0, s1, s2, s3 };

        AssertMiter(frame, expected);
        AssertTenonMortise(frame, expected, 20);
    }

    // 2.3 Материализация правильного пятиугольника
    [Fact]
    public void Test_RegularPentagon()
    {
        var points = new List<Point>();
        for (int i = 0; i < 5; i++)
        {
            double angle = Math.PI / 2 + i * 2 * Math.PI / 5;
            points.Add(new Point(100 * Math.Cos(angle), 100 * Math.Sin(angle)));
        }
        var contour = Contour.CreateClosed(points.Select(p => new ContourVertex(p)).ToArray());
        var frame = CreateAndMaterialize(contour, 20);

        double side = points[0].DistanceTo(points[1]); // ~117.55
        var expected = Enumerable.Repeat(side, 5).ToList();

        AssertMiter(frame, expected);
        AssertTenonMortise(frame, expected, 20);
    }

    // 2.4 Материализация неправильного пятиугольника
    [Fact]
    public void Test_IrregularPentagon()
    {
        var contour = Contour.CreateClosed(new[]
        {
            new ContourVertex(new Point(0, 0)),
            new ContourVertex(new Point(100, 0)),
            new ContourVertex(new Point(120, 40)), // Острый угол
            new ContourVertex(new Point(50, 80)),  // Тупой угол
            new ContourVertex(new Point(-20, 30))  // Выступ
        });
        var frame = CreateAndMaterialize(contour, 20);

        var expected = new List<double>
        {
            100,
            new Point(100, 0).DistanceTo(new Point(120, 40)),
            new Point(120, 40).DistanceTo(new Point(50, 80)),
            new Point(50, 80).DistanceTo(new Point(-20, 30)),
            new Point(-20, 30).DistanceTo(new Point(0, 0))
        };
        AssertMiter(frame, expected);
        AssertTenonMortise(frame, expected, 20);
    }

    // 2.5 Материализация правильного шестиугольника
    [Fact]
    public void Test_RegularHexagon()
    {
        var points = new List<Point>();
        for (int i = 0; i < 6; i++)
        {
            double angle = i * Math.PI / 3;
            points.Add(new Point(100 * Math.Cos(angle), 100 * Math.Sin(angle)));
        }
        var contour = Contour.CreateClosed(points.Select(p => new ContourVertex(p)).ToArray());
        var frame = CreateAndMaterialize(contour, 20);

        double side = 100;
        var expected = Enumerable.Repeat(side, 6).ToList();

        AssertMiter(frame, expected);
        AssertTenonMortise(frame, expected, 20);
    }

    // 2.6 Материализация неправильного шестиугольника
    [Fact]
    public void Test_IrregularHexagon()
    {
        var contour = Contour.CreateClosed(new[]
        {
            new ContourVertex(new Point(0, 0)),
            new ContourVertex(new Point(50, 0)),
            new ContourVertex(new Point(100, 30)),
            new ContourVertex(new Point(80, 90)),
            new ContourVertex(new Point(20, 100)),
            new ContourVertex(new Point(-10, 50))
        });
        var frame = CreateAndMaterialize(contour, 20);

        var expected = new List<double>
        {
            50,
            new Point(50, 0).DistanceTo(new Point(100, 30)),
            new Point(100, 30).DistanceTo(new Point(80, 90)),
            new Point(80, 90).DistanceTo(new Point(20, 100)),
            new Point(20, 100).DistanceTo(new Point(-10, 50)),
            new Point(-10, 50).DistanceTo(new Point(0, 0))
        };
        AssertMiter(frame, expected);
        AssertTenonMortise(frame, expected, 20);
    }

    // 2.7 Материализация 4-угольной с аркой
    [Fact]
    public void Test_ArchedQuadrilateral()
    {
        // Прямоугольник 100x50, верхняя грань - дуга
        var contour = Contour.CreateClosed(new[]
        {
            new ContourVertex(new Point(0, 0)),
            new ContourVertex(new Point(100, 0), 0.5), // Дуга
            new ContourVertex(new Point(100, 50)),
            new ContourVertex(new Point(0, 50))
        });
        var frame = CreateAndMaterialize(contour, 20);

        // ВАЖНО: Текущая математика Board считает длину по хорде (расстояние между точками).
        // Для дуги длина по хорде = 100. В будущем добавим расчет по дуге.
        var expected = new List<double> { 100, 50, 100, 50 };

        AssertMiter(frame, expected);
        AssertTenonMortise(frame, expected, 20);
    }
}