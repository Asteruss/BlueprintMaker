using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>
/// Стратегия: повторение формы сегмента элемента, к которому крепится доборник
/// </summary>
public sealed class SegmentOffsetRule : IDerivationRule
{
    private readonly IWindowNode _wallParent; // Элемент, к которому крепимся (Рама/Импост)
    public int StartSegment { get; }
    public int SegmentCount { get; }

    public SegmentOffsetRule(IWindowNode wallParent, int startSegment, int segmentCount)
    {
        _wallParent = wallParent;
        StartSegment = startSegment;
        SegmentCount = segmentCount;
    }

    public Contour Resolve(IWindowNode parent)
    {
        // Берем контур от сегмента
        var wallContour = _wallParent.GetContour();
        var wallVerts = wallContour.Vertices;

        // Логика вырезания сегментов (как была раньше)
        if (SegmentCount == wallVerts.Count && wallContour.IsClosed)
            return Contour.CreateClosed(new List<ContourVertex>(wallVerts));
        

        var resultVerts = new List<ContourVertex> { wallVerts[StartSegment] };
        for (int i = 1; i <= SegmentCount; i++)
        {
            int idx = (StartSegment + i) % wallVerts.Count;
            resultVerts.Add(wallVerts[idx]);
        }

        return Contour.CreateOpen(resultVerts);
    }
}