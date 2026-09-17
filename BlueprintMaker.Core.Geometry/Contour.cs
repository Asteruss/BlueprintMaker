using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueprintMaker.Core.Geometry;

/// <summary>
/// Локальный контур элемента. Хранит форму (проволоку) одного конкретного элемента (рамы, импоста)
/// Неизменяем: при любом изменении формы создается новый экземпляр
/// </summary>
public sealed class Contour
{
    public IReadOnlyList<ContourVertex> Vertices { get; }
    public bool IsClosed { get; }
    public Rect BoundingBox { get; }

    private Contour(IReadOnlyList<ContourVertex> vertices, bool isClosed)
    {
        Vertices = vertices;
        IsClosed = isClosed;
        BoundingBox = ContourMath.CalculateBounds(vertices, isClosed);
    }

    public static Contour CreateClosed(IReadOnlyList<ContourVertex> vertices)
    {
        if (vertices.Count < 3)
            throw new ArgumentException("Замкнутый контур должен содержать минимум 3 вершины.");
        return new Contour(vertices, isClosed: true);
    }

    public static Contour CreateOpen(IReadOnlyList<ContourVertex> vertices)
    {
        if (vertices.Count < 2)
            throw new ArgumentException("Открытый контур должен содержать минимум 2 вершины.");
        return new Contour(vertices, isClosed: false);
    }
}