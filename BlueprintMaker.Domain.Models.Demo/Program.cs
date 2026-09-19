using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Demo;

class Program
{
    static void Main()
    {
        var vertices = new ContourVertex[]
        {
            new(new Point(0, 0)),
            new(new Point(100, 0)),
            new(new Point(100, 100)),
            new(new Point(0, 100))
        };
        var contour = Contour.CreateClosed(vertices);

        // Создаем раму
        Frame frame = Frame.Create(contour);
        var lightSpace = frame.InnerSpace;

        Console.WriteLine($"Рама (Скелет): {frame.Width} мм ширина");
        PrintBounds("Световое пространство", lightSpace.GetContour());

        Console.WriteLine("\n--- ЭТАП 2: Материализация (Расширение) ---");
        // Пользователь нажал "Расширить" и задал ширину бруса 10 мм
        frame.Materialize(10);

        Console.WriteLine($"Рама (Материализована): {frame.Width} мм ширина");
        PrintBounds("Световое пространство", lightSpace.GetContour());

        Console.WriteLine("\n--- ЭТАП 3: Деформация рамы (Сдвиг угла) ---");
        // Пользователь взял правый нижний угол и потянул его на 150 по X и 200 по Y
        var oldVertices = frame.GetContour().Vertices;
        var newVertices = oldVertices.Select((v, i) =>
            i == 2 ? new ContourVertex(new Point(150, 200), v.Bulge) : v // Меняем 3-ю точку (индекс 2)
        ).ToList();

        var newContour = Contour.CreateClosed(newVertices);
        frame.UpdateContour(newContour);

        Console.WriteLine("Угол рамы сдвинут на (150, 200).");
        PrintBounds("Световое пространство", lightSpace.GetContour());

        Console.ReadKey();
    }

    static void PrintBounds(string name, Contour contour)
    {
        var bounds = contour.BoundingBox;
        Console.WriteLine($"{name}: Ширина={bounds.Width:F2}, Высота={bounds.Height:F2} " +
                          $"(от X={bounds.MinX:F2}, Y={bounds.MinY:F2})");
    }
}